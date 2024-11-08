using BrainStormEra.Models;
using BrainStormEra.Repo;
using BrainStormEra.Views.Home;
using Microsoft.AspNetCore.Mvc;

namespace BrainStormEra.Controllers.Home
{
    public class HomePageGuestController : Controller
    {
        private readonly GuestRepo _guestRepo;

        public HomePageGuestController(GuestRepo guestRepo)
        {
            _guestRepo = guestRepo;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var categories = _guestRepo.GetTopCategories();
            var recommendedCourses = _guestRepo.GetRecommendedCourses();

            var viewModel = new HomePageGuestViewtModel
            {
                RecommendedCourses = recommendedCourses
            };

            ViewBag.Categories = categories; // Pass categories to the view using ViewBag

            return View("~/Views/Home/Index.cshtml", viewModel);
        }
    }
}
