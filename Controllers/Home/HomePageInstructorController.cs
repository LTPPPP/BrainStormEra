using BrainStormEra.Models;
using BrainStormEra.Repositories;
using BrainStormEra.Views.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BrainStormEra.Controllers
{
    public class HomePageInstructorController : Controller
    {
        private readonly InstructorRepo _repository;
        private readonly ILogger<HomePageInstructorController> _logger;

        public HomePageInstructorController(IConfiguration configuration, InstructorRepo repository, ILogger<HomePageInstructorController> logger)
        {
            string connectionString = configuration.GetConnectionString("SwpMainContext");
            _repository = repository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> HomePageInstructor()
        {
            var userId = Request.Cookies["user_id"];

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LoginPage", "Login");
            }

            var (fullName, userPicture) = await _repository.GetUserDetailsAsync(userId);

            ViewBag.FullName = fullName;
            ViewBag.UserPicture = userPicture;

            var categories = _repository.GetTopCategories();
            var recommendedCourses = _repository.GetRecommendedCourses();

            ViewBag.Categories = categories;

            var viewModel = new HomePageInstructorViewModel
            {
                RecommendedCourses = recommendedCourses
            };

            return View("~/Views/Home/HomePageInstructor.cshtml", viewModel);
        }

    }
}
