using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using Microsoft.Extensions.Logging;

namespace BrainStormEra.Controllers
{
    public class HomePageLearnerController : Controller
    {
        private readonly SwpMainFpContext _dbContext;
        private readonly ILogger<HomePageLearnerController> _logger;

        public HomePageLearnerController(SwpMainFpContext dbContext, ILogger<HomePageLearnerController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult HomePageLearner()
        {
            var userId = Request.Cookies["user_id"];

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Cookies do not contain user_id.");
                return RedirectToAction("LoginPage", "Login");
            }

            var user = _dbContext.Accounts.FirstOrDefault(u => u.UserId.ToString() == userId);

            if (user == null)
            {
                _logger.LogWarning($"User with ID {userId} not found.");
                return RedirectToAction("LoginPage", "Login");
            }

            // Set data in ViewBag
            ViewBag.FullName = user?.FullName ?? "Learner";
            ViewBag.UserPicture = string.IsNullOrEmpty(user?.UserPicture) ? "~/lib/img/User-img/default_user.png" : user.UserPicture;

            return View("~/Views/Home/HomePageLearner.cshtml");
        }
    }
}
