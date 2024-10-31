using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using BrainStormEra.Views.Home;
using BrainStormEra.Views.Course;

namespace BrainStormEra.Controllers
{
    public class HomePageLearnerController : Controller
    {
        private readonly SwpMainContext _dbContext;
        private readonly ILogger<HomePageLearnerController> _logger;

        public HomePageLearnerController(SwpMainContext dbContext, ILogger<HomePageLearnerController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> HomePageLearner()
        {
            var userId = Request.Cookies["user_id"];

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Cookies do not contain user_id.");
                return RedirectToAction("LoginPage", "Login");
            }

            
            Models.Account user = null;
            int completedCoursesCount = 0;
            int totalCoursesEnrolled = 0;
            int userRank = 0;
            var achievements = new List<Models.Achievement>();
            var recommendedCourses = new List<ManagementCourseViewModel>();
            var notifications = new List<Notification>();

            using (var connection = _dbContext.Database.GetDbConnection())
            {
                await connection.OpenAsync();

                // Get user information
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM account WHERE user_id = @userId";
                    command.Parameters.Add(new SqlParameter("@userId", userId));

                    using var reader = await command.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        user = new Models.Account
                        {
                            UserId = reader["user_id"].ToString(),
                            FullName = reader["full_name"].ToString(),
                            UserPicture = reader["user_picture"]?.ToString()
                        };
                    }
                }

                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found.");
                    return RedirectToAction("LoginPage", "Login");
                }

                // Get completed courses count
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                SELECT COUNT(*) 
                FROM enrollment 
                WHERE user_id = @userId AND enrollment_status = 5";
                    command.Parameters.Add(new SqlParameter("@userId", userId));

                    completedCoursesCount = Convert.ToInt32(await command.ExecuteScalarAsync());
                }

                // Get total courses enrolled
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                SELECT COUNT(*) 
                FROM enrollment 
                WHERE user_id = @userId";
                    command.Parameters.Add(new SqlParameter("@userId", userId));

                    totalCoursesEnrolled = Convert.ToInt32(await command.ExecuteScalarAsync());
                }

                // Get achievements
                using (var command = connection.CreateCommand())
                {
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
                }

                // Get user rank
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                WITH RankedUsers AS (
                    SELECT 
                        user_id,
                        DENSE_RANK() OVER (ORDER BY COUNT(*) DESC) as rank
                    FROM enrollment
                    WHERE enrollment_status = 5
                    GROUP BY user_id
                )
                SELECT COALESCE(rank, 0) as rank
                FROM RankedUsers
                WHERE user_id = @userId";
                    command.Parameters.Add(new SqlParameter("@userId", userId));

                    var result = await command.ExecuteScalarAsync();
                    userRank = result != DBNull.Value ? Convert.ToInt32(result) : 0;
                }

                // Get recommended courses
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                SELECT TOP 4 
                    c.course_id, 
                    c.course_name, 
                    c.course_description, 
                    c.course_status, 
                    c.course_picture, 
                    c.price, 
                    c.course_created_at, 
                    a.full_name AS CreatedBy,
                    COALESCE(AVG(CAST(f.star_rating AS FLOAT)), 0) as StarRating
                FROM course AS c 
                LEFT JOIN enrollment AS e ON c.course_id = e.course_id 
                INNER JOIN account AS a ON c.created_by = a.user_id 
                LEFT JOIN feedback AS f ON c.course_id = f.course_id
                WHERE c.course_status = 2 
                AND (e.user_id IS NULL OR e.user_id != @userId) 
                GROUP BY 
                    c.course_id, 
                    c.course_name, 
                    c.course_description, 
                    c.course_status, 
                    c.course_picture, 
                    c.price, 
                    c.course_created_at, 
                    a.full_name 
                ORDER BY COUNT(e.user_id) DESC";
                    command.Parameters.Add(new SqlParameter("@userId", userId));

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        recommendedCourses.Add(new ManagementCourseViewModel
                        {
                            CourseId = reader["course_id"].ToString(),
                            CourseName = reader["course_name"].ToString(),
                            CourseDescription = reader["course_description"].ToString(),
                            CourseStatus = Convert.ToInt32(reader["course_status"]),
                            CoursePicture = reader["course_picture"].ToString(),
                            Price = Convert.ToDecimal(reader["price"]),
                            CourseCreatedAt = Convert.ToDateTime(reader["course_created_at"]),
                            CreatedBy = reader["CreatedBy"].ToString(),
                            StarRating = (byte)Math.Round(Convert.ToDouble(reader["StarRating"]))
                        });
                    }
                }

                // Get notifications
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                SELECT *
                FROM notification
                WHERE user_id = @userId
                ORDER BY notification_created_at DESC";
                    command.Parameters.Add(new SqlParameter("@userId", userId));

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        notifications.Add(new Notification
                        {
                            NotificationId = reader["notification_id"].ToString(),
                            UserId = reader["user_id"].ToString(),
                            NotificationContent = reader["notification_content"].ToString(),
                            NotificationType = reader["notification_type"].ToString(),
                            NotificationCreatedAt = Convert.ToDateTime(reader["notification_created_at"])
                        });
                    }
                }
            }

            ViewBag.FullName = user.FullName ?? "Learner";
            ViewBag.UserPicture = string.IsNullOrEmpty(user.UserPicture)
                ? "~/lib/img/User-img/default_user.png"
                : user.UserPicture;

            var viewModel = new HomePageLearnerViewModel
            {
                FullName = user.FullName,
                UserPicture = user.UserPicture,
                CompletedCoursesCount = completedCoursesCount,
                TotalCoursesEnrolled = totalCoursesEnrolled,
                Achievements = achievements,
                Ranking = userRank,
                RecommendedCourses = recommendedCourses,
                Notifications = notifications
            };

            return View("~/Views/Home/HomePageLearner.cshtml", viewModel);
        }
    }
}