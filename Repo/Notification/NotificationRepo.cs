using BrainStormEra.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrainStormEra.Repo.Notification
{
    public class NotificationRepo
    {
        private readonly SwpMainContext _context;
        private readonly string _connectionString;

        public NotificationRepo(SwpMainContext context, IConfiguration configuration)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("SwpMainContext");
        }

        public List<Models.Notification> GetNotifications(string userId)
        {
            var notifications = new List<Models.Notification>();
            string sqlQuery = @"
        SELECT n.notification_id, n.user_id, n.notification_title, n.notification_content, n.notification_type, 
               n.notification_created_at, n.created_by, a.user_picture -- Fetch the avatar URL from the account table
        FROM notification n
        LEFT JOIN account a ON n.created_by = a.user_id -- Join the account table to get avatar_url
        WHERE n.user_id = @userId OR n.created_by = @userId
        ORDER BY n.notification_created_at DESC";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    command.Parameters.Add(new SqlParameter("@userId", userId));

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            notifications.Add(new Models.Notification
                            {
                                NotificationId = reader["notification_id"].ToString(),
                                UserId = reader["user_id"].ToString(),
                                NotificationTitle = reader["notification_title"].ToString(),
                                NotificationContent = reader["notification_content"].ToString(),
                                NotificationType = reader["notification_type"].ToString(),
                                NotificationCreatedAt = (DateTime)reader["notification_created_at"],
                                CreatedBy = reader["created_by"].ToString(),
                                CreatorImageUrl = reader["user_picture"] as string // Get avatar URL from account table
                            });
                        }
                    }
                }
            }

            return notifications;
        }



        public List<object> GetUsers(string currentUserId)
        {
            var users = new List<object>();
            string sqlQuery = @"
                SELECT user_id, full_name 
                FROM account 
                WHERE user_id != @currentUserId";

            using (var connection = new SqlConnection(_connectionString))
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

            return users;
        }

        public int GetNextNotificationIdNumber()
        {
            string idQuery = "SELECT ISNULL(MAX(CAST(SUBSTRING(notification_id, 2, LEN(notification_id) - 1) AS INT)), 0) + 1 FROM notification WHERE notification_id LIKE 'N%'";
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = idQuery;
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public void CreateNotification(Models.Notification notification)
        {
            string insertQuery = @"
        INSERT INTO notification (notification_id, user_id, notification_title, notification_content, notification_type, notification_created_at, created_by)
        VALUES (@NotificationId, @UserId, @Title, @Content, @Type, @CreatedAt, @CreatedBy)";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = insertQuery;
                    command.Parameters.Add(new SqlParameter("@NotificationId", notification.NotificationId));
                    command.Parameters.Add(new SqlParameter("@UserId", notification.UserId));
                    command.Parameters.Add(new SqlParameter("@Title", notification.NotificationTitle));
                    command.Parameters.Add(new SqlParameter("@Content", notification.NotificationContent));
                    command.Parameters.Add(new SqlParameter("@Type", notification.NotificationType));
                    command.Parameters.Add(new SqlParameter("@CreatedAt", notification.NotificationCreatedAt));
                    command.Parameters.Add(new SqlParameter("@CreatedBy", notification.CreatedBy));

                    command.ExecuteNonQuery();
                }
            }
        }


        public Models.Notification GetNotificationById(string id)
        {
            Models.Notification notification = null;
            string sqlQuery = "SELECT notification_id, notification_title, notification_content, notification_type FROM notification WHERE notification_id = @id";

            using (var connection = new SqlConnection(_connectionString))
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
                            notification = new Models.Notification
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

            return notification;
        }

        public bool UpdateNotification(Models.Notification updatedNotification)
        {
            string sqlQuery = @"
                UPDATE notification 
                SET notification_title = @Title, 
                    notification_content = @Content, 
                    notification_type = @Type 
                WHERE notification_id = @Id";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    command.Parameters.Add(new SqlParameter("@Title", updatedNotification.NotificationTitle));
                    command.Parameters.Add(new SqlParameter("@Content", updatedNotification.NotificationContent));
                    command.Parameters.Add(new SqlParameter("@Type", updatedNotification.NotificationType));
                    command.Parameters.Add(new SqlParameter("@Id", updatedNotification.NotificationId));

                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool DeleteNotification(string id)
        {
            string sqlQuery = "DELETE FROM notification WHERE notification_id = @id";
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    command.Parameters.Add(new SqlParameter("@id", id));
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool DeleteSelectedNotifications(string[] ids)
        {
            string sqlQuery = "DELETE FROM notification WHERE notification_id IN (" + string.Join(",", ids.Select((_, i) => $"@id{i}")) + ")";
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    for (int i = 0; i < ids.Length; i++)
                    {
                        command.Parameters.Add(new SqlParameter($"@id{i}", ids[i]));
                    }
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
