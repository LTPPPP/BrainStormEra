using BrainStormEra.Models;
using BrainStormEra.Views.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BrainStormEra.Controllers.Home
{
    public class HomePageGuestController : Controller
    {
        private readonly SwpMainContext _context;

        public HomePageGuestController(SwpMainContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var categories = GetTopCategories();
            var recommendedCourses = GetRecommendedCourses();

            var viewModel = new HomePageGuestViewtModel
            {
                RecommendedCourses = recommendedCourses
            };

            ViewBag.Categories = categories; // Pass categories to the view using ViewBag

            return View("~/Views/Home/Index.cshtml", viewModel);
        }

        private List<CourseCategory> GetTopCategories()
        {
            return _context.CourseCategories
                .OrderBy(c => c.CourseCategoryName)
                .Take(5)
                .ToList();
        }

        private List<HomePageGuestViewtModel.ManagementCourseViewModel> GetRecommendedCourses()
        {
            var recommendedCourses = _context.Courses
                .Where(c => c.CourseStatus == 2)
                .OrderByDescending(c => c.Enrollments.Count())
                .Take(4)
                .Select(c => new HomePageGuestViewtModel.ManagementCourseViewModel
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    CourseDescription = c.CourseDescription,
                    CourseStatus = c.CourseStatus ?? 0,
                    CoursePicture = c.CoursePicture,
                    Price = c.Price,
                    CourseCreatedAt = c.CourseCreatedAt,
                    CreatedBy = c.CreatedByNavigation.FullName,
                    StarRating = (byte?)(c.Feedbacks.Average(f => (double?)f.StarRating) ?? 0),
                    CourseCategories = c.CourseCategories.Select(cc => cc.CourseCategoryName).ToList()
                })
                .ToList();

            return recommendedCourses;
        }
    }
}
