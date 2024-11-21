using BrainStormEra.Repo.Certificate;
using Microsoft.AspNetCore.Mvc;

namespace BrainStormEra.Controllers.Certificate
{
    public class CertificateController : Controller
    {
        private readonly ICertificateRepository _certificateRepository;

        public CertificateController(ICertificateRepository certificateRepository)
        {
            _certificateRepository = certificateRepository;
        }

        public async Task<IActionResult> CompletedCourses()
        {
            var userId = Request.Cookies["user_id"];
            var completedCourses = await _certificateRepository.GetCompletedCoursesAsync(userId);

            bool hasCompletedCourses = completedCourses != null && completedCourses.Count > 0;

            if (!hasCompletedCourses)
            {
                ViewData["NoCertificatesMessage"] = "You haven't completed any courses yet. Start learning!";
            }

            ViewData["UserId"] = userId; // Pass user_id through ViewData
            return View(completedCourses); // Pass completed courses to view
        }


        public async Task<IActionResult> CertificateDetails(string courseId)
        {
            var userId = Request.Cookies["user_id"]; // Get user ID from cookies

            if (string.IsNullOrEmpty(courseId) || string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID or Course ID is missing.");
            }

            var certificate = await _certificateRepository.GetCertificateDetailsAsync(userId, courseId);

            if (certificate == null)
            {
                return NotFound("No certificate found for this course.");
            }
            var duration = (certificate.CompletedDate - certificate.StartedDate).TotalDays;
            if (duration < 1)
            {
                ViewData["Duration"] = 1;
            }
            else
            {
                ViewData["Duration"] = Math.Round(duration);
            }

            return View(certificate); // Pass the certificate details to the view
        }
        [HttpGet]
        public async Task<IActionResult> CertificateDetails(string userId, string courseId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(courseId))
            {
                return BadRequest("User ID or Course ID is missing.");
            }

            var certificate = await _certificateRepository.GetCertificateDetailsAsync(userId, courseId);

            if (certificate == null)
            {
                return NotFound("Certificate not found for this course.");
            }

            return Json(new
            {
                userName = certificate.UserName,
                courseName = certificate.CourseName,
                completedDate = certificate.CompletedDate.ToString("yyyy-MM-dd")
            });
        }
    }
}