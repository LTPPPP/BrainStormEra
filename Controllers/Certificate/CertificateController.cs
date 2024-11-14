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

        // Lấy danh sách các khóa học đã hoàn thành của người dùng
        public async Task<IActionResult> CompletedCourses()
        {
            var userId = Request.Cookies["user_id"];
            var completedCourses = await _certificateRepository.GetCompletedCoursesAsync(userId);

            ViewData["UserId"] = userId; // Truyền user_id qua ViewData
            return View(completedCourses); // Truyền danh sách các khóa học vào View
        }


        [HttpPost]
        public async Task<IActionResult> CertificateDetails(string courseId)
        {
            var userId = Request.Cookies["user_id"]; // Get user ID from cookies

            if (string.IsNullOrEmpty(courseId))
            {
                return BadRequest("404 EXCEPTION.");
            }

            var certificate = await _certificateRepository.GetCertificateDetailsAsync(userId, courseId);

            var duration = (certificate.CompletedDate - certificate.StartedDate).TotalDays;
            ViewData["Duration"] = Math.Round(duration);

            return View(certificate); // Pass the certificate details to the view
        }

    }


}
