using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using BrainStormEra.Views.Home;
using BrainStormEra.Views.Course;

namespace BrainStormEra.Controllers
{
    public class HomePageLearnerController : Controller
    {
        private readonly SwpMainContext _context;
        private readonly ILogger<HomePageLearnerController> _logger;

        public HomePageLearnerController(SwpMainContext context, ILogger<HomePageLearnerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> HomePageLearner()
        {
            var userId = Request.Cookies["user_id"];

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Cookies do not contain user_id.");
                return RedirectToAction("LoginPage", "Login");
            }

            var user = await _context.Accounts
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                _logger.LogWarning($"User with ID {userId} not found.");
                return RedirectToAction("LoginPage", "Login");
            }

            var categories = await _context.CourseCategories
                .OrderBy(c => c.CourseCategoryName)
                .Take(5)
                .ToListAsync();
            ViewBag.Categories = categories;

            var completedCoursesCount = await _context.Enrollments
                .CountAsync(e => e.UserId == userId && e.EnrollmentStatus == 5);
            var totalCoursesEnrolled = await _context.Enrollments
                .CountAsync(e => e.UserId == userId);
            var userRank = await _context.Accounts
                .Where(a => a.UserId == userId)
                .Select(a => a.PaymentPoint)
                .FirstOrDefaultAsync();
            var recommendedCourses = await _context.Courses
                .Where(c => c.CourseStatus == 2)
                .OrderByDescending(c => c.Enrollments.Count)
                .Take(4)
                .Select(c => new ManagementCourseViewModel
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    CourseDescription = c.CourseDescription,
                    CourseStatus = c.CourseStatus,
                    CoursePicture = c.CoursePicture,
                    Price = c.Price,
                    CourseCreatedAt = c.CourseCreatedAt,
                    CreatedBy = c.CreatedByNavigation.FullName,
                    StarRating = c.Feedbacks.Average(f => f.StarRating) ?? 0,
                    CourseCategories = c.CourseCategories.Select(cc => new CourseCategory
                    {
                        CourseCategoryName = cc.CourseCategoryName
                    }).ToList()
                })
                .ToListAsync();
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.NotificationCreatedAt)
                .ToListAsync();
            var dynamicAchievements = await _context.UserAchievements
                .Where(ua => ua.UserId == userId)
                .OrderByDescending(ua => ua.ReceivedDate)
                .Take(3)
                .Select(ua => new Models.Achievement
                {
                    AchievementId = ua.AchievementId,
                    AchievementName = ua.Achievement.AchievementName,
                    AchievementDescription = ua.Achievement.AchievementDescription,
                    AchievementIcon = ua.Achievement.AchievementIcon,
                    AchievementCreatedAt = ua.ReceivedDate
                })
                .ToListAsync();

            ViewBag.FullName = user.FullName ?? "Learner";
            ViewBag.UserPicture = string.IsNullOrEmpty(user.UserPicture)
                ? "~/lib/img/User-img/default_user.png"
                : user.UserPicture;

            var viewModel = new HomePageLearnerViewModel
            {
                FullName = user.FullName,
                UserPicture = user.UserPicture,
                CompletedCoursesCount = completedCoursesCount,
                TotalCoursesEnrolled = totalCoursesEnrolled,
                Achievements = dynamicAchievements,
                Ranking = userRank ?? 0,
                RecommendedCourses = recommendedCourses,
                Notifications = notifications
            };

            return View("~/Views/Home/HomePageLearner.cshtml", viewModel);
        }
    }
}
