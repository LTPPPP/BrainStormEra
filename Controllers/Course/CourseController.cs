using BrainStormEra.Models;
using BrainStormEra.Views.Course;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace BrainStormEra.Controllers.Course
{
    public class CourseController : Controller
    {
        private readonly SwpMainContext _context;
        private readonly ILogger<CourseController> _logger;

        public CourseController(SwpMainContext context, ILogger<CourseController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public ActionResult AddCourse()
        {
            var viewModel = new CreateCourseViewModel
            {
                CourseCategories = _context.CourseCategories.FromSqlRaw("SELECT * FROM course_category").ToList()
            };
            return View(viewModel);
        }

        // CREATE 
        [HttpPost]
        public ActionResult AddCourse(CreateCourseViewModel viewmodel)
        {
            var userId = Request.Cookies["user_id"];
            var lastCourse = _context.Courses
                .FromSqlRaw("SELECT TOP 1 * FROM course ORDER BY course_id DESC")
                .FirstOrDefault();

            var newCourseId = lastCourse == null ? "CO001" : "CO" + (int.Parse(lastCourse.CourseId.Substring(2)) + 1).ToString("D3");
            viewmodel.CourseId = newCourseId;

            if (!ModelState.IsValid)
            {
                // Check for duplicate course name
                var existingCourse = _context.Courses
                    .FromSqlInterpolated($"SELECT * FROM course WHERE course_name = {viewmodel.CourseName} AND course_id != {viewmodel.CourseId}")
                    .FirstOrDefault();

                if (existingCourse != null)
                {
                    ModelState.AddModelError("CourseName", "The Course Name already exists. Please enter a different name.");
                    viewmodel.CourseCategories = _context.CourseCategories.FromSqlRaw("SELECT * FROM course_category").ToList();
                    return View(viewmodel);
                }

                // Check if category is selected
                if (viewmodel.CategoryIds == null || !viewmodel.CategoryIds.Any())
                {
                    ModelState.AddModelError("CategoryIds", "Please select at least one category.");
                    viewmodel.CourseCategories = _context.CourseCategories.FromSqlRaw("SELECT * FROM course_category").ToList();
                    return View(viewmodel);
                }

                var obj = new Models.Course
                {
                    CourseId = newCourseId,
                    CourseName = viewmodel.CourseName,
                    CourseDescription = viewmodel.CourseDescription,
                    CourseStatus = 3,
                    CreatedBy = userId,
                    CoursePicture = viewmodel.CoursePicture.FileName,
                    Price = viewmodel.Price,
                };

                // Handle file upload for CoursePicture
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

                    obj.CoursePicture = $"/uploads/Course-img/{fileName}";
                }

                // Add selected categories to course
                foreach (var categoryId in viewmodel.CategoryIds)
                {
                    var category = _context.CourseCategories
                        .FromSqlInterpolated($"SELECT * FROM course_category WHERE course_category_id = {categoryId}")
                        .FirstOrDefault();
                    if (category != null)
                    {
                        obj.CourseCategories.Add(category);
                    }
                }

                _context.Courses.Add(obj);
                _context.SaveChanges();
                return RedirectToAction("CourseManagement");
            }
            return RedirectToAction("CourseManagement");
        }

        // Edit Course
        [HttpGet]
        public ActionResult EditCourse()
        {
            var courseId = HttpContext.Request.Cookies["CourseId"];

            var course = _context.Courses
                .FromSqlInterpolated($"SELECT * FROM course WHERE course_id = {courseId}")
                .Include(c => c.CourseCategories)
                .FirstOrDefault();

            if (course == null)
            {
                return RedirectToAction("CourseManagement");
            }

            var viewModel = new EditCourseViewModel
            {
                CourseId = course.CourseId,
                CourseName = course.CourseName,
                CourseCategories = _context.CourseCategories.FromSqlRaw("SELECT * FROM course_category").ToList(),
                SelectedCategories = course.CourseCategories.ToList(),
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
            var course = _context.Courses
                .FromSqlInterpolated($"SELECT * FROM course WHERE course_id = {viewmodel.CourseId}")
                .Include(c => c.CourseCategories)
                .FirstOrDefault();

            if (course == null)
            {
                return RedirectToAction("CourseManagement");
            }

            var existingCourse = _context.Courses
                .FromSqlInterpolated($"SELECT * FROM course WHERE course_name = {viewmodel.CourseName} AND course_id != {viewmodel.CourseId}")
                .FirstOrDefault();

            if (existingCourse != null)
            {
                ModelState.AddModelError("CourseName", "The Course Name already exists. Please enter a different name.");
                viewmodel.CourseCategories = _context.CourseCategories.FromSqlRaw("SELECT * FROM course_category").ToList();
            }

            if (viewmodel.CategoryIds == null || !viewmodel.CategoryIds.Any())
            {
                ModelState.AddModelError("CategoryIds", "Please select at least one category.");
                viewmodel.CourseCategories = _context.CourseCategories.FromSqlRaw("SELECT * FROM course_category").ToList();
                return View(viewmodel);
            }

            course.CourseName = viewmodel.CourseName;
            course.CourseDescription = viewmodel.CourseDescription;
            course.Price = viewmodel.Price;

            if (viewmodel.CoursePicture != null && viewmodel.CoursePicture.Length > 0)
            {
                if (viewmodel.CoursePicture.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("CoursePicture", "File size should not exceed 2MB.");
                    viewmodel.CourseCategories = _context.CourseCategories.FromSqlRaw("SELECT * FROM course_category").ToList();
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

                course.CoursePicture = $"/uploads/Course-img/{fileName}";
            }

            var selectedCategoryIds = viewmodel.CategoryIds ?? new List<string>();
            var existingCategoryIds = course.CourseCategories.Select(c => c.CourseCategoryId).ToList();

            foreach (var categoryId in existingCategoryIds)
            {
                if (!selectedCategoryIds.Contains(categoryId))
                {
                    var categoryToRemove = course.CourseCategories.FirstOrDefault(c => c.CourseCategoryId == categoryId);
                    if (categoryToRemove != null)
                    {
                        course.CourseCategories.Remove(categoryToRemove);
                    }
                }
            }

            foreach (var categoryId in selectedCategoryIds)
            {
                if (!existingCategoryIds.Contains(categoryId))
                {
                    var categoryToAdd = _context.CourseCategories
                        .FromSqlInterpolated($"SELECT * FROM course_category WHERE course_category_id = {categoryId}")
                        .FirstOrDefault();
                    if (categoryToAdd != null)
                    {
                        course.CourseCategories.Add(categoryToAdd);
                    }
                }
            }

            _context.SaveChanges();
            return RedirectToAction("CourseManagement");
        }

        // Course Management
        public ActionResult CourseManagement()
        {
            var userId = Request.Cookies["user_id"];
            var user_role = Request.Cookies["user_role"];

            switch (user_role)
            {
                case "2":
                    // SQL query for instructor courses, filtered by the created_by field
                    var instructorCourses = _context.Courses
                        .FromSqlInterpolated($"SELECT * FROM course WHERE created_by = {userId}")
                        .ToList();

                    // Create the management view models and calculate the StarRating using raw SQL
                    var instructorCoursesViewModel = instructorCourses.Select(course => new ManagementCourseViewModel
                    {
                        CourseId = course.CourseId,
                        CourseName = course.CourseName,
                        CourseDescription = course.CourseDescription,
                        CourseStatus = course.CourseStatus,
                        CoursePicture = course.CoursePicture,
                        Price = course.Price,
                        CourseCreatedAt = course.CourseCreatedAt,
                        StarRating = (byte?)_context.Feedbacks
                            .FromSqlInterpolated($"SELECT * FROM feedback WHERE course_id = {course.CourseId}")
                            .Average(f => (double?)f.StarRating) ?? 0
                    }).ToList();

                    return View("CourseManagement", instructorCoursesViewModel);

                default:
                    // SQL query for all active courses with status = 2, ordered by creation date descending
                    var courseList = _context.Courses
                        .FromSqlRaw("SELECT * FROM course WHERE course_status = 2 ORDER BY course_created_at DESC")
                        .ToList();

                    // Map courses to ManagementCourseViewModel
                    var courseListViewModel = courseList.Select(course => new ManagementCourseViewModel
                    {
                        CourseId = course.CourseId,
                        CourseName = course.CourseName,
                        CourseDescription = course.CourseDescription,
                        CourseStatus = course.CourseStatus,
                        CoursePicture = course.CoursePicture,
                        Price = course.Price,
                        CourseCreatedAt = course.CourseCreatedAt,
                        StarRating = (byte?)_context.Feedbacks
                            .FromSqlInterpolated($"SELECT * FROM feedback WHERE course_id = {course.CourseId}")
                            .Average(f => (double?)f.StarRating) ?? 0
                    }).ToList();

                    return View("CourseManagement", courseListViewModel);
            }
        }

        // DELETE
        public ActionResult ConfirmDelete()
        {
            var courseId = HttpContext.Request.Cookies["CourseId"];

            var course = _context.Courses
                .FromSqlInterpolated($"SELECT * FROM course WHERE course_id = {courseId}")
                .Include(c => c.CourseCategories)
                .FirstOrDefault();

            if (course == null) return RedirectToAction("ErrorPage", "Home");

            var viewModel = new DeleteCourseViewModel
            {
                CourseId = course.CourseId,
                CourseName = course.CourseName,
                CourseCategories = _context.CourseCategories.FromSqlRaw("SELECT * FROM course_category").ToList(),
                CourseCategoryId = course.CourseCategories.FirstOrDefault()?.CourseCategoryId,
                CourseDescription = course.CourseDescription,
                CoursePictureFile = course.CoursePicture,
                Price = course.Price
            };
            return View("DeleteCourse", viewModel);
        }

        [HttpPost]
        public ActionResult DeleteCourse()
        {
            var courseId = HttpContext.Request.Cookies["CourseId"];
            var course = _context.Courses
                .FromSqlInterpolated($"SELECT * FROM course WHERE course_id = {courseId}")
                .FirstOrDefault();

            if (course != null)
            {
                _context.Courses.Remove(course);
                _context.SaveChanges();

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

        [HttpGet]
        public IActionResult CourseDetail(int page = 1, int pageSize = 4)
        {
            try
            {
                var courseId = Request.Cookies["CourseId"];
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = Request.Cookies["user_role"];

                if (string.IsNullOrEmpty(courseId))
                {
                    _logger.LogWarning("Course ID is null or empty.");
                    return View("ErrorPage", "Course ID not found in cookies.");
                }

                var course = _context.Courses
                    .FromSqlInterpolated($"SELECT * FROM course WHERE course_id = {courseId}")
                    .Include(c => c.Chapters)
                        .ThenInclude(ch => ch.Lessons)
                    .Include(c => c.CourseCategories)
                    .FirstOrDefault();

                if (course == null)
                {
                    _logger.LogError($"Course not found with ID: {courseId}");
                    return View("ErrorPage", "Course not found.");
                }

                var categories = _context.CourseCategories
                    .FromSqlInterpolated($"SELECT cc.course_category_name FROM course_category cc JOIN course_category_mapping ccm ON cc.course_category_id = ccm.course_category_id WHERE ccm.course_id = {courseId}")
                    .Select(cc => cc.CourseCategoryName)
                    .ToList();
                ViewBag.CourseCategories = categories;

                bool isEnrolled = false;
                if (userRole == "3" && !string.IsNullOrEmpty(userId))
                {
                    isEnrolled = _context.Enrollments
                        .FromSqlInterpolated($"SELECT * FROM enrollment WHERE user_id = {userId} AND course_id = {courseId}")
                        .Any();
                }
                ViewBag.IsEnrolled = isEnrolled;

                var learnersCount = _context.Enrollments
                    .FromSqlInterpolated($"SELECT * FROM enrollment WHERE course_id = {courseId} AND approved = 1")
                    .Count();
                ViewBag.LearnersCount = learnersCount;

                var feedbacks = _context.Feedbacks
                    .FromSqlInterpolated($"SELECT * FROM feedback WHERE course_id = {courseId} AND hidden_status = 0 ORDER BY feedback_date DESC OFFSET {(page - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY")
                    .Include(f => f.User)
                    .ToList();

                var totalComments = _context.Feedbacks
                    .FromSqlInterpolated($"SELECT * FROM feedback WHERE course_id = {courseId} AND hidden_status = 0")
                    .Count();

                var averageRating = _context.Feedbacks
                    .FromSqlInterpolated($"SELECT * FROM feedback WHERE course_id = {courseId} AND hidden_status = 0")
                    .Select(f => (double?)f.StarRating)
                    .Average() ?? 0.0;

                ViewBag.TotalComments = totalComments;
                ViewBag.AverageRating = averageRating;
                ViewBag.Comments = feedbacks;

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalComments / pageSize);

                var createdBy = _context.Accounts
                    .FromSqlInterpolated($"SELECT full_name FROM account WHERE user_id = {course.CreatedBy}")
                    .Select(a => a.FullName)
                    .FirstOrDefault() ?? "Unknown";
                ViewBag.CreatedBy = createdBy;

                var ratingPercentages = new Dictionary<int, double>();
                for (int i = 1; i <= 5; i++)
                {
                    var count = _context.Feedbacks
                        .FromSqlInterpolated($"SELECT * FROM feedback WHERE course_id = {courseId} AND star_rating = {i}")
                        .Count();
                    ratingPercentages[i] = totalComments > 0 ? (double)count / totalComments : 0;
                }
                ViewBag.RatingPercentages = ratingPercentages;

                return View(course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while loading the course.");
                var errorViewModel = new ErrorViewModel
                {
                    RequestId = "An unexpected error occurred."
                };
                return View("Error", errorViewModel);
            }
        }
    }
}
