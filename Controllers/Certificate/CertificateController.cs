﻿using BrainStormEra.Repo.Certificate;
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

            if (completedCourses == null || completedCourses.Count == 0)
            {
                return NotFound("Không có khóa học nào đã hoàn thành.");
            }

            ViewData["UserId"] = userId; // Truyền user_id qua ViewData
            return View(completedCourses); // Truyền danh sách các khóa học vào View
        }


        // Lấy thông tin chi tiết của một chứng chỉ cụ thể
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
                return NotFound("Không tìm thấy chứng chỉ cho khóa học này.");
            }
            var duration = (certificate.CompletedDate - certificate.StartedDate).TotalDays;
            ViewData["Duration"] = Math.Round(duration);

            return View(certificate); // Pass the certificate details to the view
        }

    }


}