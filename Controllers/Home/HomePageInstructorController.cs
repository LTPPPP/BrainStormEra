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
        STUFF((
            SELECT DISTINCT ', ' + cc.course_category_name
            FROM course_category_mapping AS ccm
            JOIN course_category AS cc ON ccm.course_category_id = cc.course_category_id
            WHERE ccm.course_id = c.course_id
            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS CourseCategories
    FROM 
        course c
        LEFT JOIN account a ON c.created_by = a.user_id
        LEFT JOIN enrollment e ON c.course_id = e.course_id
        LEFT JOIN feedback f ON c.course_id = f.course_id
    WHERE 
        c.course_status = 2
    GROUP BY 
        c.course_id, c.course_name, c.course_description, c.course_status, 
        c.course_picture, c.price, c.course_created_at, a.full_name
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

                // Dictionary to keep track of courses and their categories
                var courseDictionary = new Dictionary<string, ManagementCourseViewModel>();

                // Execute the recommended courses query
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var courseId = reader["CourseId"].ToString();

                            // Check if course already exists in dictionary
                            if (!courseDictionary.TryGetValue(courseId, out var course))
                            {
                                course = new ManagementCourseViewModel
                                {
                                    CourseId = courseId,
                                    CourseName = reader["CourseName"].ToString(),
                                    CourseDescription = reader["CourseDescription"].ToString(),
                                    CourseStatus = reader["CourseStatus"] as int?,
                                    CoursePicture = reader["CoursePicture"].ToString(),
                                    Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                                    CourseCreatedAt = reader.GetDateTime(reader.GetOrdinal("CourseCreatedAt")),
                                    CreatedBy = reader["CreatedBy"].ToString(),
                                    StarRating = reader["StarRating"] as byte?,
                                    CourseCategories = new List<CourseCategory>()
                                };
                                courseDictionary[courseId] = course;
                            }

                            // Add each category to the course as individual items in the list
                            var categoriesString = reader["CourseCategories"].ToString();
                            if (!string.IsNullOrEmpty(categoriesString))
                            {
                                foreach (var categoryName in categoriesString.Split(','))
                                {
                                    var trimmedCategoryName = categoryName.Trim();
                                    if (!string.IsNullOrEmpty(trimmedCategoryName) &&
                                        course.CourseCategories.All(c => c.CourseCategoryName != trimmedCategoryName))
                                    {
                                        course.CourseCategories.Add(new CourseCategory
                                        {
                                            CourseCategoryName = trimmedCategoryName
                                        });
                                    }
                                }
                            }
                        }
                    }
                }

                // Convert dictionary values to list
                recommendedCourses = courseDictionary.Values.ToList();
            }

            // Pass categories to the view using ViewBag
            ViewBag.Categories = categories;

            // Prepare the view model
            var viewModel = new HomePageInstructorViewModel
            {
                RecommendedCourses = recommendedCourses
            };
            return View("~/Views/Home/HomePageInstructor.cshtml", viewModel);
        }
    }
}