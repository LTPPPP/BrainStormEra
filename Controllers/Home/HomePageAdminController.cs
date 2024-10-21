using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using System.Security.Claims;

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
            // Check if the user is authenticated
            if (User.Identity.IsAuthenticated)
            {
                // Retrieve user ID and role from claims
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = User.Identity.Name;  // This retrieves the username stored as ClaimTypes.Name
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userId != null)
                {
                    // Fetch user details from the database
                    var user = _context.Accounts.FirstOrDefault(u => u.UserId == userId);

                    if (user != null)
                    {
                        ViewBag.FullName = user.FullName;
                        ViewBag.UserPicture = user.UserPicture ?? "~/lib/img/User-img/default_user.png"; // Use default image if no picture
                    }
                    else
                    {
                        // Handle case where user is not found
                        ViewBag.FullName = "Guest";
                        ViewBag.UserPicture = "~/lib/img/User-img/default_user.png";
                    }

                    return View("~/Views/Home/HomePageAdmin.cshtml");
                }
            }

            // Redirect to login if the user is not authenticated or cookies are not found
            return RedirectToAction("LoginPage", "Login");
        }
    }
}
