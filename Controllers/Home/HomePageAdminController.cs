using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using System.Security.Claims;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace BrainStormEra.Controllers
{
    public class HomePageAdminController : Controller
    {
        private readonly SwpMainContext _context;
        private readonly string _connectionString;

        public HomePageAdminController(SwpMainContext context, IConfiguration configuration)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("SwpMainContext");
        }

        [HttpGet]
        public IActionResult HomepageAdmin()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = User.Identity.Name;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userId != null)
                {
                    var user = _context.Accounts.FirstOrDefault(u => u.UserId == userId);

                    if (user != null)
                    {
                        ViewBag.FullName = user.FullName;
                        ViewBag.UserPicture = user.UserPicture ?? "~/lib/img/User-img/default_user.png";
                    }
                    else
                    {
                        ViewBag.FullName = "Guest";
                        ViewBag.UserPicture = "~/lib/img/User-img/default_user.png";
                    }

                    return View("~/Views/Home/HomePageAdmin.cshtml");
                }
            }

            return RedirectToAction("LoginPage", "Login");
        }

        [HttpGet]
        public IActionResult GetUserStatistics()
        {
            try
            {
                var firstDate = _context.Accounts.Min(u => u.AccountCreatedAt).Date;
                var currentDate = DateTime.Now.Date;

                var userStatistics = _context.Accounts
                    .GroupBy(u => u.AccountCreatedAt.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToList();

                var fullStatistics = Enumerable.Range(0, (currentDate - firstDate).Days + 1)
                    .Select(offset => new
                    {
                        Date = firstDate.AddDays(offset),
                        Count = userStatistics.FirstOrDefault(d => d.Date == firstDate.AddDays(offset))?.Count ?? 0
                    })
                    .ToList();

                return Json(fullStatistics.Select(d => new { Date = d.Date.ToString("yyyy-MM-dd"), Count = d.Count }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user statistics: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving user statistics." });
            }
        }

        [HttpGet]
        public JsonResult GetConversationStatistics()
        {
            try
            {
                var firstDate = _context.ChatbotConversations.Min(c => c.ConversationTime.Date);
                var currentDate = DateTime.Now.Date;

                var statistics = _context.ChatbotConversations
                    .GroupBy(c => c.ConversationTime.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToList();

                var allConversations = Enumerable.Range(0, (currentDate - firstDate).Days + 1)
                    .Select(offset => new
                    {
                        Date = firstDate.AddDays(offset),
                        Count = statistics.FirstOrDefault(d => d.Date == firstDate.AddDays(offset))?.Count ?? 0
                    })
                    .ToList();

                return Json(allConversations.Select(d => new { Date = d.Date.ToString("yyyy-MM-dd"), Count = d.Count }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching conversation statistics: {ex.Message}");
                return Json(new { message = "An error occurred while retrieving conversation statistics." });
            }
        }
        [HttpGet]
        public JsonResult GetCourseCreationStatistics()
        {
            try
            {
                // Kiểm tra courses
                if (!_context.Courses.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "No courses found",
                        data = new List<object>()
                    });
                }

                // Lấy ngày hiện tại
                var currentDate = DateTime.Now.Date;

                // Query courses trong phạm vi hợp lệ (quá khứ đến hiện tại)
                var courses = _context.Courses
                    .AsNoTracking()
                    .Where(c => c.CourseCreatedAt.Date <= currentDate)
                    .Select(c => new { c.CourseId, c.CourseCreatedAt })
                    .ToList();

                // Group by date và đếm
                var coursesByDate = courses
                    .GroupBy(c => c.CourseCreatedAt.Date)
                    .Select(g => new {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                // Lấy ngày bắt đầu từ dữ liệu thực tế
                var firstDate = coursesByDate.Any()
                    ? coursesByDate.Min(x => x.Date)
                    : currentDate.AddDays(-30); // Default 30 ngày nếu không có data

                // Tạo range date hợp lệ
                var allDates = Enumerable.Range(0, (currentDate - firstDate).Days + 1)
                    .Select(offset => firstDate.AddDays(offset))
                    .ToList();

                // Tạo dataset đầy đủ
                var fullData = allDates.Select(date => new {
                    Date = date.ToString("yyyy-MM-dd"),
                    Count = coursesByDate.FirstOrDefault(x => x.Date == date)?.Count ?? 0
                }).ToList();

                // Debug logging
                Console.WriteLine($"Data points: {fullData.Count}");
                Console.WriteLine($"Date range: {firstDate:yyyy-MM-dd} to {currentDate:yyyy-MM-dd}");
                Console.WriteLine($"Total courses: {courses.Count}");

                return Json(new
                {
                    success = true,
                    data = fullData,
                    totalCourses = courses.Count,
                    dateRange = new
                    {
                        start = firstDate.ToString("yyyy-MM-dd"),
                        end = currentDate.ToString("yyyy-MM-dd")
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCourseCreationStatistics: {ex.Message}");
                return Json(new
                {
                    success = false,
                    error = ex.Message,
                    data = new List<object>()
                });
            }
        }

    }
}
