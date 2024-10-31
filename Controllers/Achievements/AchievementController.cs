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

        // Display learner's achievements based on user ID
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
        public async Task<IActionResult> AddAchievement(string achievementName, string achievementDescription, IFormFile achievementIcon)
        {
            var iconPath = "/uploads/Achievement/default.png"; // Default image
            if (achievementIcon != null && achievementIcon.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "Achievement");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"{Path.GetFileName(achievementIcon.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await achievementIcon.CopyToAsync(stream);
                }

                iconPath = $"/uploads/Achievement/{fileName}";
            }

            await _achievementRepo.AddAchievement(achievementName, achievementDescription, iconPath);

            return RedirectToAction("AdminAchievements");
        }

        [HttpPost]
        public async Task<IActionResult> EditAchievement(string achievementId, string achievementName, string achievementDescription, IFormFile achievementIcon, DateTime? achievementCreatedAt)
        {
            var iconPath = achievementIcon != null && achievementIcon.Length > 0
                ? $"/uploads/Achievement/{Path.GetFileName(achievementIcon.FileName)}"
                : null;

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

            var result = await _achievementRepo.EditAchievement(achievementId, achievementName, achievementDescription, iconPath, achievementCreatedAt);

            return Json(result);
        }

        // Delete Achievement for Admin
        [HttpPost]
        public async Task<IActionResult> DeleteAchievement(string achievementId)
        {
            var result = await _achievementRepo.DeleteAchievement(achievementId);
            return Json(result);
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
