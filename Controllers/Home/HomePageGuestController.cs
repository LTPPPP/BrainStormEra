using BrainStormEra.Models;
using BrainStormEra.Views.Course;
using BrainStormEra.Views.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.SqlClient;

namespace BrainStormEra.Controllers.Home
{
    public class HomePageGuestController : Controller
    {
        private readonly string _connectionString;

        private readonly SwpMainContext _dbContext;

        public HomePageGuestController(IConfiguration configuration, SwpMainContext context)
        {
            _connectionString = configuration.GetConnectionString("SwpMainContext");

            _dbContext = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            string categoryQuery = @"
                SELECT TOP 5
                    course_category_id AS CourseCategoryId,
                    course_category_name AS CourseCategoryName
                FROM
                    course_category
                ORDER BY
                    course_category_name;
            ";
            var categories = new List<CourseCategory>();

            // Execute both SQL queries in a single connection
            using (var connection = _dbContext.Database.GetDbConnection())
            {
                connection.Open();

                // Execute the category query
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = categoryQuery;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var category = new CourseCategory
                            {
                                CourseCategoryId = reader["CourseCategoryId"].ToString(),
                                CourseCategoryName = reader["CourseCategoryName"].ToString()
                            };
                            categories.Add(category);
                        }
                    }
                }

                ViewBag.Categories = categories; // Pass categories to the view using ViewBag

                string sqlQuery = @"
SELECT TOP 4
    c.course_id AS CourseId,
    c.course_name AS CourseName,
    c.course_description AS CourseDescription,
    c.course_status AS CourseStatus,
    c.course_picture AS CoursePicture,
    c.price AS Price,
    c.course_created_at AS CourseCreatedAt,
    a.full_name AS CreatedBy,
    ROUND(AVG(COALESCE(f.star_rating, 0)), 0) AS StarRating,
    STUFF((
        SELECT DISTINCT ', ' + cc.course_category_name
        FROM course_category_mapping AS ccm
        JOIN course_category AS cc ON ccm.course_category_id = cc.course_category_id
        WHERE ccm.course_id = c.course_id
        FOR XML PATH(''), TYPE
    ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS CourseCategories
FROM
    course AS c
JOIN
    account AS a ON c.created_by = a.user_id
LEFT JOIN
    enrollment AS e ON c.course_id = e.course_id
LEFT JOIN
    feedback AS f ON c.course_id = f.course_id
WHERE
    c.course_status = 2
GROUP BY
    c.course_id, c.course_name, c.course_description, c.course_status,
    c.course_picture, c.price, c.course_created_at, a.full_name
ORDER BY
    COUNT(e.enrollment_id) DESC;";


                var recommendedCourses = new List<HomePageGuestViewtModel.ManagementCourseViewModel>();

                // Mở kết nối và thực hiện truy vấn SQL
                using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    _dbContext.Database.OpenConnection();

                    using (var result = command.ExecuteReader())
                    {
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
                                StarRating = result["StarRating"] != DBNull.Value ? (byte?)Convert.ToByte(result["StarRating"]) : (byte?)0,
                                CourseCategories = result["CourseCategories"].ToString().Split(',').Select(c => c.Trim()).ToList()
                            };
                            recommendedCourses.Add(course);
                        }
                    }
                }

                // Tạo view model chứa danh sách các khóa học và gửi đến view
                var viewModel = new HomePageGuestViewtModel
                {
                    RecommendedCourses = recommendedCourses
                };
                ViewBag.Categories = categories; // Pass categories to the view using ViewBag

                return View("~/Views/Home/Index.cshtml", viewModel);
            }
        }
    }
}
