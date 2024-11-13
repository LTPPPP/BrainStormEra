using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using BrainStormEra.Repo;

namespace BrainStormEra.Controllers.Achievement
{
    public class AchievementController : Controller
    {
        private readonly AchievementRepo _achievementRepo;

        public AchievementController(AchievementRepo achievementRepo)
        {
            _achievementRepo = achievementRepo;
        }

        public async Task<IActionResult> LearnerAchievements()
        {
            var userId = Request.Cookies["user_id"];
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LoginPage", "Login");
            }

            // Assign achievements based on completed courses
            await _achievementRepo.AssignAchievementsBasedOnCompletedCourses();

            // Retrieve the learner's achievements
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
                var allAchievements = await _achievementRepo.GetAdminAchievements();
                ViewData["UserId"] = userId;
                ViewData["Achievements"] = allAchievements;

                return View("~/Views/Achievements/AdminAchievements.cshtml");
            }

            return NotFound();
        }


        [HttpPost]

        [HttpPost]
        public async Task<IActionResult> AddAchievement(string achievementName, string achievementDescription, IFormFile achievementIcon)
        {
            // Check if the achievement name already exists to prevent duplicates
            if (await _achievementRepo.AchievementNameExists(achievementName))
            {
                return Json(new { success = false, message = "Achievement name already exists. Please choose a different name." });
            }

            // Get the list of all current conditions from the repository
            var allConditions = await _achievementRepo.GetAllConditions();
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

            await _achievementRepo.AddAchievement(achievementName, conditionValue.ToString(), iconPath);
            return Json(new { success = true });
        }





        [HttpGet]
        public async Task<IActionResult> CheckAchievementName(string achievementName, string achievementId = null)
        {
            var nameExists = await _achievementRepo.AchievementNameExists(achievementName, achievementId);
            return Json(new { success = !nameExists });
        }
        [HttpGet]
        public async Task<IActionResult> GetMaxCondition()
        {
            int maxCondition = await _achievementRepo.GetMaxCondition();
            return Json(new { success = true, maxCondition });
        }
        [HttpPost]
        public async Task<IActionResult> EditAchievement(string achievementId, string achievementName, string achievementDescription, IFormFile achievementIcon, DateTime? achievementCreatedAt)
        {
            // Check if the achievement name already exists, excluding the current achievement's ID
            if (await _achievementRepo.AchievementNameExists(achievementName, achievementId))
            {
                return Json(new { success = false, message = "Achievement name already exists. Please choose a different name." });
            }

            // Determine the icon path, or keep it unchanged if no new file is provided
            var iconPath = achievementIcon != null && achievementIcon.Length > 0
                ? $"/uploads/Achievement/{Path.GetFileName(achievementIcon.FileName)}"
                : null;

            // If a new icon is provided, save it to the uploads folder
            if (iconPath != null)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "Achievement");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, Path.GetFileName(achievementIcon.FileName));
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await achievementIcon.CopyToAsync(stream);
                }
            }

            // Extract the numeric part from achievementDescription (without 'courses')
            var conditionNumber = achievementDescription.Replace(" courses", "").Trim();

            // Call the repository to update the achievement details
            var result = await _achievementRepo.EditAchievement(achievementId, achievementName, conditionNumber, iconPath, achievementCreatedAt);

            // Return the result of the operation as JSON
            return Json(new { success = result });
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
            var result = await _achievementRepo.DeleteAchievement(achievementId);
            if (result)
            {
                return Json(new { success = true, message = "Achievement deleted successfully" });
            }
            else
            {
                return Json(new { success = false, message = "Failed to delete achievement" });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetAchievement(string achievementId)
        {
            var achievement = await _achievementRepo.GetAchievement(achievementId);
            if (achievement == null)
            {
                return Json(new { success = false, message = "Achievement not found!" });
            }

            return Json(new { success = true, data = achievement });
        }
    }
}
