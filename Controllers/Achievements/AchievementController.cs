using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using BrainStormEra.Models;

namespace BrainStormEra.Controllers.Achievement
{
    public class AchievementController : Controller
    {
        private readonly AchievementRepo _achievementRepo;

        public AchievementController(SwpMainContext context)
        {
            _achievementRepo = new AchievementRepo(context);
        }

        // Display learner's achievements
        public async Task<IActionResult> LearnerAchievements()
        {
            var userId = Request.Cookies["user_id"];
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LoginPage", "Login");
            }

            var learnerAchievements = await _achievementRepo.GetLearnerAchievements(userId);

            ViewData["UserId"] = userId;
            ViewData["Achievements"] = learnerAchievements;

            return View("~/Views/Achievements/LearnerAchievements.cshtml");
        }

        // Get achievement details via AJAX
        [HttpGet]
        public async Task<IActionResult> GetAchievementDetails(string achievementId, string userId)
        {
            if (string.IsNullOrEmpty(achievementId) || string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Invalid achievementId or userId" });
            }

            var achievement = await _achievementRepo.GetAchievementDetails(achievementId, userId);

            if (achievement == null)
            {
                return Json(new { success = false, message = "Achievement not found" });
            }

            return Json(new { success = true, data = achievement });
        }
    }
}
