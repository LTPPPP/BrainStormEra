using BrainStormEra.Models;
using BrainStormEra.Repo.Notification;
using BrainStormEra.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

namespace BrainStormEra.Controllers
{
    public class NotificationController : Controller
    {
        private readonly NotificationRepo _notificationRepo;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(NotificationRepo notificationRepo, ILogger<NotificationController> logger)
        {
            _notificationRepo = notificationRepo;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Notifications()
        {
            var currentUserId = Request.Cookies["user_id"];
            _logger.LogInformation("Current UserId from cookie: {UserId}", currentUserId);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var notifications = _notificationRepo.GetNotifications(currentUserId);
            _logger.LogInformation("Number of notifications for user {currentUserId}: {count}", currentUserId, notifications.Count);

            return PartialView("~/Views/Home/Notification/_NotificationsModal.cshtml", notifications);
        }

        public JsonResult GetUsers()
        {
            var currentUserId = Request.Cookies["user_id"];
            var users = _notificationRepo.GetUsers(currentUserId);
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
            int nextIdNumber = _notificationRepo.GetNextNotificationIdNumber();

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

                _notificationRepo.CreateNotification(newNotification);
                nextIdNumber++;
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult GetNotificationById(string id)
        {
            var notification = _notificationRepo.GetNotificationById(id);
            if (notification == null)
            {
                return NotFound();
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    notification.NotificationId,
                    notification.NotificationTitle,
                    notification.NotificationContent,
                    notification.NotificationType
                }
            });
        }

        [HttpPost]
        public IActionResult EditNotification([FromBody] Notification updatedNotification)
        {
            bool success = _notificationRepo.UpdateNotification(updatedNotification);
            return Json(new { success, message = success ? "" : "Notification not found." });
        }

        [HttpPost]
        public IActionResult DeleteNotification(string id)
        {
            bool success = _notificationRepo.DeleteNotification(id);
            return Json(new { success, message = success ? "" : "Notification not found." });
        }

        [HttpPost]
        public IActionResult DeleteSelectedNotifications(string[] ids)
        {
            bool success = _notificationRepo.DeleteSelectedNotifications(ids);
            return Json(new { success, message = success ? "" : "No notifications found to delete." });
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
