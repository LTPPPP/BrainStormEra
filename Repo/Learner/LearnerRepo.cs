using BrainStormEra.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using BrainStormEra.Views.Course;

namespace BrainStormEra.Repo
{
    public class LearnerRepo
    {
        private readonly string _connectionString;

        public LearnerRepo(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SwpMainContext");
        }

        private async Task<DbConnection> GetConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async Task<Models.Account> GetUserByIdAsync(string userId)
        {
            using var connection = await GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM account WHERE user_id = @userId";
            command.Parameters.Add(new SqlParameter("@userId", userId));

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Models.Account
                {
                    UserId = reader["user_id"].ToString(),
                    FullName = reader["full_name"].ToString(),
                    UserPicture = reader["user_picture"]?.ToString()
                };
            }
            return null;
        }

        public async Task<int> GetCompletedCoursesCountAsync(string userId)
        {
            using var connection = await GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM enrollment WHERE user_id = @userId AND enrollment_status = 5";
            command.Parameters.Add(new SqlParameter("@userId", userId));

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<int> GetTotalCoursesEnrolledAsync(string userId)
        {
            using var connection = await GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM enrollment WHERE user_id = @userId";
            command.Parameters.Add(new SqlParameter("@userId", userId));

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<List<Models.Achievement>> GetAchievementsAsync(string userId)
        {
            var achievements = new List<Models.Achievement>();

            using var connection = await GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT TOP 3
                    a.achievement_id,
                    a.achievement_name,
                    a.achievement_description,
                    a.achievement_icon,
                    a.achievement_created_at
                FROM user_achievement ua
                INNER JOIN achievement a ON ua.achievement_id = a.achievement_id
                WHERE ua.user_id = @userId
                ORDER BY a.achievement_created_at DESC";
            command.Parameters.Add(new SqlParameter("@userId", userId));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                achievements.Add(new Models.Achievement
                {
                    AchievementId = reader["achievement_id"].ToString(),
                    AchievementName = reader["achievement_name"].ToString(),
                    AchievementDescription = reader["achievement_description"].ToString(),
                    AchievementIcon = reader["achievement_icon"].ToString(),
                    AchievementCreatedAt = Convert.ToDateTime(reader["achievement_created_at"])
                });
            }
            return achievements;
        }

        public async Task<List<ManagementCourseViewModel>> GetRecommendedCoursesAsync(string userId)
        {
            var recommendedCourses = new List<ManagementCourseViewModel>();

            using var connection = await GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT TOP 4
                    c.course_id AS CourseId,
                    c.course_name AS CourseName,
                    c.course_description AS CourseDescription,
                    c.course_status AS CourseStatus,
                    c.course_picture AS CoursePicture,
                    c.price AS Price,
                    c.course_created_at AS CourseCreatedAt,
                    a.full_name AS CreatedBy,
                    ROUND(AVG(COALESCE(f.star_rating, 0)), 0) AS StarRating,
                    STUFF((
                        SELECT DISTINCT ', ' + cc.course_category_name
                        FROM course_category_mapping AS ccm
                        JOIN course_category AS cc ON ccm.course_category_id = cc.course_category_id
                        WHERE ccm.course_id = c.course_id
                        FOR XML PATH(''), TYPE
                    ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS CourseCategories
                FROM
                    course AS c
                JOIN
                    account AS a ON c.created_by = a.user_id
                LEFT JOIN
                    enrollment AS e ON c.course_id = e.course_id
                LEFT JOIN
                    feedback AS f ON c.course_id = f.course_id
                WHERE
                    c.course_status = 2
                GROUP BY
                    c.course_id, c.course_name, c.course_description, c.course_status,
                    c.course_picture, c.price, c.course_created_at, a.full_name
                ORDER BY
                    COUNT(e.enrollment_id) DESC;";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var categories = new List<CourseCategory>();
                var categoryNames = reader["CourseCategories"].ToString().Split(',');

                foreach (var categoryName in categoryNames)
                {
                    var trimmedName = categoryName.Trim();
                    if (!string.IsNullOrEmpty(trimmedName))
                    {
                        categories.Add(new CourseCategory { CourseCategoryName = trimmedName });
                    }
                }

                recommendedCourses.Add(new ManagementCourseViewModel
                {
                    CourseId = reader["CourseId"].ToString(),
                    CourseName = reader["CourseName"].ToString(),
                    CourseDescription = reader["CourseDescription"].ToString(),
                    CourseStatus = Convert.ToInt32(reader["CourseStatus"]),
                    CoursePicture = reader["CoursePicture"].ToString(),
                    Price = Convert.ToDecimal(reader["Price"]),
                    CourseCreatedAt = Convert.ToDateTime(reader["CourseCreatedAt"]),
                    CreatedBy = reader["CreatedBy"].ToString(),
                    StarRating = reader["StarRating"] != DBNull.Value ? (byte?)Convert.ToByte(reader["StarRating"]) : (byte?)0,
                    CourseCategories = categories
                });
            }
            return recommendedCourses;
        }

        public async Task<List<Models.Notification>> GetNotificationsAsync(string userId)
        {
            var notifications = new List<Models.Notification>();

            using var connection = await GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT *
                FROM notification
                WHERE user_id = @userId
                ORDER BY notification_created_at DESC";
            command.Parameters.Add(new SqlParameter("@userId", userId));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                notifications.Add(new Models.Notification
                {
                    NotificationId = reader["notification_id"].ToString(),
                    UserId = reader["user_id"].ToString(),
                    NotificationContent = reader["notification_content"].ToString(),
                    NotificationType = reader["notification_type"].ToString(),
                    NotificationCreatedAt = Convert.ToDateTime(reader["notification_created_at"])
                });
            }
            return notifications;
        }

        public List<CourseCategory> GetTopCategories()
        {
            string categoryQuery = @"
                SELECT TOP 5
                    course_category_id AS CourseCategoryId,
                    course_category_name AS CourseCategoryName
                FROM
                    course_category
                ORDER BY
                    course_category_name;
            ";

            var categories = new List<CourseCategory>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = categoryQuery;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(new CourseCategory
                            {
                                CourseCategoryId = reader["CourseCategoryId"].ToString(),
                                CourseCategoryName = reader["CourseCategoryName"].ToString()
                            });
                        }
                    }
                }
            }

            return categories;
        }
    }
}
