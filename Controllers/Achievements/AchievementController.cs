using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BrainStormEra.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BrainStormEra.Controllers.Achievement
{
    public class AchievementController : Controller
    {
        private readonly SwpDb7Context _context;

        public AchievementController(SwpDb7Context context)
        {
            _context = context;
        }

        // Display achievements based on user role stored in cookies
        public async Task<IActionResult> AdminAchievements()
        {
            // Retrieve userId and userRole from cookies
            var userId = Request.Cookies["user_id"];
            var userRole = Request.Cookies["user_role"];

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
            {
                return RedirectToAction("LoginPage", "Login"); // Redirect to login if cookies are missing
            }

            userId = userId.ToUpper();

            if (userRole == "3") // Learner role
            {
                var learnerAchievements = await _context.UserAchievements
                    .Where(ua => ua.UserId == userId)
                    .Include(ua => ua.Achievement)
                    .Select(ua => new
                    {
                        ua.Achievement.AchievementId,
                        ua.Achievement.AchievementName,
                        ua.Achievement.AchievementDescription,
                        ua.Achievement.AchievementIcon,
                        ua.ReceivedDate
                    })
                    .ToListAsync();

                ViewData["UserId"] = userId;
                ViewData["Achievements"] = learnerAchievements;
                return View("~/Views/Achievements/LearnerAchievements.cshtml");
            }
            else if (userRole == "1") // Admin role
            {
                var allAchievements = await _context.Achievements
                    .Select(a => new
                    {
                        a.AchievementId,
                        a.AchievementName,
                        a.AchievementDescription,
                        a.AchievementIcon,
                        a.AchievementCreatedAt,
                        UserName = _context.UserAchievements
                            .Where(ua => ua.AchievementId == a.AchievementId)
                            .Select(ua => ua.User.FullName)
                            .FirstOrDefault() ?? "null"
                    })
                    .OrderBy(a => a.AchievementId)
                    .ToListAsync();

                ViewData["UserId"] = userId;
                ViewData["Achievements"] = allAchievements;
                return View("~/Views/Achievements/AdminAchievements.cshtml");
            }

            return NotFound(); // Return 404 error if the user role is invalid or not recognized
        }

        [HttpPost]
        public async Task<IActionResult> AddAchievement(string achievementName, string achievementDescription, string achievementIcon, DateTime achievementCreatedAt)
        {
            // Generate the AchievementId based on the last achievement
            var lastAchievement = await _context.Achievements.OrderByDescending(a => a.AchievementId).FirstOrDefaultAsync();
            var nextId = lastAchievement == null ? "A001" : $"A{int.Parse(lastAchievement.AchievementId.Substring(1)) + 1:D3}";

            var newAchievement = new BrainStormEra.Models.Achievement
            {
                AchievementId = nextId,
                AchievementName = achievementName,
                AchievementDescription = achievementDescription,
                AchievementIcon = achievementIcon,
                AchievementCreatedAt = achievementCreatedAt
            };

            _context.Achievements.Add(newAchievement);
            await _context.SaveChangesAsync();

            return RedirectToAction("AdminAchievements");
        }

        [HttpPost]
        public async Task<IActionResult> EditAchievement(string achievementId, string achievementName, string achievementDescription, string achievementIcon, DateTime achievementCreatedAt)
        {
            var achievement = await _context.Achievements.FirstOrDefaultAsync(a => a.AchievementId == achievementId);
            if (achievement == null)
            {
                return Json(new { success = false, message = "Achievement not found!" });
            }

            achievement.AchievementName = achievementName;
            achievement.AchievementDescription = achievementDescription;
            achievement.AchievementIcon = achievementIcon;
            achievement.AchievementCreatedAt = achievementCreatedAt;

            await _context.SaveChangesAsync();

            return RedirectToAction("AdminAchievements");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAchievement(string achievementId)
        {
            var achievement = await _context.Achievements.FirstOrDefaultAsync(a => a.AchievementId == achievementId);
            if (achievement == null)
            {
                return Json(new { success = false, message = "Achievement not found!" });
            }

            _context.Achievements.Remove(achievement);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Achievement deleted successfully!" });
        }
    }
}
