using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BrainStormEra.Controllers.Achievement
{
    public class AchievementController : Controller
    {
        private readonly SwpMainContext _context;

        public AchievementController(SwpMainContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> LearnerAchievements()
        {
            var userId = Request.Cookies["user_id"];
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LoginPage", "Login");
            }

            // Assign achievements based on completed courses
            await AssignAchievementsBasedOnCompletedCourses();

            // Retrieve the learner's achievements
            var learnerAchievements = await _context.UserAchievements
                .Where(ua => ua.UserId == userId)
                .Join(_context.Achievements, ua => ua.AchievementId, a => a.AchievementId, (ua, a) => new
                {
                    AchievementId = a.AchievementId,
                    AchievementName = a.AchievementName,
                    AchievementDescription = a.AchievementDescription,
                    AchievementIcon = a.AchievementIcon,
                    ReceivedDate = ua.ReceivedDate
                })
                .ToListAsync();

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

            var achievement = await _context.UserAchievements
                .Where(ua => ua.UserId == userId && ua.AchievementId == achievementId)
                .Join(_context.Achievements, ua => ua.AchievementId, a => a.AchievementId, (ua, a) => new
                {
                    AchievementName = a.AchievementName,
                    AchievementDescription = a.AchievementDescription,
                    AchievementIcon = a.AchievementIcon,
                    ReceivedDate = ua.ReceivedDate
                })
                .FirstOrDefaultAsync();

            if (achievement == null)
            {
                return Json(new { success = false, message = "Achievement not found" });
            }

            return Json(new { success = true, data = achievement });
        }

        // Display admin's achievements
        public async Task<IActionResult> AdminAchievements()
        {
            var userId = Request.Cookies["user_id"];
            var userRole = Request.Cookies["user_role"];

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
            {
                return RedirectToAction("LoginPage", "Login");
            }

            userId = userId.ToUpper();

            if (userRole == "1") // Admin role
            {
                var allAchievements = await _context.Achievements
                    .Select(a => new
                    {
                        AchievementId = a.AchievementId,
                        AchievementName = a.AchievementName,
                        AchievementDescription = a.AchievementDescription,
                        AchievementIcon = a.AchievementIcon,
                        AchievementCreatedAt = a.AchievementCreatedAt,
                        UserName = _context.UserAchievements
                            .Where(ua => ua.AchievementId == a.AchievementId)
                            .Join(_context.Accounts, ua => ua.UserId, acc => acc.UserId, (ua, acc) => acc.FullName)
                            .FirstOrDefault() ?? "null"
                    })
                    .ToListAsync();

                ViewData["UserId"] = userId;
                ViewData["Achievements"] = allAchievements;

                return View("~/Views/Achievements/AdminAchievements.cshtml");
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> AddAchievement(string achievementName, string achievementDescription, IFormFile achievementIcon)
        {
            // Check if the achievement name already exists to prevent duplicates
            if (await _context.Achievements.AnyAsync(a => a.AchievementName == achievementName))
            {
                return Json(new { success = false, message = "Achievement name already exists. Please choose a different name." });
            }

            // Get the list of all current conditions from the repository
            var allConditions = await _context.Achievements
                .Where(a => int.TryParse(a.AchievementDescription, out _))
                .Select(a => int.Parse(a.AchievementDescription))
                .ToListAsync();

            int conditionValue;

            // Convert achievementDescription to a number for validation
            if (!int.TryParse(achievementDescription, out conditionValue))
            {
                return Json(new { success = false, message = "Condition must be a valid number." });
            }

            // Ensure the condition is unique and greater than the maximum existing condition
            if (allConditions.Any())
            {
                int maxCondition = allConditions.Max();
                if (allConditions.Contains(conditionValue) || conditionValue <= maxCondition)
                {
                    return Json(new { success = false, message = $"Condition must be unique and greater than the current max condition of {maxCondition} courses." });
                }
            }
            else
            {
                // Handle case when there are no existing conditions
                if (allConditions.Contains(conditionValue))
                {
                    return Json(new { success = false, message = "Condition must be unique." });
                }
            }

            // Continue processing and add the achievement if all conditions are met
            var iconPath = await SaveAchievementIcon(achievementIcon);

            var newAchievement = new Achievement
            {
                AchievementId = GenerateAchievementId(),
                AchievementName = achievementName,
                AchievementDescription = conditionValue.ToString(),
                AchievementIcon = iconPath,
                AchievementCreatedAt = DateTime.Today
            };

            _context.Achievements.Add(newAchievement);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> CheckAchievementName(string achievementName, string achievementId = null)
        {
            var nameExists = await _context.Achievements
                .AnyAsync(a => a.AchievementName == achievementName && a.AchievementId != achievementId);
            return Json(new { success = !nameExists });
        }

        [HttpGet]
        public async Task<IActionResult> GetMaxCondition()
        {
            int maxCondition = await _context.Achievements
                .Where(a => int.TryParse(a.AchievementDescription, out _))
                .MaxAsync(a => int.Parse(a.AchievementDescription)) ?? 0;
            return Json(new { success = true, maxCondition });
        }

        [HttpPost]
        public async Task<IActionResult> EditAchievement(string achievementId, string achievementName, string achievementDescription, IFormFile achievementIcon, DateTime? achievementCreatedAt)
        {
            // Check if the achievement name already exists, excluding the current achievement's ID
            if (await _context.Achievements.AnyAsync(a => a.AchievementName == achievementName && a.AchievementId != achievementId))
            {
                return Json(new { success = false, message = "Achievement name already exists. Please choose a different name." });
            }

            // Determine the icon path, or keep it unchanged if no new file is provided
            var iconPath = achievementIcon != null && achievementIcon.Length > 0
                ? await SaveAchievementIcon(achievementIcon)
                : null;

            // Extract the numeric part from achievementDescription (without 'courses')
            var conditionNumber = achievementDescription.Replace(" courses", "").Trim();

            var achievement = await _context.Achievements.FindAsync(achievementId);
            if (achievement == null)
            {
                return Json(new { success = false, message = "Achievement not found" });
            }

            achievement.AchievementName = achievementName;
            achievement.AchievementDescription = conditionNumber;
            if (iconPath != null)
            {
                achievement.AchievementIcon = iconPath;
            }
            achievement.AchievementCreatedAt = achievementCreatedAt ?? DateTime.Today;

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        private async Task<string> SaveAchievementIcon(IFormFile achievementIcon)
        {
            var iconPath = "/uploads/Achievement/default.png";
            if (achievementIcon != null && achievementIcon.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "Achievement");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Path.GetFileName(achievementIcon.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await achievementIcon.CopyToAsync(stream);
                }
                iconPath = $"/uploads/Achievement/{fileName}";
            }
            return iconPath;
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAchievement(string achievementId)
        {
            var achievement = await _context.Achievements.FindAsync(achievementId);
            if (achievement == null)
            {
                return Json(new { success = false, message = "Achievement not found" });
            }

            _context.Achievements.Remove(achievement);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Achievement deleted successfully" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAchievement(string achievementId)
        {
            var achievement = await _context.Achievements.FindAsync(achievementId);
            if (achievement == null)
            {
                return Json(new { success = false, message = "Achievement not found!" });
            }

            return Json(new { success = true, data = achievement });
        }

        private async Task AssignAchievementsBasedOnCompletedCourses()
        {
            // Get all achievements with condition (number of completed courses)
            var achievements = await _context.Achievements
                .Where(a => int.TryParse(a.AchievementDescription, out _))
                .Select(a => new
                {
                    AchievementId = a.AchievementId,
                    RequiredCourses = int.Parse(a.AchievementDescription)
                })
                .ToListAsync();

            // Get count of completed courses (enrollment_status = 5) per user
            var userCompletedCourses = await _context.Enrollments
                .Where(e => e.EnrollmentStatus == 5)
                .GroupBy(e => e.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    CompletedCourses = g.Count()
                })
                .ToDictionaryAsync(g => g.UserId, g => g.CompletedCourses);

            // Insert achievements for each user based on completed courses
            foreach (var user in userCompletedCourses)
            {
                foreach (var achievement in achievements)
                {
                    if (user.Value >= achievement.RequiredCourses)
                    {
                        if (!await _context.UserAchievements.AnyAsync(ua => ua.UserId == user.Key && ua.AchievementId == achievement.AchievementId))
                        {
                            var userAchievement = new UserAchievement
                            {
                                UserId = user.Key,
                                AchievementId = achievement.AchievementId,
                                ReceivedDate = DateTime.Today
                            };

                            _context.UserAchievements.Add(userAchievement);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private string GenerateAchievementId()
        {
            var maxId = _context.Achievements.Max(a => int.Parse(a.AchievementId.Substring(1))) + 1;
            return "A" + maxId.ToString("D3");
        }
    }
}
