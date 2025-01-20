using BrainStormEra.Models;
using BrainStormEra.Views.Profile;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BrainStormEra.Controllers.Profile
{
    public class ManageProfileController : Controller
    {
        private readonly SwpMainContext _context;

        public ManageProfileController(SwpMainContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [HttpGet]
        public async Task<IActionResult> ViewUsers()
        {
            var userRole = Request.Cookies["user_role"];

            if (userRole == "1") // Admin
            {
                var users = await _context.Accounts
                    .Where(a => a.UserRole == 2 || a.UserRole == 3)
                    .Select(a => new UserDetailsViewModel
                    {
                        UserId = a.UserId,
                        UserRole = a.UserRole,
                        Username = a.Username,
                        UserEmail = a.UserEmail,
                        FullName = a.FullName,
                        DateOfBirth = a.DateOfBirth,
                        Gender = a.Gender,
                        PhoneNumber = a.PhoneNumber,
                        UserAddress = a.UserAddress,
                        AccountCreatedAt = a.AccountCreatedAt,
                        Approved = _context.Enrollments.Any(e => e.UserId == a.UserId && e.Approved) ? 1 : 0
                    })
                    .ToListAsync();

                var userRoleCounts = await _context.Accounts
                    .GroupBy(a => a.UserRole)
                    .Select(g => new
                    {
                        UserRole = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(g => g.UserRole, g => g.Count);

                ViewBag.UserRoleCounts = new UserRoleCountsViewModel
                {
                    TotalUsers = userRoleCounts.Values.Sum(),
                    AdminCount = userRoleCounts.GetValueOrDefault(1),
                    InstructorCount = userRoleCounts.GetValueOrDefault(2),
                    LearnerCount = userRoleCounts.GetValueOrDefault(3)
                };

                return View("~/Views/Admin/ManageUser.cshtml", users);
            }
            else if (userRole == "2") // Instructor
            {
                var instructorId = Request.Cookies["user_id"];
                var model = await _context.Enrollments
                    .Where(e => e.Course.CreatedBy == instructorId)
                    .Select(e => new UserDetailsViewModel
                    {
                        UserId = e.UserId,
                        UserRole = e.User.UserRole,
                        Username = e.User.Username,
                        UserEmail = e.User.UserEmail,
                        FullName = e.User.FullName,
                        DateOfBirth = e.User.DateOfBirth,
                        Gender = e.User.Gender,
                        PhoneNumber = e.User.PhoneNumber,
                        UserAddress = e.User.UserAddress,
                        AccountCreatedAt = e.User.AccountCreatedAt,
                        Approved = e.Approved ? 1 : 0
                    })
                    .ToListAsync();

                return View("~/Views/Instructor/ViewLearner.cshtml", model);
            }

            return Unauthorized();
        }

        [HttpGet("/api/users/{userId}")]
        public async Task<IActionResult> GetUserDetails(string userId)
        {
            var user = await _context.Accounts
                .Where(a => a.UserId == userId)
                .Select(a => new UserDetailsViewModel
                {
                    UserId = a.UserId,
                    UserRole = a.UserRole,
                    Username = a.Username,
                    UserEmail = a.UserEmail,
                    FullName = a.FullName,
                    DateOfBirth = a.DateOfBirth,
                    Gender = a.Gender,
                    PhoneNumber = a.PhoneNumber,
                    UserAddress = a.UserAddress,
                    AccountCreatedAt = a.AccountCreatedAt,
                    Approved = _context.Enrollments.Any(e => e.UserId == a.UserId && e.Approved) ? 1 : 0
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }
            return Json(user);
        }

        [HttpPost("/api/ban/{userId}")]
        public async Task<IActionResult> BanLearner(string userId)
        {
            var enrollment = await _context.Enrollments
                .Where(e => e.UserId == userId)
                .FirstOrDefaultAsync();

            if (enrollment == null)
            {
                return BadRequest("Failed to ban user: No matching enrollment found.");
            }

            enrollment.Approved = false;
            await _context.SaveChangesAsync();

            return Ok("User banned successfully.");
        }

        [HttpGet("/api/users/{userId}/completed-courses")]
        public async Task<IActionResult> GetCompletedCoursesForLearner(string userId)
        {
            var completedCourses = await _context.LessonCompletions
                .Where(lc => lc.UserId == userId)
                .Select(lc => new
                {
                    CourseId = lc.Lesson.Chapter.CourseId,
                    CourseName = lc.Lesson.Chapter.Course.CourseName,
                    CompletionDate = lc.CompletionDate
                })
                .ToListAsync();

            return Json(completedCourses);
        }

        [HttpPost("/api/unban/{userId}")]
        public async Task<IActionResult> UnbanLearner(string userId)
        {
            var enrollment = await _context.Enrollments
                .Where(e => e.UserId == userId)
                .FirstOrDefaultAsync();

            if (enrollment == null)
            {
                return BadRequest("Failed to unban user: No matching enrollment found.");
            }

            enrollment.Approved = true;
            await _context.SaveChangesAsync();

            return Ok("User unbanned successfully.");
        }

        [HttpPost("/api/promote/{userId}")]
        public async Task<IActionResult> PromoteLearner(string userId)
        {
            var learner = await _context.Accounts
                .Where(a => a.UserId == userId && a.UserRole == 3)
                .FirstOrDefaultAsync();

            if (learner == null || learner.PaymentPoint > 0 || _context.Enrollments.Any(e => e.UserId == userId))
            {
                return BadRequest("Learner cannot be promoted. Ensure payment is zero and there are no enrollments.");
            }

            var maxInstructorId = await _context.Accounts
                .Where(a => a.UserRole == 2)
                .MaxAsync(a => int.Parse(a.UserId.Substring(2)));

            var newInstructorId = $"IN{maxInstructorId + 1:D3}";

            learner.UserId = newInstructorId;
            learner.UserRole = 2;

            await _context.SaveChangesAsync();

            return Ok($"Learner promoted to Instructor with new ID: {newInstructorId}");
        }

        [HttpGet("/api/certificates/{userId}/{courseId}")]
        public async Task<IActionResult> GetCertificateForCourse(string userId, string courseId)
        {
            var certificate = await _context.Enrollments
                .Where(e => e.UserId == userId && e.CourseId == courseId && e.CertificateIssuedDate != null)
                .Select(e => new
                {
                    CertificateIssuedDate = e.CertificateIssuedDate,
                    CourseName = e.Course.CourseName
                })
                .FirstOrDefaultAsync();

            if (certificate == null)
            {
                return NotFound("Certificate not found for the specified course and user.");
            }
            return Json(certificate);
        }
    }
}
