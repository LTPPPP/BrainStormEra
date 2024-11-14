using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;

namespace BrainStormEra.Controllers
{
    public class NotificationController : Controller
    {
        private readonly SwpMainContext _context;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(SwpMainContext context, ILogger<NotificationController> logger)
        {
            _context = context;
            _logger = logger;
        }
        [HttpGet]
        public IActionResult Notifications()
        {
            var currentUserId = Request.Cookies["user_id"];
            var currentUserRole = Request.Cookies["user_role"];

            _logger.LogInformation("Current UserId from cookie: {UserId}", currentUserId);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(); // If the user is not logged in, return Unauthorized
            }

            List<Notification> notifications = new List<Notification>();

            // Raw SQL query to fetch notifications
            string sqlQuery = @"
                SELECT * 
                FROM notification 
                WHERE user_id = @currentUserId 
                OR created_by = @currentUserId 
                ORDER BY notification_created_at DESC";

            using (var connection = _context.Database.GetDbConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    command.Parameters.Add(new SqlParameter("@currentUserId", currentUserId));

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            notifications.Add(new Notification
                            {
                                NotificationId = reader["notification_id"].ToString(),
                                UserId = reader["user_id"].ToString(),
                                CourseId = reader["course_id"] as string,
                                NotificationTitle = reader["notification_title"].ToString(),
                                NotificationContent = reader["notification_content"].ToString(),
                                NotificationType = reader["notification_type"].ToString(),
                                NotificationCreatedAt = (DateTime)reader["notification_created_at"],
                                CreatedBy = reader["created_by"].ToString()
                            });
                        }
                    }
                }
            }

            foreach (var notification in notifications)
            {
                _logger.LogInformation("Notification: {NotificationId}, CreatedBy: {CreatedBy}, SentTo: {UserId}",
                                       notification.NotificationId, notification.CreatedBy, notification.UserId);
            }

            _logger.LogInformation("Number of notifications for user {currentUserId}: {count}", currentUserId, notifications.Count);

            return PartialView("~/Views/Home/Notification/_NotificationsModal.cshtml", notifications);
        }



        public JsonResult GetUsers()
        {
            var currentUserId = Request.Cookies["user_id"];
            var users = new List<object>();

            string sqlQuery = @"
        SELECT user_id, full_name 
        FROM account 
        WHERE user_id != @currentUserId";

            using (var connection = _context.Database.GetDbConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    command.Parameters.Add(new SqlParameter("@currentUserId", currentUserId));

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new
                            {
                                user_id = reader["user_id"].ToString(),
                                full_name = reader["full_name"].ToString()
                            });
                        }
                    }
                }
            }

            return Json(users);
        }


        [HttpPost]
        public IActionResult CreateNotification([FromBody] NotificationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = string.Join(", ", errors) });
            }

            var createdBy = Request.Cookies["user_id"];
            int nextIdNumber;

            using (var connection = _context.Database.GetDbConnection())
            {
                connection.Open();

                string idQuery = "SELECT ISNULL(MAX(CAST(SUBSTRING(notification_id, 2, LEN(notification_id) - 1) AS INT)), 0) + 1 FROM notification WHERE notification_id LIKE 'N%'";
                using (var idCommand = connection.CreateCommand())
                {
                    idCommand.CommandText = idQuery;
                    nextIdNumber = Convert.ToInt32(idCommand.ExecuteScalar());
                }

                foreach (var userId in model.UserIds)
                {
                    string newNotificationId = "N" + nextIdNumber.ToString().PadLeft(2, '0');

                    string insertQuery = @"
                INSERT INTO notification (notification_id, user_id, notification_title, notification_content, notification_type, notification_created_at, created_by)
                VALUES (@NotificationId, @UserId, @Title, @Content, @Type, @CreatedAt, @CreatedBy)";

                    using (var insertCommand = connection.CreateCommand())
                    {
                        insertCommand.CommandText = insertQuery;
                        insertCommand.Parameters.Add(new SqlParameter("@NotificationId", newNotificationId));
                        insertCommand.Parameters.Add(new SqlParameter("@UserId", userId));
                        insertCommand.Parameters.Add(new SqlParameter("@Title", model.NotificationTitle));
                        insertCommand.Parameters.Add(new SqlParameter("@Content", model.NotificationContent));
                        insertCommand.Parameters.Add(new SqlParameter("@Type", model.NotificationType));
                        insertCommand.Parameters.Add(new SqlParameter("@CreatedAt", DateTime.Now));
                        insertCommand.Parameters.Add(new SqlParameter("@CreatedBy", createdBy));

                        insertCommand.ExecuteNonQuery();
                    }

                    nextIdNumber++;
                }
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult GetNotificationById(string id)
        {
            object notification = null;

            string sqlQuery = "SELECT notification_id, notification_title, notification_content, notification_type FROM notification WHERE notification_id = @id";

            using (var connection = _context.Database.GetDbConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    command.Parameters.Add(new SqlParameter("@id", id));

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            notification = new
                            {
                                NotificationId = reader["notification_id"].ToString(),
                                NotificationTitle = reader["notification_title"].ToString(),
                                NotificationContent = reader["notification_content"].ToString(),
                                NotificationType = reader["notification_type"].ToString()
                            };
                        }
                    }
                }
            }

            if (notification == null)
            {
                return NotFound();
            }

            return Json(new { success = true, data = notification });
        }


        [HttpPost]
        public IActionResult EditNotification([FromBody] Notification updatedNotification)
        {
            string sqlQuery = @"
        UPDATE notification 
        SET notification_title = @Title, 
            notification_content = @Content, 
            notification_type = @Type 
        WHERE notification_id = @Id";

            using (var connection = _context.Database.GetDbConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    command.Parameters.Add(new SqlParameter("@Title", updatedNotification.NotificationTitle));
                    command.Parameters.Add(new SqlParameter("@Content", updatedNotification.NotificationContent));
                    command.Parameters.Add(new SqlParameter("@Type", updatedNotification.NotificationType));
                    command.Parameters.Add(new SqlParameter("@Id", updatedNotification.NotificationId));

                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        return Json(new { success = false, message = "Notification not found." });
                    }
                }
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteNotification(string id)
        {
            string sqlQuery = "DELETE FROM notification WHERE notification_id = @id";

            using (var connection = _context.Database.GetDbConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    command.Parameters.Add(new SqlParameter("@id", id));

                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        return Json(new { success = false, message = "Notification not found." });
                    }
                }
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteSelectedNotifications(string[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return Json(new { success = false, message = "No notification IDs provided." });
            }

            string sqlQuery = "DELETE FROM notification WHERE notification_id IN (" + string.Join(",", ids.Select((_, i) => $"@id{i}")) + ")";

            using (var connection = _context.Database.GetDbConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;

                    for (int i = 0; i < ids.Length; i++)
                    {
                        command.Parameters.Add(new SqlParameter($"@id{i}", ids[i]));
                    }

                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        return Json(new { success = false, message = "No notifications found to delete." });
                    }
                }
            }

            return Json(new { success = true });
        }


        public class NotificationViewModel
        {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.None)]

            public List<string> UserIds { get; set; }
            public string NotificationTitle { get; set; }
            public string NotificationContent { get; set; }
            public string NotificationType { get; set; }
            public DateTime NotificationCreatedAt { get; set; } = DateTime.Now;
        }
    }
}
