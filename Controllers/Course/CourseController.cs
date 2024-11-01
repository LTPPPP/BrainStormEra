using BrainStormEra.Models;
using BrainStormEra.Views.Course;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security.Claims;

namespace BrainStormEra.Controllers.Course
{
    public class CourseController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<CourseController> _logger;

        public CourseController(IConfiguration configuration, ILogger<CourseController> logger)
        {
            _connectionString = configuration.GetConnectionString("SwpMainContext");
            _logger = logger;
        }

        public ActionResult AddCourse()
        {
            var viewModel = new CreateCourseViewModel();

            // Retrieve course categories using raw SQL
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM course_category";
                var command = new SqlCommand(query, connection);
                connection.Open();
                var reader = command.ExecuteReader();
                var categories = new List<CourseCategory>();
                while (reader.Read())
                {
                    categories.Add(new CourseCategory
                    {
                        CourseCategoryId = reader["course_category_id"].ToString(),
                        CourseCategoryName = reader["course_category_name"].ToString(),
                        // Map other properties if necessary
                    });
                }
                viewModel.CourseCategories = categories;
            }

            return View(viewModel);
        }

        // CREATE 
        [HttpPost]
        public ActionResult AddCourse(CreateCourseViewModel viewmodel)
        {
            var userId = Request.Cookies["user_id"];

            // Generate new Course ID
            string newCourseId;
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT TOP 1 course_id FROM course ORDER BY course_id DESC";
                var command = new SqlCommand(query, connection);
                connection.Open();
                var lastCourseId = command.ExecuteScalar()?.ToString();
                newCourseId = lastCourseId == null ? "CO001" : "CO" + (int.Parse(lastCourseId.Substring(2)) + 1).ToString("D3");
            }
            viewmodel.CourseId = newCourseId;

            // Check for duplicate course name
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM course WHERE course_name = @CourseName AND course_id != @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseName", viewmodel.CourseName);
                command.Parameters.AddWithValue("@CourseId", viewmodel.CourseId);
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    ModelState.AddModelError("CourseName", "The Course Name already exists. Please enter a different name.");

                    // Reload categories
                    viewmodel.CourseCategories = GetCourseCategories();
                    return View(viewmodel);
                }
            }

            // Check if category is selected
            if (viewmodel.CategoryIds == null || !viewmodel.CategoryIds.Any())
            {
                ModelState.AddModelError("CategoryIds", "Please select at least one category.");
                viewmodel.CourseCategories = GetCourseCategories();
                return View(viewmodel);
            }

            // Handle file upload for CoursePicture
            string coursePicturePath = null;
            if (viewmodel.CoursePicture != null && viewmodel.CoursePicture.Length > 0)
            {
                if (viewmodel.CoursePicture.Length > 2 * 1024 * 1024) // Limit file size to 2MB
                {
                    ModelState.AddModelError("CoursePicture", "File size should not exceed 2MB.");
                    return View(viewmodel);
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "Course-img");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Path.GetFileName(viewmodel.CoursePicture.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    viewmodel.CoursePicture.CopyTo(stream);
                }

                coursePicturePath = $"/uploads/Course-img/{fileName}";
            }

            // Insert new course using raw SQL
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "INSERT INTO course (course_id, course_name, course_description, course_status, created_by, course_picture, price) " +
                            "VALUES (@CourseId, @CourseName, @CourseDescription, @CourseStatus, @CreatedBy, @CoursePicture, @Price)";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", newCourseId);
                command.Parameters.AddWithValue("@CourseName", viewmodel.CourseName);
                command.Parameters.AddWithValue("@CourseDescription", viewmodel.CourseDescription ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CourseStatus", 3);
                command.Parameters.AddWithValue("@CreatedBy", userId);
                command.Parameters.AddWithValue("@CoursePicture", coursePicturePath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Price", viewmodel.Price);

                connection.Open();
                command.ExecuteNonQuery();
            }

            // Add selected categories to course
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                foreach (var categoryId in viewmodel.CategoryIds)
                {
                    var query = "INSERT INTO course_category_mapping (course_id, course_category_id) VALUES (@CourseId, @CategoryId)";
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@CourseId", newCourseId);
                    command.Parameters.AddWithValue("@CategoryId", categoryId);
                    command.ExecuteNonQuery();
                }
            }

            return RedirectToAction("CourseManagement");
        }

        // Helper method to get course categories
        private List<CourseCategory> GetCourseCategories()
        {
            var categories = new List<CourseCategory>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM course_category";
                var command = new SqlCommand(query, connection);
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    categories.Add(new CourseCategory
                    {
                        CourseCategoryId = reader["course_category_id"].ToString(),
                        CourseCategoryName = reader["course_category_name"].ToString(),
                    });
                }
            }
            return categories;
        }

        // Edit Course
        [HttpGet]
        public ActionResult EditCourse()
        {
            var courseId = HttpContext.Request.Cookies["CourseId"];

            if (string.IsNullOrEmpty(courseId))
            {
                return RedirectToAction("CourseManagement");
            }

            var course = GetCourseById(courseId);

            if (course == null)
            {
                return RedirectToAction("CourseManagement");
            }

            var selectedCategories = GetCourseCategoriesByCourseId(courseId);

            var viewModel = new EditCourseViewModel
            {
                CourseId = course.CourseId,
                CourseName = course.CourseName,
                CourseCategories = GetCourseCategories(),
                SelectedCategories = selectedCategories,
                CourseDescription = course.CourseDescription,
                CoursePictureFile = course.CoursePicture,
                Price = course.Price
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCourse(EditCourseViewModel viewmodel)
        {
            var course = GetCourseById(viewmodel.CourseId);

            if (course == null)
            {
                return RedirectToAction("CourseManagement");
            }

            // Check for duplicate course name
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM course WHERE course_name = @CourseName AND course_id != @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseName", viewmodel.CourseName);
                command.Parameters.AddWithValue("@CourseId", viewmodel.CourseId);
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    ModelState.AddModelError("CourseName", "The Course Name already exists. Please enter a different name.");
                    viewmodel.CourseCategories = GetCourseCategories();
                    return View(viewmodel);
                }
            }

            // Check if category is selected
            if (viewmodel.CategoryIds == null || !viewmodel.CategoryIds.Any())
            {
                ModelState.AddModelError("CategoryIds", "Please select at least one category.");
                viewmodel.CourseCategories = GetCourseCategories();
                return View(viewmodel);
            }

            // Handle file upload for CoursePicture
            string coursePicturePath = course.CoursePicture;
            if (viewmodel.CoursePicture != null && viewmodel.CoursePicture.Length > 0)
            {
                if (viewmodel.CoursePicture.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("CoursePicture", "File size should not exceed 2MB.");
                    viewmodel.CourseCategories = GetCourseCategories();
                    return View(viewmodel);
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "Course-img");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Path.GetFileName(viewmodel.CoursePicture.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    viewmodel.CoursePicture.CopyTo(stream);
                }

                coursePicturePath = $"/uploads/Course-img/{fileName}";
            }

            // Update course using raw SQL
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "UPDATE course SET course_name = @CourseName, course_description = @CourseDescription, price = @Price, course_picture = @CoursePicture WHERE course_id = @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", viewmodel.CourseId);
                command.Parameters.AddWithValue("@CourseName", viewmodel.CourseName);
                command.Parameters.AddWithValue("@CourseDescription", viewmodel.CourseDescription ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Price", viewmodel.Price);
                command.Parameters.AddWithValue("@CoursePicture", coursePicturePath ?? (object)DBNull.Value);
                connection.Open();
                command.ExecuteNonQuery();
            }

            // Update course categories
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Delete existing categories
                var deleteQuery = "DELETE FROM course_category_mapping WHERE course_id = @CourseId";
                var deleteCommand = new SqlCommand(deleteQuery, connection);
                deleteCommand.Parameters.AddWithValue("@CourseId", viewmodel.CourseId);
                deleteCommand.ExecuteNonQuery();

                // Insert new categories
                foreach (var categoryId in viewmodel.CategoryIds)
                {
                    var insertQuery = "INSERT INTO course_category_mapping (course_id, course_category_id) VALUES (@CourseId, @CategoryId)";
                    var insertCommand = new SqlCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@CourseId", viewmodel.CourseId);
                    insertCommand.Parameters.AddWithValue("@CategoryId", categoryId);
                    insertCommand.ExecuteNonQuery();
                }
            }

            return RedirectToAction("CourseManagement");
        }

        // Helper method to get a course by ID
        private Models.Course GetCourseById(string courseId)
        {
            Models.Course course = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM course WHERE course_id = @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    course = new Models.Course
                    {
                        CourseId = reader["course_id"].ToString(),
                        CourseName = reader["course_name"].ToString(),
                        CourseDescription = reader["course_description"].ToString(),
                        CoursePicture = reader["course_picture"]?.ToString(),
                        Price = reader["price"] != DBNull.Value ? Convert.ToDecimal(reader["price"]) : 0,
                        CourseStatus = Convert.ToInt32(reader["course_status"]),
                        CourseCreatedAt = Convert.ToDateTime(reader["course_created_at"])
                    };
                }
            }
            return course;
        }

        // Helper method to get selected course categories
        private List<CourseCategory> GetCourseCategoriesByCourseId(string courseId)
        {
            var categories = new List<CourseCategory>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT cc.* FROM course_category cc INNER JOIN course_category_mapping ccm ON cc.course_category_id = ccm.course_category_id WHERE ccm.course_id = @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    categories.Add(new CourseCategory
                    {
                        CourseCategoryId = reader["course_category_id"].ToString(),
                        CourseCategoryName = reader["course_category_name"].ToString(),
                    });
                }
            }
            return categories;
        }

        // Course Management
        public ActionResult CourseManagement()
        {
            var userId = Request.Cookies["user_id"];
            var userRole = Request.Cookies["user_role"];

            List<ManagementCourseViewModel> coursesViewModel = new List<ManagementCourseViewModel>();

            switch (userRole)
            {
                case "2":
                    // Instructor courses
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        var query = "SELECT * FROM course WHERE created_by = @UserId";
                        var command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@UserId", userId);
                        connection.Open();
                        var reader = command.ExecuteReader();
                        var courses = new List<Models.Course>();
                        while (reader.Read())
                        {
                            courses.Add(new Models.Course
                            {
                                CourseId = reader["course_id"].ToString(),
                                CourseName = reader["course_name"].ToString(),
                                CourseDescription = reader["course_description"].ToString(),
                                CourseStatus = Convert.ToInt32(reader["course_status"]),
                                CoursePicture = reader["course_picture"]?.ToString(),
                                Price = reader["price"] != DBNull.Value ? Convert.ToDecimal(reader["price"]) : 0,
                                CourseCreatedAt = Convert.ToDateTime(reader["course_created_at"])
                            });
                        }

                        foreach (var course in courses)
                        {
                            // Calculate average rating
                            decimal averageRating = GetAverageRating(course.CourseId);
                            coursesViewModel.Add(new ManagementCourseViewModel
                            {
                                CourseId = course.CourseId,
                                CourseName = course.CourseName,
                                CourseDescription = course.CourseDescription,
                                CourseStatus = course.CourseStatus,
                                CoursePicture = course.CoursePicture,
                                Price = course.Price,
                                CourseCreatedAt = course.CourseCreatedAt,
                                StarRating = (byte)averageRating
                            });
                        }
                    }
                    break;

                default:
                    // All active courses with status = 2
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        var query = "SELECT * FROM course WHERE course_status = 2 ORDER BY course_created_at DESC";
                        var command = new SqlCommand(query, connection);
                        connection.Open();
                        var reader = command.ExecuteReader();
                        var courses = new List<Models.Course>();
                        while (reader.Read())
                        {
                            courses.Add(new Models.Course
                            {
                                CourseId = reader["course_id"].ToString(),
                                CourseName = reader["course_name"].ToString(),
                                CourseDescription = reader["course_description"].ToString(),
                                CourseStatus = Convert.ToInt32(reader["course_status"]),
                                CoursePicture = reader["course_picture"]?.ToString(),
                                Price = reader["price"] != DBNull.Value ? Convert.ToDecimal(reader["price"]) : 0,
                                CourseCreatedAt = Convert.ToDateTime(reader["course_created_at"])
                            });
                        }

                        foreach (var course in courses)
                        {
                            // Calculate average rating
                            decimal averageRating = GetAverageRating(course.CourseId);
                            coursesViewModel.Add(new ManagementCourseViewModel
                            {
                                CourseId = course.CourseId,
                                CourseName = course.CourseName,
                                CourseDescription = course.CourseDescription,
                                CourseStatus = course.CourseStatus,
                                CoursePicture = course.CoursePicture,
                                Price = course.Price,
                                CourseCreatedAt = course.CourseCreatedAt,
                                StarRating = (byte)averageRating
                            });
                        }
                    }
                    break;
            }

            return View("CourseManagement", coursesViewModel);
        }

        // Helper method to get average rating
        private decimal GetAverageRating(string courseId)
        {
            decimal averageRating = 0;
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT AVG(CAST(star_rating AS FLOAT)) FROM feedback WHERE course_id = @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                connection.Open();
                var result = command.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    averageRating = 0;
                }
                else
                {
                    averageRating = Convert.ToDecimal(result);
                }

            }
            return averageRating;
        }

        // DELETE
        public ActionResult ConfirmDelete()
        {
            var courseId = HttpContext.Request.Cookies["course_id"];

            var course = GetCourseById(courseId);

            if (course == null) return RedirectToAction("ErrorPage", "Home");

            var viewModel = new DeleteCourseViewModel
            {
                CourseId = course.CourseId,
                CourseName = course.CourseName,
                CourseCategories = GetCourseCategories(),
                CourseCategoryId = GetCourseCategoriesByCourseId(courseId).FirstOrDefault()?.CourseCategoryId,
                CourseDescription = course.CourseDescription,
                CoursePictureFile = course.CoursePicture,
                Price = course.Price
            };
            return View("DeleteCourse", viewModel);
        }

        [HttpPost]
        public ActionResult DeleteCourse()
        {
            var courseId = HttpContext.Request.Cookies["course_id"];
            var course = GetCourseById(courseId);

            if (course != null)
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    // Delete course
                    var query = "DELETE FROM course WHERE course_id = @CourseId";
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@CourseId", courseId);
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                var userRole = HttpContext.Request.Cookies["user_role"];
                if (userRole == "1")
                {
                    return RedirectToAction("CourseAcceptance", "Course");
                }
                else if (userRole == "2")
                {
                    return RedirectToAction("CourseManagement");
                }
            }
            return RedirectToAction("ErrorPage", "Home");
        }

        // ... Continue rewriting other methods similarly 

        [HttpGet]
        public IActionResult CourseDetail(int page = 1, int pageSize = 4)
        {
            var courseId = Request.Cookies["CourseId"];
            var userId = Request.Cookies["user_id"];
            var userRole = Request.Cookies["user_role"];
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Message"] = "You need to log in to access the course details.";
                // Chuyển hướng đến trang đăng nhập nếu người dùng chưa đăng nhập
                return RedirectToAction("LoginPage", "Login");
            }
            if (string.IsNullOrEmpty(courseId))
            {
                _logger.LogWarning("Course ID is null or empty.");
                return View("ErrorPage", "Course ID not found in cookies.");
            }
            // Kiểm tra Enrollment
            bool isEnrolled = false;
            bool isBanned = false;
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT approved FROM enrollment WHERE user_id = @UserId AND course_id = @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@CourseId", courseId);
                connection.Open();
                var result = command.ExecuteScalar();
                if (result != null)
                {
                    isEnrolled = true;
                    isBanned = !(bool)result;
                }
            }

            ViewBag.IsEnrolled = isEnrolled;
            ViewBag.IsBanned = isBanned;

            // Lấy thông tin Course
            var course = GetCourseById(courseId);
            if (course == null)
            {
                _logger.LogError($"Course not found with ID: {courseId}");
                return View("ErrorPage", "Course not found.");
            }

            // Lấy danh sách Category của khóa học
            var categories = new List<string>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT cc.course_category_name FROM course_category cc " +
                            "JOIN course_category_mapping ccm ON cc.course_category_id = ccm.course_category_id " +
                            "WHERE ccm.course_id = @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    categories.Add(reader["course_category_name"].ToString());
                }
            }
            ViewBag.CourseCategories = categories;

            // Tính toán số lượng học viên đã đăng ký
            int learnersCount = 0;
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT COUNT(*) FROM enrollment WHERE course_id = @CourseId AND approved = 1";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                connection.Open();
                learnersCount = (int)command.ExecuteScalar();
            }
            ViewBag.LearnersCount = learnersCount;

            // Lấy danh sách feedback
            var feedbacks = new List<Feedback>();
            int offset = (page - 1) * pageSize;
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT f.*, a.full_name FROM feedback f " +
                            "JOIN account a ON f.user_id = a.user_id " +
                            "WHERE f.course_id = @CourseId AND f.hidden_status = 0 " +
                            "ORDER BY f.feedback_date DESC " +
                            "OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                command.Parameters.AddWithValue("@Offset", offset);
                command.Parameters.AddWithValue("@PageSize", pageSize);
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var feedback = new Feedback
                    {
                        FeedbackId = reader["feedback_id"].ToString(),
                        CourseId = reader["course_id"].ToString(),
                        UserId = reader["user_id"].ToString(),
                        StarRating = reader["star_rating"] != DBNull.Value ? (byte?)Convert.ToByte(reader["star_rating"]) : null,
                        Comment = reader["comment"].ToString(),
                        FeedbackDate = reader["feedback_date"] != DBNull.Value
                            ? DateOnly.FromDateTime(Convert.ToDateTime(reader["feedback_date"]))
                            : DateOnly.MinValue,
                        User = new Models.Account
                        {
                            FullName = reader["full_name"].ToString()
                        }
                    };
                    feedbacks.Add(feedback);
                }
            }
            ViewBag.Comments = feedbacks;

            // Tính toán tổng số lượng feedback và rating trung bình
            int totalComments = 0;
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT COUNT(*) FROM feedback WHERE course_id = @CourseId AND hidden_status = 0";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                connection.Open();
                totalComments = (int)command.ExecuteScalar();
            }
            ViewBag.TotalComments = totalComments;

            double averageRating = 0;
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT AVG(CAST(star_rating AS FLOAT)) FROM feedback WHERE course_id = @CourseId AND hidden_status = 0";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                connection.Open();
                var result = command.ExecuteScalar();
                averageRating = result != DBNull.Value ? Convert.ToDouble(result) : 0.0;
            }
            ViewBag.AverageRating = averageRating;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalComments / pageSize);

            return View(course);
        }


        public ActionResult CourseAcceptance()
        {
            var pendingCourses = new List<Models.Course>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM course WHERE course_status IN (0, 1, 2) ORDER BY " +
                            "CASE WHEN course_status = 1 THEN 0 ELSE 1 END, course_created_at";
                var command = new SqlCommand(query, connection);
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var course = new Models.Course
                    {
                        CourseId = reader["course_id"].ToString(),
                        CourseName = reader["course_name"].ToString(),
                        CourseStatus = Convert.ToInt32(reader["course_status"]),
                        CourseCreatedAt = Convert.ToDateTime(reader["course_created_at"])
                    };
                    pendingCourses.Add(course);
                }
            }
            return View("CourseAcceptance", pendingCourses);
        }


        [HttpGet]
        public IActionResult ReviewCourse(int page = 1, int pageSize = 4)
        {
            try
            {
                var courseId = Request.Cookies["course_id"];

                if (string.IsNullOrEmpty(courseId))
                {
                    _logger.LogWarning("Course ID is null or empty.");
                    return View("ErrorPage", "Course ID not found in cookies.");
                }

                var course = GetCourseById(courseId);

                if (course == null)
                {
                    _logger.LogError($"Course not found with ID: {courseId}");
                    return View("ErrorPage", "Course not found.");
                }

                // Get learners count
                int learnersCount = 0;
                using (var connection = new SqlConnection(_connectionString))
                {
                    var query = "SELECT COUNT(*) FROM enrollment WHERE course_id = @CourseId AND approved = 1";
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@CourseId", courseId);
                    connection.Open();
                    learnersCount = (int)command.ExecuteScalar();
                }
                ViewBag.LearnersCount = learnersCount;

                // Get feedbacks
                var feedbacks = new List<Feedback>();
                int offset = (page - 1) * pageSize;
                using (var connection = new SqlConnection(_connectionString))
                {
                    var query = "SELECT f.*, a.full_name FROM feedback f " +
                                "JOIN account a ON f.user_id = a.user_id " +
                                "WHERE f.course_id = @CourseId AND f.hidden_status = 0 " +
                                "ORDER BY f.feedback_date DESC " +
                                "OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@CourseId", courseId);
                    command.Parameters.AddWithValue("@Offset", offset);
                    command.Parameters.AddWithValue("@PageSize", pageSize);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var feedback = new Feedback
                        {
                            FeedbackId = reader["feedback_id"].ToString(),
                            CourseId = reader["course_id"].ToString(),
                            UserId = reader["user_id"].ToString(),
                            StarRating = reader["star_rating"] != DBNull.Value ? (byte?)Convert.ToByte(reader["star_rating"]) : null,
                            Comment = reader["comment"].ToString(),
                            FeedbackDate = reader["feedback_date"] != DBNull.Value
               ? DateOnly.FromDateTime(Convert.ToDateTime(reader["feedback_date"]))
               : DateOnly.MinValue, // Hoặc một giá trị mặc định nào đó
                            User = new Models.Account
                            {
                                FullName = reader["full_name"].ToString()
                            }
                        };
                        feedbacks.Add(feedback);
                    }
                }
                ViewBag.Comments = feedbacks;

                // Get total comments
                int totalComments = 0;
                using (var connection = new SqlConnection(_connectionString))
                {
                    var query = "SELECT COUNT(*) FROM feedback WHERE course_id = @CourseId AND hidden_status = 0";
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@CourseId", courseId);
                    connection.Open();
                    totalComments = (int)command.ExecuteScalar();
                }
                ViewBag.TotalComments = totalComments;

                // Get average rating
                double averageRating = 0;
                using (var connection = new SqlConnection(_connectionString))
                {
                    var query = "SELECT AVG(CAST(star_rating AS FLOAT)) FROM feedback WHERE course_id = @CourseId AND hidden_status = 0";
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@CourseId", courseId);
                    connection.Open();
                    var result = command.ExecuteScalar();
                    averageRating = result != DBNull.Value ? Convert.ToDouble(result) : 0.0;
                }
                ViewBag.AverageRating = averageRating;

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalComments / pageSize);

                // Get created by
                string createdBy = "Unknown";
                using (var connection = new SqlConnection(_connectionString))
                {
                    var query = "SELECT full_name FROM account WHERE user_id = @UserId";
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UserId", course.CreatedBy);
                    connection.Open();
                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        createdBy = result.ToString();
                    }
                }
                ViewBag.CreatedBy = createdBy;

                // Get rating percentages
                var ratingPercentages = new Dictionary<int, double>();
                for (int i = 1; i <= 5; i++)
                {
                    int count = 0;
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        var query = "SELECT COUNT(*) FROM feedback WHERE course_id = @CourseId AND star_rating = @Rating";
                        var command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@CourseId", courseId);
                        command.Parameters.AddWithValue("@Rating", i);
                        connection.Open();
                        count = (int)command.ExecuteScalar();
                    }
                    ratingPercentages[i] = totalComments > 0 ? (double)count / totalComments : 0;
                }
                ViewBag.RatingPercentages = ratingPercentages;

                return View(course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while loading the course.");
                return View("ErrorPage", "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public IActionResult ChangeStatus(string courseId, int status)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "UPDATE course SET course_status = @Status WHERE course_id = @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                command.Parameters.AddWithValue("@Status", status);
                connection.Open();
                command.ExecuteNonQuery();
            }
            return RedirectToAction("CourseAcceptance", "Course");
        }

        [HttpPost]
        public IActionResult Enroll(string courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            Models.Account user = null;
            Models.Course course = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                // Lấy thông tin người dùng
                var userQuery = "SELECT * FROM account WHERE user_id = @UserId";
                var userCommand = new SqlCommand(userQuery, connection);
                userCommand.Parameters.AddWithValue("@UserId", userId);

                // Lấy thông tin khóa học
                var courseQuery = "SELECT * FROM course WHERE course_id = @CourseId";
                var courseCommand = new SqlCommand(courseQuery, connection);
                courseCommand.Parameters.AddWithValue("@CourseId", courseId);

                connection.Open();

                using (var reader = userCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        user = new Models.Account
                        {
                            UserId = reader["user_id"].ToString(),
                            PaymentPoint = Convert.ToDecimal(reader["payment_point"])
                        };
                    }
                }

                // Reset connection để chạy command thứ hai
                connection.Close();
                connection.Open();

                using (var reader = courseCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        course = new Models.Course
                        {
                            CourseId = reader["course_id"].ToString(),
                            Price = Convert.ToDecimal(reader["price"])
                        };
                    }
                }
            }

            if (user == null || course == null)
            {
                return View("ErrorPage", "User or Course not found.");
            }

            if (user.PaymentPoint >= course.Price)
            {
                string newEnrollmentId = GenerateNewEnrollmentId();

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Tạo bản ghi trong bảng enrollment
                    var enrollmentQuery = "INSERT INTO enrollment (enrollment_id, user_id, course_id, enrollment_status, approved, enrollment_created_at) " +
                                          "VALUES (@EnrollmentId, @UserId, @CourseId, 1, 1, @CreatedAt)";
                    var enrollmentCommand = new SqlCommand(enrollmentQuery, connection);
                    enrollmentCommand.Parameters.AddWithValue("@EnrollmentId", newEnrollmentId);
                    enrollmentCommand.Parameters.AddWithValue("@UserId", userId);
                    enrollmentCommand.Parameters.AddWithValue("@CourseId", courseId);
                    enrollmentCommand.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    enrollmentCommand.ExecuteNonQuery();

                    // Trừ điểm của người dùng sau khi đăng ký khóa học thành công
                    var updateUserQuery = "UPDATE account SET payment_point = payment_point - @Price WHERE user_id = @UserId";
                    var updateUserCommand = new SqlCommand(updateUserQuery, connection);
                    updateUserCommand.Parameters.AddWithValue("@Price", course.Price);
                    updateUserCommand.Parameters.AddWithValue("@UserId", userId);
                    updateUserCommand.ExecuteNonQuery();
                }

                return RedirectToAction("CourseDetail", new { id = courseId });
            }
            else
            {
                TempData["ErrorMessage"] = "You do not have enough points to enroll in this course.";
                return RedirectToAction("CourseDetail", new { id = courseId });
            }
        }


        [HttpPost]
        public IActionResult RequestToAdmin(string courseId)
        {
            bool hasChaptersAndLessons = false;
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT COUNT(*) FROM chapter ch JOIN lesson l ON ch.chapter_id = l.chapter_id WHERE ch.course_id = @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                connection.Open();
                hasChaptersAndLessons = Convert.ToInt32(command.ExecuteScalar()) > 0;
            }

            if (hasChaptersAndLessons)
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var query = "UPDATE course SET course_status = 1 WHERE course_id = @CourseId";
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@CourseId", courseId);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                return Json(new { success = true, message = "Request sent to Admin successfully." });
            }
            else
            {
                return Json(new { success = false, message = "The course must have at least 1 chapter and 1 lesson. Back to edit and add more" });
            }
        }

        // Helper method to generate new enrollment ID
        private string GenerateNewEnrollmentId()
        {
            string maxEnrollmentId = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT TOP 1 enrollment_id FROM enrollment ORDER BY enrollment_id DESC";
                var command = new SqlCommand(query, connection);
                connection.Open();
                maxEnrollmentId = command.ExecuteScalar()?.ToString();
            }

            int newIdNumber = 1;
            if (!string.IsNullOrEmpty(maxEnrollmentId) && maxEnrollmentId.Length > 2)
            {
                newIdNumber = int.Parse(maxEnrollmentId.Substring(2)) + 1;
            }

            string newEnrollmentId = "EN" + newIdNumber.ToString("D3");
            return newEnrollmentId;
        }

    }
}
