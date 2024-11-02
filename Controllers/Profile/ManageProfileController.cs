using BrainStormEra.Models;
using BrainStormEra.Repo.Admin;
using BrainStormEra.Views.Profile;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BrainStormEra.Controllers.Profile
{
    public class ManageProfileController : Controller
    {
        private readonly ProfileRepo _profileRepo;

        public ManageProfileController(ProfileRepo profileRepo)
        {
            _profileRepo = profileRepo ?? throw new ArgumentNullException(nameof(profileRepo));
        }

        [HttpGet]
        public async Task<IActionResult> ViewUsers()
        {
            var userRole = Request.Cookies["user_role"];

            if (userRole == "1") // Admin
            {
                var users = await _profileRepo.GetLearnersAndInstructorsAsync();
                return View("~/Views/Admin/ManageUser.cshtml", users);
            }
            else if (userRole == "2") // Instructor
            {
                var instructorId = Request.Cookies["user_id"];
                var model = await _profileRepo.GetLearnersByInstructorCoursesAsync(instructorId);
                return View("~/Views/Instructor/ViewLearner.cshtml", model);
            }

            return Unauthorized();
        }

        [HttpGet("/api/users/{userId}")]
        public async Task<IActionResult> GetUserDetails(string userId)
        {
            var user = await _profileRepo.GetUserDetailsAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            return Json(user);
        }

        [HttpPost("/api/ban/{userId}")]
        public async Task<IActionResult> BanLearner(string userId)
        {
            try
            {
                await _profileRepo.BanLearnerAsync(userId);
                return Ok("User banned successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to ban user: {ex.Message}");
            }
        }

        [HttpPost("/api/unban/{userId}")]
        public async Task<IActionResult> UnbanLearner(string userId)
        {
            try
            {
                await _profileRepo.UnbanLearnerAsync(userId);
                return Ok("User unbanned successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to unban user: {ex.Message}");
            }
        }

        [HttpPost("/api/promote/{userId}")]
        public async Task<IActionResult> PromoteLearner(string userId)
        {
            var newInstructorId = await _profileRepo.PromoteLearnerToInstructorAsync(userId);

            if (newInstructorId == null)
                return BadRequest("Learner cannot be promoted. Ensure payment is zero and there are no enrollments.");

            return Ok($"Learner promoted to Instructor with new ID: {newInstructorId}");
        }
    }
}
