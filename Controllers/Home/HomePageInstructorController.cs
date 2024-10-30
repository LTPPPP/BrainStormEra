﻿using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using Microsoft.Extensions.Logging;
using BrainStormEra.Views.Home;
using BrainStormEra.Views.Course;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra.Controllers
{
    public class HomePageInstructorController : Controller
    {
        private readonly SwpMainContext _dbContext;
        private readonly ILogger<HomePageInstructorController> _logger;

        public HomePageInstructorController(SwpMainContext dbContext, ILogger<HomePageInstructorController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult HomePageInstructor()
        {
            var userId = Request.Cookies["user_id"];

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LoginPage", "Login");
            }

            var account = _dbContext.Accounts.FirstOrDefault(a => a.UserId.ToString() == userId);

            if (account == null)
            {
                _logger.LogWarning($"User with ID {userId} not found.");
                return RedirectToAction("ErrorPage");
            }

            // Set data in ViewBag
            ViewBag.FullName = account.FullName;
            ViewBag.UserPicture = string.IsNullOrEmpty(account.UserPicture) ? "~/lib/img/User-img/default_user.png" : account.UserPicture;

            var recommendedCourses = _dbContext.Courses
         .Include(c => c.CourseCategories)        // Include danh mục khóa học
         .Include(c => c.Enrollments)             // Include danh sách Enrollments
         .Include(c => c.CreatedByNavigation)     // Include thông tin người tạo từ bảng Account
         .Where(c => c.CourseStatus == 2) // Chỉ lấy các khóa học có CourseStatus = 2, chưa đăng ký bởi người dùng
         .OrderByDescending(c => c.Enrollments.Count) // Sắp xếp giảm dần theo số lượng người đăng ký (Enrollments)
         .Take(4) // Lấy top 4 khóa học có lượt Enrollments cao nhất
         .Select(course => new ManagementCourseViewModel
         {
             CourseId = course.CourseId,
             CourseName = course.CourseName,
             CourseDescription = course.CourseDescription,
             CourseStatus = course.CourseStatus,
             CoursePicture = course.CoursePicture,
             Price = course.Price,
             CourseCreatedAt = course.CourseCreatedAt,
             CreatedBy = course.CreatedByNavigation.FullName,  // Lấy thông tin người tạo
             CourseCategories = course.CourseCategories.ToList(),
             StarRating = (byte?)Math.Round(
                 _dbContext.Feedbacks
                     .Where(f => f.CourseId == course.CourseId)
                     .Average(f => (double?)f.StarRating) ?? 0) // Tính trung bình StarRating
         })
         .ToList();

            // Nếu userRanking không null, ta có thể lấy Rank của người dùng hiện tại

            var viewModel = new HomePageInstructorViewModel
            {


                RecommendedCourses = recommendedCourses
            };


            return View("~/Views/Home/HomePageInstructor.cshtml", viewModel);
        }
    }
}
