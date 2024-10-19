using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace BrainStormEra.Controllers
{
    public class HomePageInstructorController : Controller
    {
        private readonly SwpDb7Context _dbContext;
        private readonly ILogger<HomePageInstructorController> _logger;

        public HomePageInstructorController(SwpDb7Context dbContext, ILogger<HomePageInstructorController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // Trang HomePageInstructor
        public IActionResult HomePageInstructor()
        {
            try
            {
                var userId = HttpContext.Session.GetString("user_id");

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Session does not contain user_id.");
                    return RedirectToAction("LoginPage", "Login");
                }

                var user = _dbContext.Accounts.FirstOrDefault(u => u.UserId == userId);

                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found.");
                    return RedirectToAction("LoginPage", "Login");
                }

                // Nếu user không có hình ảnh, sử dụng hình ảnh mặc định
                var imagePath = string.IsNullOrEmpty(user.UserPicture) ? "~/lib/img/User-img/default_user.png" : user.UserPicture;

                var model = new Account
                {
                    FullName = user?.FullName ?? "Instructor",
                    UserPicture = string.IsNullOrEmpty(user?.UserPicture) ? "~/lib/img/User-img/default_user.png" : user.UserPicture
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in HomePageInstructor action.");
                return RedirectToAction("Error", "Home");
            }
        }
    }
}
