using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using System.Security.Claims;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;

namespace BrainStormEra.Controllers
{
    public class HomePageAdminController : Controller
    {
        private readonly SwpMainContext _context;

        public HomePageAdminController(SwpMainContext context)
        {
            _context = context;
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
        public JsonResult GetCourseStatistics()
        {
            try
            {
                var firstDate = _context.Courses.Min(c => c.CourseCreatedAt).Date;
                var currentDate = DateTime.Now.Date;

                var statistics = _context.Courses
                    .GroupBy(c => new { c.CourseCreatedAt.Date, Status = c.CourseStatus })
                    .Select(g => new
                    {
                        Date = g.Key.Date,
                        Status = g.Key.Status,
                        Count = g.Count()
                    })
                    .ToList();

                var requests = Enumerable.Range(0, (currentDate - firstDate).Days + 1)
                    .Select(offset => new
                    {
                        Date = firstDate.AddDays(offset),
                        Count = statistics.FirstOrDefault(d => d.Date == firstDate.AddDays(offset) && d.Status == 1)?.Count ?? 0
                    })
                    .ToList();

                var approvals = Enumerable.Range(0, (currentDate - firstDate).Days + 1)
                    .Select(offset => new
                    {
                        Date = firstDate.AddDays(offset),
                        Count = statistics.FirstOrDefault(d => d.Date == firstDate.AddDays(offset) && d.Status == 4)?.Count ?? 0
                    })
                    .ToList();

                return Json(new { requests, approvals });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching course statistics: {ex.Message}");
                return Json(new { message = "An error occurred while retrieving course statistics." });
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

    }
}
