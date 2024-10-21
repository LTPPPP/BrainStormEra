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

        [HttpGet]
        public async Task<IActionResult> Achievements()
        {
            // Retrieve userId from cookies
            var userId = Request.Cookies["userId"];

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LoginPage", "Login");
            }

            // Fetch user information based on userId
            var user = await _context.Accounts.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return RedirectToAction("LoginPage", "Login");
            }

            switch (user.UserRole)
            {
                case 3: // Learner
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
                    return View("LearnerAchievements");

                case 1: 
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
                    return View("AdminAchievements");

                default:
                    return RedirectToAction("LoginPage", "Login");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddAchievement(string achievementId, string achievementName, string achievementDescription, string achievementIcon, DateTime achievementCreatedAt)
        {
            if (string.IsNullOrEmpty(achievementId) || string.IsNullOrEmpty(achievementName))
            {
                return Json(new { success = false, message = "Achievement ID and Name are required." });
            }

            var existingAchievement = await _context.Achievements.FirstOrDefaultAsync(a => a.AchievementId == achievementId);
            if (existingAchievement != null)
            {
                return Json(new { success = false, message = "Achievement ID already exists!" });
            }

            var newAchievement = new Models.Achievement
            {
                AchievementId = achievementId,
                AchievementName = achievementName,
                AchievementDescription = achievementDescription,
                AchievementIcon = achievementIcon,
                AchievementCreatedAt = achievementCreatedAt
            };

            await _context.Achievements.AddAsync(newAchievement);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Achievement added successfully!" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAchievement(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId))
            {
                return Json(new { success = false, message = "Achievement ID is required." });
            }

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
            if (string.IsNullOrEmpty(achievementId))
            {
                return Json(new { success = false, message = "Achievement ID is required." });
            }

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
                if (string.IsNullOrEmpty(achievementId))
                {
                    return Json(new { success = false, message = "Achievement ID is required." });
                }

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
    }
}
