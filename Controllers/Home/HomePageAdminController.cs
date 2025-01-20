using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using BrainStormEra.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> HomepageAdmin()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != null)
                {
                    var user = await _context.Accounts
                        .Where(a => a.UserId == userId)
                        .Select(a => new
                        {
                            a.FullName,
                            UserPicture = a.UserPicture ?? "~/lib/img/User-img/default_user.png"
                        })
                        .FirstOrDefaultAsync();

                    if (user != null)
                    {
                        ViewBag.FullName = user.FullName;
                        ViewBag.UserPicture = user.UserPicture;
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
        public async Task<IActionResult> GetUserStatistics()
        {
            try
            {
                var userStatistics = await _context.Accounts
                    .GroupBy(a => a.AccountCreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        Count = g.Count()
                    })
                    .ToListAsync();

                return Json(userStatistics);
            }
            catch
            {
                return StatusCode(500, new { message = "An error occurred while retrieving user statistics." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetConversationStatistics()
        {
            try
            {
                var conversationStatistics = await _context.ChatbotConversations
                    .GroupBy(c => c.ConversationTime.Date)
                    .Select(g => new
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        Count = g.Count()
                    })
                    .ToListAsync();

                return Json(conversationStatistics);
            }
            catch
            {
                return Json(new { message = "An error occurred while retrieving conversation statistics." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCourseCreationStatistics()
        {
            try
            {
                var courseCount = await _context.Courses.CountAsync();
                if (courseCount == 0)
                {
                    return Json(new { success = false, message = "No courses found", data = new List<object>() });
                }

                var courseStatistics = await _context.Courses
                    .Where(c => c.CourseCreatedAt <= DateTime.Now)
                    .GroupBy(c => c.CourseCreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        Count = g.Count()
                    })
                    .ToListAsync();

                var result = new
                {
                    success = true,
                    data = courseStatistics,
                    totalCourses = courseStatistics.Count,
                    dateRange = new
                    {
                        start = courseStatistics.Count > 0 ? courseStatistics[0].Date : "N/A",
                        end = DateTime.Now.ToString("yyyy-MM-dd")
                    }
                };

                return Json(result);
            }
            catch
            {
                return Json(new { message = "An error occurred while retrieving course statistics." });
            }
        }
    }
}
