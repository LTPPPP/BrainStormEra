using BrainStormEra.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrainStormEra.Repo.Notification
{
    public class NotificationRepo
    {
        private readonly SwpMainContext _context;

        public NotificationRepo(SwpMainContext context)
        {
            _context = context;
        }

        public List<Models.Notification> GetNotifications(string userId)
        {
            return _context.Notifications
                .Where(n => n.UserId == userId || n.CreatedBy == userId)
                .OrderByDescending(n => n.NotificationCreatedAt)
                .Select(n => new Models.Notification
                {
                    NotificationId = n.NotificationId,
                    UserId = n.UserId,
                    NotificationTitle = n.NotificationTitle,
                    NotificationContent = n.NotificationContent,
                    NotificationType = n.NotificationType,
                    NotificationCreatedAt = n.NotificationCreatedAt,
                    CreatedBy = n.CreatedBy,
                    CreatorImageUrl = n.CreatedByNavigation.UserPicture
                })
                .ToList();
        }

        public List<object> GetUsers(string currentUserId)
        {
            return _context.Accounts
                .Where(a => a.UserId != currentUserId)
                .Select(a => new
                {
                    user_id = a.UserId,
                    full_name = a.FullName
                })
                .ToList<object>();
        }

        public int GetNextNotificationIdNumber()
        {
            var lastNotification = _context.Notifications
                .Where(n => n.NotificationId.StartsWith("N"))
                .OrderByDescending(n => n.NotificationId)
                .FirstOrDefault();

            if (lastNotification == null)
            {
                return 1;
            }

            int lastNumber = int.Parse(lastNotification.NotificationId.Substring(1));
            return lastNumber + 1;
        }

        public void CreateNotification(Models.Notification notification)
        {
            _context.Notifications.Add(notification);
            _context.SaveChanges();
        }

        public Models.Notification GetNotificationById(string id)
        {
            return _context.Notifications
                .Where(n => n.NotificationId == id)
                .Select(n => new Models.Notification
                {
                    NotificationId = n.NotificationId,
                    NotificationTitle = n.NotificationTitle,
                    NotificationContent = n.NotificationContent,
                    NotificationType = n.NotificationType
                })
                .FirstOrDefault();
        }

        public bool UpdateNotification(Models.Notification updatedNotification)
        {
            var notification = _context.Notifications.Find(updatedNotification.NotificationId);
            if (notification == null)
            {
                return false;
            }

            notification.NotificationTitle = updatedNotification.NotificationTitle;
            notification.NotificationContent = updatedNotification.NotificationContent;
            notification.NotificationType = updatedNotification.NotificationType;

            _context.SaveChanges();
            return true;
        }

        public bool DeleteNotification(string id)
        {
            var notification = _context.Notifications.Find(id);
            if (notification == null)
            {
                return false;
            }

            _context.Notifications.Remove(notification);
            _context.SaveChanges();
            return true;
        }

        public bool DeleteSelectedNotifications(string[] ids)
        {
            var notifications = _context.Notifications.Where(n => ids.Contains(n.NotificationId)).ToList();
            if (notifications.Count == 0)
            {
                return false;
            }

            _context.Notifications.RemoveRange(notifications);
            _context.SaveChanges();
            return true;
        }
    }
}
