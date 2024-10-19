using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace BrainStormEra.Controllers
{
    public class HomePageLearnerController : Controller
    {
        private readonly SwpDb7Context _dbContext;
        private readonly ILogger<HomePageLearnerController> _logger;

        public HomePageLearnerController(SwpDb7Context dbContext, ILogger<HomePageLearnerController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // Trang HomePageLearner
        public IActionResult HomePageLearner()
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
                    FullName = user?.FullName ?? "Learner",
                    UserPicture = string.IsNullOrEmpty(user?.UserPicture) ? "~/lib/img/User-img/default_user.png" : user.UserPicture
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in HomePageLearner action.");
                return RedirectToAction("Error", "Home");
            }
        }
    }
}
