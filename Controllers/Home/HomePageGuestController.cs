using BrainStormEra.Models;
using BrainStormEra.Views.Course;
using BrainStormEra.Views.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

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
                    c.course_id,
                    c.course_name,
                    c.course_description,
                    c.course_status,
                    c.course_picture,
                    c.price,
                    c.course_created_at,
                    a.full_name AS CreatedBy,
                    AVG(f.star_rating) AS StarRating
                FROM
                    course AS c
                JOIN
                    account AS a ON c.created_by = a.user_id
                LEFT JOIN
                    course_category_mapping AS cc ON c.course_id = cc.course_id
                LEFT JOIN
                    enrollment AS e ON c.course_id = e.course_id
                LEFT JOIN
                    feedback AS f ON c.course_id = f.course_id
                WHERE
                    c.course_status = 2
                GROUP BY
                    c.course_id, c.course_name, c.course_description, c.course_status, 
                    c.course_picture, c.Price, c.course_created_at, a.full_name
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
                            CourseId = result["course_id"].ToString(),
                            CourseName = result["course_name"].ToString(),
                            CourseDescription = result["course_description"].ToString(),
                            CourseStatus = Convert.ToInt32(result["course_status"]),
                            CoursePicture = result["course_picture"].ToString(),
                            Price = Convert.ToDecimal(result["price"]),
                            CourseCreatedAt = Convert.ToDateTime(result["course_created_at"]),
                            CreatedBy = result["CreatedBy"].ToString(),
                            StarRating = result["StarRating"] != DBNull.Value ? (byte?)Convert.ToByte(result["StarRating"]) : (byte?)0
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

            return View("~/Views/Home/Index.cshtml", viewModel);
        }
    }
}
