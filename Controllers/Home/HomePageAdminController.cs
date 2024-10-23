using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using System.Security.Claims;
using System.Linq;

namespace BrainStormEra.Controllers
{
    public class HomePageAdminController : Controller
    {
        private readonly SwpDb7Context _context;

        public HomePageAdminController(SwpDb7Context context)
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
                var userStatistics = _context.Accounts
                    .Where(u => u.AccountCreatedAt != null)
                    .GroupBy(u => u.AccountCreatedAt.Value.Date)
                    .Select(g => new
                    {
                        date = g.Key,
                        count = g.Count()
                    })
                    .OrderBy(g => g.date)
                    .ToList();

                return Json(userStatistics);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user statistics: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving user statistics." });
            }
        }
    }
}
