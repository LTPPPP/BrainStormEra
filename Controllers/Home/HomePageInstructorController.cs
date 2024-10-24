using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using Microsoft.Extensions.Logging;

namespace BrainStormEra.Controllers
{
    public class HomePageInstructorController : Controller
    {
        private readonly SwpMainFpContext _dbContext;
        private readonly ILogger<HomePageInstructorController> _logger;

        public HomePageInstructorController(SwpMainFpContext dbContext, ILogger<HomePageInstructorController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult HomePageInstructor()
        {
            var userId = Request.Cookies["user_id"];

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LoginPage", "Login");
            }

            var account = _dbContext.Accounts.FirstOrDefault(a => a.UserId.ToString() == userId);

            if (account == null)
            {
                _logger.LogWarning($"User with ID {userId} not found.");
                return RedirectToAction("ErrorPage");
            }

            // Set data in ViewBag
            ViewBag.FullName = account.FullName;
            ViewBag.UserPicture = string.IsNullOrEmpty(account.UserPicture) ? "~/lib/img/User-img/default_user.png" : account.UserPicture;

            return View("~/Views/Home/HomePageInstructor.cshtml");
        }
    }
}
