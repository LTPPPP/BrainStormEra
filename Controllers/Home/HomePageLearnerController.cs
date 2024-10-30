using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using Microsoft.Extensions.Logging;
using BrainStormEra.Views.Home;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium.BiDi.Modules.Script;
using BrainStormEra.Views.Course;

namespace BrainStormEra.Controllers
{
    public class HomePageLearnerController : Controller
    {
        private readonly SwpMainContext _dbContext;
        private readonly ILogger<HomePageLearnerController> _logger;

        public HomePageLearnerController(SwpMainContext dbContext, ILogger<HomePageLearnerController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult HomePageLearner()
        {
            var userId = Request.Cookies["user_id"];

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Cookies do not contain user_id.");
                return RedirectToAction("LoginPage", "Login");
            }

            var user = _dbContext.Accounts.FirstOrDefault(u => u.UserId.ToString() == userId);

            if (user == null)
            {
                _logger.LogWarning($"User with ID {userId} not found.");
                return RedirectToAction("LoginPage", "Login");
            }

            // Set data in ViewBag
            ViewBag.FullName = user?.FullName ?? "Learner";
            ViewBag.UserPicture = string.IsNullOrEmpty(user?.UserPicture) ? "~/lib/img/User-img/default_user.png" : user.UserPicture;

            // xử lí dữ liệu để đẩy lên page 
            // Lấy các khóa học có nhiều người tham gia nhất
            var completedCoursesCount = _dbContext.Enrollments
          .Where(e => e.UserId == userId && e.EnrollmentStatus == 5)
          .Count();

            var totalCoursesEnrolled = _dbContext.Enrollments
                .Where(e => e.UserId == userId)
                .Count();


            // Lấy thông tin thành tích của người dùng
            var achievements = _dbContext.UserAchievements
                .Where(ua => ua.UserId == userId)
                .Include(ua => ua.Achievement) // Load related Achievements
                .OrderByDescending(ua => ua.Achievement.AchievementCreatedAt) // Sort by AchievementCreatedAt
                .Take(3) // Limit to 3 latest achievements
                .Select(ua => new BrainStormEra.Models.Achievement
                {
                    AchievementName = ua.Achievement.AchievementName,
                    AchievementIcon = ua.Achievement.AchievementIcon,
                })
                .ToList();



            // Tính số khóa học đã hoàn thành cho mỗi người dùng và xếp hạng
            var userRanking = _dbContext.Enrollments
                .Where(e => e.EnrollmentStatus == 5) // Chỉ tính những khóa học đã hoàn thành
                .GroupBy(e => e.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    CompletedCoursesCount = g.Count()
                })
                .OrderByDescending(u => u.CompletedCoursesCount)
                .ToList()
                .Select((u, index) => new
                {
                    u.UserId,
                    u.CompletedCoursesCount,
                    Rank = index + 1 // Xếp hạng bắt đầu từ 1
                })
                .FirstOrDefault(u => u.UserId == userId);

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
            int userRank = userRanking != null ? userRanking.Rank : 0;

            var viewModel = new HomePageLearnerViewModel
            {
                FullName = user.FullName,
                UserPicture = user.UserPicture,
                CompletedCoursesCount = completedCoursesCount,
                TotalCoursesEnrolled = totalCoursesEnrolled,
                Achievements = achievements,
                Ranking = userRank,
                RecommendedCourses = recommendedCourses
            };


            return View("~/Views/Home/HomePageLearner.cshtml", viewModel);
        }

    }
}
