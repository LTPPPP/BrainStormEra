using BrainStormEra.Models;
using BrainStormEra.Views.Course;
using BrainStormEra.Views.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra.Controllers.Home
{
    public class HomePageGuestController : Controller
    {


        private readonly SwpMainContext _dbContext;

        public HomePageGuestController(SwpMainContext context)
        {
            _dbContext = context;
        }

        [HttpGet]
        public IActionResult Index()
        {

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


            var viewModel = new HomePageGuestViewtModel
            {


                RecommendedCourses = recommendedCourses
            };
            Console.WriteLine("Number of recommended courses: " + recommendedCourses.Count);
            return View("~/Views/Home/Index.cshtml", viewModel);

        }
    }
}
