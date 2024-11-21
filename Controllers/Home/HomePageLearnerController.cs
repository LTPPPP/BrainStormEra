using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using BrainStormEra.Views.Home;
using BrainStormEra.Views.Course;
using BrainStormEra.Repositories;
using BrainStormEra.Repo;

namespace BrainStormEra.Controllers
{
    public class HomePageLearnerController : Controller
    {
        private readonly LearnerRepo _learnerRepo;
        private readonly AccountRepo _accountRepo;
        private readonly AchievementRepo _achievementRepo;
        private readonly ILogger<HomePageInstructorController> _logger;

        public HomePageLearnerController(IConfiguration configuration, LearnerRepo learnerRepo, AchievementRepo achievementRepo, AccountRepo accountRepo, ILogger<HomePageInstructorController> logger)
        {
            string connectionString = configuration.GetConnectionString("SwpMainContext");
            _learnerRepo = learnerRepo;
            _accountRepo = accountRepo;
            _achievementRepo = achievementRepo;
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

            var user = await _learnerRepo.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User with ID {userId} not found.");
                return RedirectToAction("LoginPage", "Login");
            }

            var categories = _learnerRepo.GetTopCategories();
            ViewBag.Categories = categories;

            var completedCoursesCount = await _learnerRepo.GetCompletedCoursesCountAsync(userId);
            var totalCoursesEnrolled = await _learnerRepo.GetTotalCoursesEnrolledAsync(userId);
            var userRank = await _accountRepo.GetUserRankAsync(userId);
            var recommendedCourses = await _learnerRepo.GetRecommendedCoursesAsync(userId);
            var notifications = await _learnerRepo.GetNotificationsAsync(userId);
            var dynamicAchievements = await _achievementRepo.GetLearnerAchievements(userId);

            var achievements = dynamicAchievements.Select(a => new Models.Achievement
            {
                AchievementId = a.AchievementId,
                AchievementName = a.AchievementName,
                AchievementDescription = a.AchievementDescription,
                AchievementIcon = a.AchievementIcon,
                AchievementCreatedAt = DateTime.Parse(a.ReceivedDate.ToString())
            }).ToList();

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
                Achievements = achievements,
                Ranking = int.TryParse(userRank, out var rank) ? rank : 0,
                RecommendedCourses = recommendedCourses,
                Notifications = notifications
            };

            return View("~/Views/Home/HomePageLearner.cshtml", viewModel);
        }


    }
}