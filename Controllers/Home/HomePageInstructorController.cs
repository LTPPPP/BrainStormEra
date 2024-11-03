using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using Microsoft.Extensions.Logging;
using BrainStormEra.Views.Home;
using BrainStormEra.Views.Course;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra.Controllers
{
    public class HomePageInstructorController : Controller
    {
        private readonly SwpMainContext _dbContext;
        private readonly ILogger<HomePageInstructorController> _logger;

        public HomePageInstructorController(SwpMainContext dbContext, ILogger<HomePageInstructorController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult HomePageInstructor()
        {
            var userId = Request.Cookies["user_id"];

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LoginPage", "Login");
            }

            var account = _dbContext.Accounts.FirstOrDefault(a => a.UserId.ToString() == userId);

            if (account == null)
            {
                _logger.LogWarning($"User with ID {userId} not found.");
                return RedirectToAction("ErrorPage");
            }

            // Set data in ViewBag
            ViewBag.FullName = account.FullName;
            ViewBag.UserPicture = string.IsNullOrEmpty(account.UserPicture)
                ? "~/lib/img/User-img/default_user.png"
                : account.UserPicture;

            // Prepare SQL queries to fetch recommended courses and categories
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
                    COALESCE(ROUND(AVG(f.star_rating), 0), 0) AS StarRating,
                    cc.course_category_name AS CourseCategory
                FROM 
                    course c
                    LEFT JOIN account a ON c.created_by = a.user_id
                    LEFT JOIN enrollment e ON c.course_id = e.course_id
                    LEFT JOIN feedback f ON c.course_id = f.course_id
                    LEFT JOIN course_category_mapping ccm ON c.course_id = ccm.course_id
                    LEFT JOIN course_category cc ON ccm.course_category_id = cc.course_category_id
                WHERE 
                    c.course_status = 2
                GROUP BY 
                    c.course_id, c.course_name, c.course_description, c.course_status, 
                    c.course_picture, c.price, c.course_created_at, a.full_name, cc.course_category_name
                ORDER BY 
                    COUNT(e.enrollment_id) DESC;
            ";

            string categoryQuery = @"
                SELECT TOP 5
                    course_category_id AS CourseCategoryId,
                    course_category_name AS CourseCategoryName
                FROM
                    course_category
                ORDER BY
                    course_category_name;
            ";

            var recommendedCourses = new List<ManagementCourseViewModel>();
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

                // Execute the recommended courses query
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var course = new ManagementCourseViewModel
                            {
                                CourseId = reader["CourseId"].ToString(),
                                CourseName = reader["CourseName"].ToString(),
                                CourseDescription = reader["CourseDescription"].ToString(),
                                CourseStatus = reader["CourseStatus"] as int?,
                                CoursePicture = reader["CoursePicture"].ToString(),
                                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                                CourseCreatedAt = reader.GetDateTime(reader.GetOrdinal("CourseCreatedAt")),
                                CreatedBy = reader["CreatedBy"].ToString(),
                                StarRating = reader["StarRating"] as byte?,
                                CourseCategories = new List<CourseCategory>
                                {
                                    new CourseCategory
                                    {
                                        CourseCategoryName = reader["CourseCategory"].ToString()
                                    }
                                }
                            };
                            recommendedCourses.Add(course);
                        }
                    }
                }
            }

            ViewBag.Categories = categories; // Pass categories to the view using ViewBag

            // Prepare the view model
            var viewModel = new HomePageInstructorViewModel
            {
                RecommendedCourses = recommendedCourses
            };

            return View("~/Views/Home/HomePageInstructor.cshtml", viewModel);
        }
    }
}
