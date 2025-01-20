using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BrainStormEra.Controllers.Certificate
{
    public class CertificateController : Controller
    {
        private readonly SwpMainContext _context;

        public CertificateController(SwpMainContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> CompletedCourses()
        {
            var userId = Request.Cookies["user_id"];
            var completedCourses = await _context.Enrollments
                .Where(e => e.UserId == userId && e.EnrollmentStatus == 5 && e.CertificateIssuedDate != null)
                .Join(_context.Courses,
                      e => e.CourseId,
                      c => c.CourseId,
                      (e, c) => new CertificateSummaryViewModel
                      {
                          CourseId = c.CourseId,
                          CourseName = c.CourseName,
                          CompletedDate = e.CertificateIssuedDate.Value
                      })
                .ToListAsync();

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

            var certificate = await _context.Enrollments
                .Where(e => e.UserId == userId && e.CourseId == courseId && e.EnrollmentStatus == 5)
                .Join(_context.Courses,
                      e => e.CourseId,
                      c => c.CourseId,
                      (e, c) => new { Enrollment = e, Course = c })
                .Join(_context.Accounts,
                      ec => ec.Enrollment.UserId,
                      a => a.UserId,
                      (ec, a) => new CertificateViewModel
                      {
                          UserName = a.FullName,
                          CourseName = ec.Course.CourseName,
                          CourseDescription = ec.Course.CourseDescription,
                          CompletedDate = ec.Enrollment.CertificateIssuedDate.Value,
                          StartedDate = ec.Enrollment.EnrollmentCreatedAt
                      })
                .FirstOrDefaultAsync();

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

            var certificate = await _context.Enrollments
                .Where(e => e.UserId == userId && e.CourseId == courseId && e.EnrollmentStatus == 5)
                .Join(_context.Courses,
                      e => e.CourseId,
                      c => c.CourseId,
                      (e, c) => new { Enrollment = e, Course = c })
                .Join(_context.Accounts,
                      ec => ec.Enrollment.UserId,
                      a => a.UserId,
                      (ec, a) => new
                      {
                          UserName = a.FullName,
                          CourseName = ec.Course.CourseName,
                          CompletedDate = ec.Enrollment.CertificateIssuedDate.Value
                      })
                .FirstOrDefaultAsync();

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
