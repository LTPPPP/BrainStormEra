using BrainStormEra.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

public class AchievementRepo
{
    private readonly SwpMainContext _context;

    public AchievementRepo(SwpMainContext context)
    {
        _context = context;
    }

    // Retrieve learner's achievements based on userId
    public async Task<List<dynamic>> GetLearnerAchievements(string userId)
    {
        var achievements = new List<dynamic>();

        using (var connection = _context.Database.GetDbConnection())
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
        using (var connection = _context.Database.GetDbConnection())
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
}
