using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BrainStormEra.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

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

                if (learnerAchievements == null || learnerAchievements.Count == 0)
                {
                    return NotFound(); // Return 404 error if no achievements found for the learner
                }

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

                if (allAchievements == null || allAchievements.Count == 0)
                {
                    return NotFound(); // Return 404 error if no achievements found for the admin
                }

                ViewData["UserId"] = userId;
                ViewData["Achievements"] = allAchievements;
                return View("~/Views/Achievements/AdminAchievements.cshtml");
            }

            return NotFound(); // Return 404 error if the user role is invalid or not recognized
        }

        [HttpPost]
        public async Task<IActionResult> AddAchievement(string achievementId, string achievementName, string achievementDescription, string achievementIcon, DateTime achievementCreatedAt)
        {
            var existingAchievement = await _context.Achievements.FirstOrDefaultAsync(a => a.AchievementId == achievementId);
            if (existingAchievement != null)
            {
                return Json(new { success = false, message = "Achievement ID already exists!" });
            }

            var newAchievement = new BrainStormEra.Models.Achievement
            {
                AchievementId = achievementId,
                AchievementName = achievementName,
                AchievementDescription = achievementDescription,
                AchievementIcon = achievementIcon,
                AchievementCreatedAt = achievementCreatedAt
            };

            _context.Achievements.Add(newAchievement);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Achievement added successfully!" });
        }

        // Get Achievement for Edit
        [HttpGet]
        public async Task<IActionResult> GetAchievement(string achievementId)
        {
            var achievement = await _context.Achievements.FirstOrDefaultAsync(a => a.AchievementId == achievementId);
            if (achievement == null)
            {
                return Json(new { success = false, message = "Achievement not found!" });
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    achievement.AchievementId,
                    achievement.AchievementName,
                    achievement.AchievementDescription,
                    achievement.AchievementIcon,
                    achievement.AchievementCreatedAt
                }
            });
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

            return Json(new { success = true, message = "Achievement updated successfully!" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAchievement([FromBody] JsonElement json)
        {
            try
            {
                var achievementId = json.GetProperty("achievementId").GetString();

                var achievement = await _context.Achievements.FirstOrDefaultAsync(a => a.AchievementId == achievementId);
                if (achievement == null)
                {
                    return Json(new { success = false, message = "Achievement not found!" });
                }

                _context.Achievements.Remove(achievement);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Achievement deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error occurred: {ex.Message}" });
            }
        }

        // Get Next Achievement ID (for auto increment)
        [HttpGet]
        public async Task<IActionResult> GetNextAchievementId()
        {
            var lastAchievement = await _context.Achievements
                .OrderByDescending(a => a.AchievementId)
                .FirstOrDefaultAsync();

            if (lastAchievement == null)
            {
                return Json(new { success = true, nextId = "A001" });
            }

            var nextIdNumber = int.Parse(lastAchievement.AchievementId.Substring(1)) + 1;
            var nextId = "A" + nextIdNumber.ToString("D3");

            return Json(new { success = true, nextId });
        }
    }
}

