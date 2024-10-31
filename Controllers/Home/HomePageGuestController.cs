using BrainStormEra.Models;
using BrainStormEra.Views.Course;
using BrainStormEra.Views.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using BrainStormEra.Views.Course;
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
            // Định nghĩa truy vấn SQL để lấy top 4 khoá học được đề xuất
            string sqlQuery = @"
        SELECT TOP 4
            c.CourseId,
            c.CourseName,
            c.CourseDescription,
            c.CourseStatus,
            c.CoursePicture,
            c.Price,
            c.CourseCreatedAt,
            a.FullName AS CreatedBy,
            AVG(f.StarRating) AS StarRating
        FROM
            course AS c
        JOIN
            account AS a ON c.CreatedBy = a.user_id
        LEFT JOIN
            course_category_mapping AS cc ON c.CourseId = cc.course_id
        LEFT JOIN
            enrollment AS e ON c.CourseId = e.course_id
        LEFT JOIN
            feedback AS f ON c.CourseId = f.course_id
        WHERE
            c.CourseStatus = 2
        GROUP BY
            c.CourseId, c.CourseName, c.CourseDescription, c.CourseStatus, 
            c.CoursePicture, c.Price, c.CourseCreatedAt, a.FullName
        ORDER BY
            COUNT(e.enrollment_id) DESC;";

            // Tạo danh sách để lưu trữ các khóa học được đề xuất
            var recommendedCourses = new List<HomePageGuestViewtModel.ManagementCourseViewModel>();

            // Mở kết nối đến database và thực hiện truy vấn
            using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = sqlQuery;
                _dbContext.Database.OpenConnection();

                using (var result = command.ExecuteReader())
                {
                    // Duyệt qua kết quả truy vấn và ánh xạ sang ManagementCourseViewModel
                    while (result.Read())
                    {
                        var course = new HomePageGuestViewtModel.ManagementCourseViewModel
                        {
                            CourseId = result["CourseId"].ToString(),
                            CourseName = result["CourseName"].ToString(),
                            CourseDescription = result["CourseDescription"].ToString(),
                            CourseStatus = Convert.ToInt32(result["CourseStatus"]),
                            CoursePicture = result["CoursePicture"].ToString(),
                            Price = Convert.ToDecimal(result["Price"]),
                            CourseCreatedAt = Convert.ToDateTime(result["CourseCreatedAt"]),
                            CreatedBy = result["CreatedBy"].ToString(),
                            StarRating = result["StarRating"] != DBNull.Value ? (byte?)Convert.ToByte(result["StarRating"]) : null
                        };
                        recommendedCourses.Add(course);
                    }
                }
            }

            // Chuẩn bị view model với danh sách các khóa học được đề xuất
            var viewModel = new HomePageGuestViewtModel
            {
                RecommendedCourses = recommendedCourses
            };

            Console.WriteLine("Số lượng khóa học được đề xuất: " + recommendedCourses.Count);
            return View("~/Views/Home/Index.cshtml", viewModel);
        }
    }
}
