using BrainStormEra.Models;
using BrainStormEra.Repo;
using BrainStormEra.Repo.Admin;
using BrainStormEra.Views.Profile;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace BrainStormEra.Controllers.Profile
{
    public class ManageProfileController : Controller
    {
        private readonly ProfileRepo _profileRepo;
        private readonly AccountRepo _accountRepo;

        public ManageProfileController(ProfileRepo profileRepo, AccountRepo accountRepo)
        {
            _profileRepo = profileRepo ?? throw new ArgumentNullException(nameof(profileRepo));
            _accountRepo = accountRepo ?? throw new ArgumentNullException(nameof(accountRepo));
        }

        [HttpGet]
        public async Task<IActionResult> ViewUsers()
        {
            var userRole = Request.Cookies["user_role"];

            if (userRole == "1") // Admin
            {
                var users = await _profileRepo.GetLearnersAndInstructorsAsync();
                var userRoleCounts = await _accountRepo.GetUserRoleCountsAsync();
                ViewBag.UserRoleCounts = userRoleCounts;
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

        [HttpGet("/api/users/{userId}/completed-courses")]
        public async Task<IActionResult> GetCompletedCoursesForLearner(string userId)
        {
            var completedCourses = await _profileRepo.GetCompletedCoursesForLearnerAsync(userId);
            return Json(completedCourses);
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
            try
            {
                var newInstructorId = await _profileRepo.PromoteLearnerToInstructorAsync(userId);

                if (newInstructorId == null)
                    return BadRequest("Learner cannot be promoted. Ensure payment is zero and there are no enrollments.");

                return Ok($"Learner promoted to Instructor with new ID: {newInstructorId}");
            }
            catch (SqlException ex) when (ex.Number == 547) // Foreign key violation
            {
                return BadRequest("Promotion failed due to existing dependencies in the notification records.");
            }
            catch (Exception ex)
            {
                return BadRequest($"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpGet("/api/certificates/{userId}/{courseId}")]
        public async Task<IActionResult> GetCertificateForCourse(string userId, string courseId)
        {
            var certificate = await _profileRepo.GetCertificateDetailsForCourseAsync(userId, courseId);
            if (certificate == null)
            {
                return NotFound("Certificate not found for the specified course and user.");
            }
            return Json(certificate);
        }
    }
}
