using BrainStormEra.Models;  // Thêm tham chiếu tới model
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
            // set cứng để test 
            //var userId = "IN01";

            var userId = HttpContext.Session.GetString("user_id");
            var userRole = HttpContext.Session.GetString("user_role");

            // Lọc thông báo theo UserId
            var notifications = _context.Notifications
                                        .Where(n => n.UserId == userId)
                                        .OrderByDescending(n => n.NotificationCreatedAt)
                                        .ToList();

            _logger.LogInformation("Number of notifications for user {userId}: {count}", userId, notifications.Count);

            // Truyền thêm UserId vào ViewBag để sử dụng trong View, dùng để test
            ViewBag.UserRole = userRole;

            return PartialView("_NotificationsModal", notifications);
        }

        public JsonResult GetUsers()
        {
            // Lấy user_id của người đang đăng nhập từ session
            var currentUserId = HttpContext.Session.GetString("user_id");

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
                // Lấy tất cả các NotificationId hiện tại, loại bỏ phần "N" và chuyển thành số nguyên
                var existingIds = _context.Notifications
                                          .Select(n => n.NotificationId)
                                          .Where(id => id.StartsWith("N"))
                                          .Select(id => int.Parse(id.Substring(1))) // Loại bỏ chữ "N" và lấy phần số
                                          .ToList();

                // Tìm số lớn nhất trong các ID hiện tại và cộng thêm 1 để tạo ID mới
                int nextIdNumber = existingIds.Count > 0 ? existingIds.Max() + 1 : 1;

                foreach (var userId in model.UserIds)
                {
                    // Tạo ID mới theo định dạng "N{number}"
                    string newNotificationId = "N" + nextIdNumber.ToString();

                    var newNotification = new Notification
                    {
                        NotificationId = newNotificationId,
                        UserId = userId,
                        NotificationTitle = model.NotificationTitle,
                        NotificationContent = model.NotificationContent,
                        NotificationType = model.NotificationType,
                        NotificationCreatedAt = DateTime.Now
                    };

                    _context.Notifications.Add(newNotification);
                    nextIdNumber++; // Tăng ID cho lần tạo kế tiếp
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
