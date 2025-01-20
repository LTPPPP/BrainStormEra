using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

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
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var notifications = _context.Notifications
                .Where(n => n.UserId == currentUserId || n.CreatedBy == currentUserId)
                .OrderByDescending(n => n.NotificationCreatedAt)
                .Select(n => new
                {
                    n.NotificationId,
                    n.UserId,
                    n.NotificationTitle,
                    n.NotificationContent,
                    n.NotificationType,
                    n.NotificationCreatedAt,
                    n.CreatedBy,
                    CreatorImageUrl = n.CreatedByNavigation.UserPicture
                })
                .ToList();

            // Kiểm tra thông báo mới dựa trên thời gian lần cuối người dùng xem
            var lastViewed = HttpContext.Session.GetString("LastViewedNotifications") ?? DateTime.MinValue.ToString();
            var lastViewedDate = DateTime.Parse(lastViewed);

            var newNotificationsCount = notifications.Count(n => n.NotificationCreatedAt > lastViewedDate);
            ViewBag.NewNotificationsCount = newNotificationsCount;

            return PartialView("~/Views/Home/Notification/_NotificationsModal.cshtml", notifications);
        }

        [HttpPost]
        public IActionResult MarkNotificationsAsViewed()
        {
            HttpContext.Session.SetString("LastViewedNotifications", DateTime.Now.ToString());
            return Json(new { success = true });
        }

        public JsonResult GetUsers()
        {
            var currentUserId = Request.Cookies["user_id"];
            var users = _context.Accounts
                .Where(a => a.UserId != currentUserId)
                .Select(a => new
                {
                    user_id = a.UserId,
                    full_name = a.FullName
                })
                .ToList();
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
            int nextIdNumber = GetNextNotificationIdNumber();

            foreach (var userId in model.UserIds)
            {
                string newNotificationId = "N" + nextIdNumber.ToString().PadLeft(2, '0');

                var newNotification = new Notification
                {
                    NotificationId = newNotificationId,
                    UserId = userId,
                    NotificationTitle = model.NotificationTitle,
                    NotificationContent = model.NotificationContent,
                    NotificationType = model.NotificationType,
                    NotificationCreatedAt = DateTime.Now,
                    CreatedBy = createdBy
                };

                _context.Notifications.Add(newNotification);
                nextIdNumber++;
            }

            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult GetNotificationById(string id)
        {
            var notification = _context.Notifications
                .Where(n => n.NotificationId == id)
                .Select(n => new
                {
                    n.NotificationId,
                    n.NotificationTitle,
                    n.NotificationContent,
                    n.NotificationType
                })
                .FirstOrDefault();

            if (notification == null)
            {
                return NotFound();
            }

            return Json(new
            {
                success = true,
                data = notification
            });
        }

        [HttpPost]
        public IActionResult EditNotification([FromBody] Notification updatedNotification)
        {
            var notification = _context.Notifications.Find(updatedNotification.NotificationId);
            if (notification == null)
            {
                return Json(new { success = false, message = "Notification not found." });
            }

            notification.NotificationTitle = updatedNotification.NotificationTitle;
            notification.NotificationContent = updatedNotification.NotificationContent;
            notification.NotificationType = updatedNotification.NotificationType;

            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteNotification(string id)
        {
            var notification = _context.Notifications.Find(id);
            if (notification == null)
            {
                return Json(new { success = false, message = "Notification not found." });
            }

            _context.Notifications.Remove(notification);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteSelectedNotifications(string[] ids)
        {
            var notifications = _context.Notifications.Where(n => ids.Contains(n.NotificationId)).ToList();
            if (notifications.Count == 0)
            {
                return Json(new { success = false, message = "No notifications found to delete." });
            }

            _context.Notifications.RemoveRange(notifications);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        private int GetNextNotificationIdNumber()
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

        public class NotificationViewModel
        {
            public List<string> UserIds { get; set; }
            public string NotificationTitle { get; set; }
            public string NotificationContent { get; set; }
            public string NotificationType { get; set; }
            public DateTime NotificationCreatedAt { get; set; } = DateTime.Now;
        }
    }
}
