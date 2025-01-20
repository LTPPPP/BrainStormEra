using BrainStormEra.Models;
using BrainStormEra.Views.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BrainStormEra.Controllers
{
    public class HomePageInstructorController : Controller
    {
        private readonly SwpMainContext _context;
        private readonly ILogger<HomePageInstructorController> _logger;

        public HomePageInstructorController(SwpMainContext context, ILogger<HomePageInstructorController> logger)
        {
            _context = context;
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

            var userDetails = await _context.Accounts
                .Where(a => a.UserId == userId)
                .Select(a => new { a.FullName, a.UserPicture })
                .FirstOrDefaultAsync();

            ViewBag.FullName = userDetails?.FullName ?? "Guest";
            ViewBag.UserPicture = userDetails?.UserPicture ?? "~/lib/img/User-img/default_user.png";

            var categories = await _context.CourseCategories
                .OrderBy(c => c.CourseCategoryName)
                .Take(5)
                .ToListAsync();

            ViewBag.Categories = categories;

            var recommendedCourses = await _context.Courses
                .Where(c => c.CourseStatus == 2)
                .OrderByDescending(c => c.Enrollments.Count())
                .Take(4)
                .Select(c => new BrainStormEra.Views.Course.ManagementCourseViewModel
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    CourseDescription = c.CourseDescription,
                    CourseStatus = (int)c.CourseStatus,
                    CoursePicture = c.CoursePicture,
                    Price = c.Price,
                    CourseCreatedAt = c.CourseCreatedAt,
                    CreatedBy = c.CreatedByNavigation.FullName,
                    StarRating = c.Feedbacks.Any() ? (byte?)c.Feedbacks.Average(f => f.StarRating) : (byte?)0,
                    CourseCategories = c.CourseCategories.Select(cc => new CourseCategory
                    {
                        CourseCategoryName = cc.CourseCategoryName
                    }).ToList()
                })
                .ToListAsync();

            var viewModel = new HomePageInstructorViewModel
            {
                RecommendedCourses = recommendedCourses
            };

            return View("~/Views/Home/HomePageInstructor.cshtml", viewModel);
        }
    }
}
