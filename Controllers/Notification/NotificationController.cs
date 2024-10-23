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

namespace BrainStormEra.Controllers
{
    public class NotificationController : Controller
    {
        private readonly SwpDb7Context _context;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(SwpDb7Context context, ILogger<NotificationController> logger)
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
                return Unauthorized(); // Nếu người dùng chưa đăng nhập, trả về Unauthorized
            }


            var notifications = _context.Notifications
                                        .Where(n => n.UserId == currentUserId || n.CreatedBy == currentUserId)
                                        .OrderByDescending(n => n.NotificationCreatedAt)
                                        .ToList();

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
            // Lấy user_id của người đang đăng nhập từ session
            var currentUserId = Request.Cookies["user_id"];

            // Lấy danh sách user từ cơ sở dữ liệu, loại bỏ người đang đăng nhập hiện tại
            var users = _context.Accounts
                                .Where(u => u.UserId != currentUserId)  // Loại bỏ user hiện tại
                                .Select(u => new
                                {
                                    user_id = u.UserId,    // Đảm bảo tên trường khớp với tên trong bảng cơ sở dữ liệu
                                    full_name = u.FullName // Đảm bảo tên trường khớp với tên trong bảng cơ sở dữ liệu
                                })
                                .ToList();

            // Trả về dữ liệu dưới dạng JSON
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

            try
            {
                var createdBy = Request.Cookies["user_id"];

                var existingIds = _context.Notifications
                                          .Select(n => n.NotificationId)
                                          .Where(id => id.StartsWith("N"))
                                          .Select(id => int.Parse(id.Substring(1)))
                                          .ToList();

                int nextIdNumber = existingIds.Count > 0 ? existingIds.Max() + 1 : 1;

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
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating notification: " + ex.Message });
            }
        }


        [HttpGet]
        public IActionResult GetNotificationById(string id)
        {
            var notification = _context.Notifications.FirstOrDefault(n => n.NotificationId == id);
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
            var notification = _context.Notifications.FirstOrDefault(n => n.NotificationId == updatedNotification.NotificationId);
            if (notification == null)
            {
                return Json(new { success = false, message = "Notification not found." });
            }

            // Cập nhật thông tin Notification
            notification.NotificationTitle = updatedNotification.NotificationTitle;
            notification.NotificationContent = updatedNotification.NotificationContent;
            notification.NotificationType = updatedNotification.NotificationType;

            _context.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteNotification(string id)
        {
            var notification = _context.Notifications.FirstOrDefault(n => n.NotificationId == id);

            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                _context.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Notification not found." });
        }

        [HttpPost]
        public IActionResult DeleteSelectedNotifications(string[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return Json(new { success = false, message = "No notification IDs provided." });
            }

            var notifications = _context.Notifications
                .Where(n => ids.Contains(n.NotificationId))
                .ToList();

            if (notifications.Any())
            {
                _context.Notifications.RemoveRange(notifications);
                _context.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "No notifications found to delete." });
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
