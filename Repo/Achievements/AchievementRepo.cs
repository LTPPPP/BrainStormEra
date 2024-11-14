using BrainStormEra.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AchievementRepo
{
    private readonly string _connectionString;

    public AchievementRepo(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SwpMainContext");
    }

    // Retrieve learner's achievements based on userId
    public async Task<List<dynamic>> GetLearnerAchievements(string userId)
    {
        var achievements = new List<dynamic>();

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT a.achievement_id, a.achievement_name, a.achievement_description, 
                           a.achievement_icon, ua.received_date
                    FROM user_achievement ua
                    JOIN achievement a ON ua.achievement_id = a.achievement_id
                    WHERE ua.user_id = @userId";
                command.Parameters.Add(new SqlParameter("@userId", userId));

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        achievements.Add(new
                        {
                            AchievementId = reader["achievement_id"].ToString(),
                            AchievementName = reader["achievement_name"].ToString(),
                            AchievementDescription = reader["achievement_description"].ToString(),
                            AchievementIcon = reader["achievement_icon"].ToString(),
                            ReceivedDate = reader["received_date"].ToString()
                        });
                    }
                }
            }
        }

        return achievements;
    }

    // Retrieve achievement details based on achievementId and userId
    public async Task<dynamic> GetAchievementDetails(string achievementId, string userId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT a.achievement_name, a.achievement_description, 
                           a.achievement_icon, ua.received_date
                    FROM user_achievement ua
                    JOIN achievement a ON ua.achievement_id = a.achievement_id
                    WHERE ua.user_id = @userId AND ua.achievement_id = @achievementId";
                command.Parameters.Add(new SqlParameter("@userId", userId));
                command.Parameters.Add(new SqlParameter("@achievementId", achievementId));

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new
                        {
                            AchievementName = reader["achievement_name"].ToString(),
                            AchievementDescription = reader["achievement_description"].ToString(),
                            AchievementIcon = reader["achievement_icon"].ToString(),
                            ReceivedDate = reader["received_date"]
                        };
                    }
                }
            }
        }

        return null;
    }

    // Retrieve all achievements for admin view
    public async Task<List<dynamic>> GetAdminAchievements()
    {
        var allAchievements = new List<dynamic>();

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
    SELECT a.achievement_id, a.achievement_name, a.achievement_description, 
           a.achievement_icon, a.achievement_created_at,
           ISNULL((SELECT TOP 1 acc.full_name FROM user_achievement ua 
                   JOIN account acc ON ua.user_id = acc.user_id
                   WHERE ua.achievement_id = a.achievement_id), 'null') AS user_name
    FROM achievement a
    ORDER BY a.achievement_id";


                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        allAchievements.Add(new
                        {
                            AchievementId = reader["achievement_id"].ToString(),
                            AchievementName = reader["achievement_name"].ToString(),
                            AchievementDescription = reader["achievement_description"].ToString(),
                            AchievementIcon = reader["achievement_icon"].ToString(),
                            AchievementCreatedAt = reader["achievement_created_at"],
                            UserName = reader["user_name"].ToString()
                        });
                    }
                }
            }
        }

        return allAchievements;
    }


    // Add a new achievement
    public async Task AddAchievement(string achievementName, string achievementDescription, string iconPath)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    DECLARE @nextId NVARCHAR(10) = (
                        SELECT COALESCE(MAX(CAST(SUBSTRING(achievement_id, 2, LEN(achievement_id) - 1) AS INT)) + 1, 1)
                        FROM achievement
                    );
                    SET @nextId = 'A' + RIGHT('000' + CAST(@nextId AS NVARCHAR), 3);

                    INSERT INTO achievement (achievement_id, achievement_name, achievement_description, achievement_icon, achievement_created_at)
                    VALUES (@nextId, @achievementName, @achievementDescription, @iconPath, @createdAt);";
                command.Parameters.Add(new SqlParameter("@achievementName", achievementName));
                command.Parameters.Add(new SqlParameter("@achievementDescription", achievementDescription));
                command.Parameters.Add(new SqlParameter("@iconPath", iconPath));
                command.Parameters.Add(new SqlParameter("@createdAt", DateTime.Today));

                await command.ExecuteNonQueryAsync();
            }
        }
    }

    // Edit an existing achievement
    public async Task<bool> EditAchievement(string achievementId, string achievementName, string achievementDescription, string iconPath, DateTime? achievementCreatedAt)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    UPDATE achievement
                    SET achievement_name = @achievementName,
                        achievement_description = @achievementDescription,
                        achievement_icon = COALESCE(@iconPath, achievement_icon),
                        achievement_created_at = @achievementCreatedAt
                    WHERE achievement_id = @achievementId;";
                command.Parameters.Add(new SqlParameter("@achievementId", achievementId));
                command.Parameters.Add(new SqlParameter("@achievementName", achievementName));
                command.Parameters.Add(new SqlParameter("@achievementDescription", achievementDescription));
                command.Parameters.Add(new SqlParameter("@iconPath", (object)iconPath ?? DBNull.Value));
                command.Parameters.Add(new SqlParameter("@achievementCreatedAt", achievementCreatedAt ?? DateTime.Today));

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }
    }

    // Delete an achievement
    // Delete an achievement
    public async Task<bool> DeleteAchievement(string achievementId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                DELETE FROM achievement
                WHERE achievement_id = @achievementId;";
                command.Parameters.Add(new SqlParameter("@achievementId", achievementId));

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }
    }
    // Check if an achievement name already exists
    // Check if an achievement name already exists
    public async Task<bool> AchievementNameExists(string achievementName, string achievementId = null)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = achievementId == null
                    ? @"SELECT COUNT(*) FROM achievement WHERE achievement_name = @achievementName"
                    : @"SELECT COUNT(*) FROM achievement WHERE achievement_name = @achievementName AND achievement_id != @achievementId";

                command.Parameters.Add(new SqlParameter("@achievementName", achievementName));

                if (achievementId != null)
                    command.Parameters.Add(new SqlParameter("@achievementId", achievementId));

                var count = (int)await command.ExecuteScalarAsync();
                return count > 0;
            }
        }
    }

    // Get the maximum condition value from existing achievements
    public async Task<int> GetMaxCondition()
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT MAX(CAST(achievement_description AS INT)) FROM achievement WHERE ISNUMERIC(achievement_description) = 1";
                var maxCondition = await command.ExecuteScalarAsync();
                return maxCondition != DBNull.Value ? Convert.ToInt32(maxCondition) : 0;
            }
        }
    }
    public async Task<List<int>> GetAllConditions()
    {
        var conditions = new List<int>();

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT CAST(achievement_description AS INT) FROM achievement WHERE ISNUMERIC(achievement_description) = 1";
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        conditions.Add(Convert.ToInt32(reader[0]));
                    }
                }
            }
        }

        return conditions;
    }

    public async Task AssignAchievementsBasedOnCompletedCourses()
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            // Get all achievements with condition (number of completed courses)
            var achievements = new List<dynamic>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                SELECT achievement_id, CAST(achievement_description AS INT) AS required_courses
                FROM achievement
                WHERE ISNUMERIC(achievement_description) = 1";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        achievements.Add(new
                        {
                            AchievementId = reader["achievement_id"].ToString(),
                            RequiredCourses = Convert.ToInt32(reader["required_courses"])
                        });
                    }
                }
            }

            // Get count of completed courses (enrollment_status = 4) per user
            var userCompletedCourses = new Dictionary<string, int>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                SELECT user_id, COUNT(*) AS completed_courses
                FROM enrollment
                WHERE enrollment_status = 5
                GROUP BY user_id";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        userCompletedCourses[reader["user_id"].ToString()] = Convert.ToInt32(reader["completed_courses"]);
                    }
                }
            }

            // Insert achievements for each user based on completed courses
            foreach (var user in userCompletedCourses)
            {
                foreach (var achievement in achievements)
                {
                    if (user.Value >= achievement.RequiredCourses)
                    {
                        using (var insertCommand = connection.CreateCommand())
                        {
                            insertCommand.CommandText = @"
                            IF NOT EXISTS (SELECT 1 FROM user_achievement WHERE user_id = @userId AND achievement_id = @achievementId)
                            BEGIN
                                INSERT INTO user_achievement (user_id, achievement_id, received_date, enrollment_id)
                                VALUES (@userId, @achievementId, @receivedDate, NULL)
                            END";

                            insertCommand.Parameters.AddWithValue("@userId", user.Key);
                            insertCommand.Parameters.AddWithValue("@achievementId", achievement.AchievementId);
                            insertCommand.Parameters.AddWithValue("@receivedDate", DateTime.Today);

                            await insertCommand.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }
    }


    // Retrieve a single achievement for display
    public async Task<dynamic> GetAchievement(string achievementId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT achievement_id, achievement_name, achievement_description, 
                           achievement_icon, achievement_created_at
                    FROM achievement
                    WHERE achievement_id = @achievementId;";
                command.Parameters.Add(new SqlParameter("@achievementId", achievementId));

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new
                        {
                            AchievementId = reader["achievement_id"].ToString(),
                            AchievementName = reader["achievement_name"].ToString(),
                            AchievementDescription = reader["achievement_description"].ToString(),
                            AchievementIcon = reader["achievement_icon"].ToString(),
                            AchievementCreatedAt = reader["achievement_created_at"]
                        };
                    }
                }
            }
        }

        return null;
    }

}
