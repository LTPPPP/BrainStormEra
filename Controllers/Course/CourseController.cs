using BrainStormEra.Models;
using BrainStormEra.Repo;
using BrainStormEra.Repo.Chapter;
using BrainStormEra.Repo.Course;
using BrainStormEra.Views.Course;
using Microsoft.AspNetCore.Http.HttpResults;
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
        private readonly CourseRepo _courseRepo;
        private readonly LessonRepo _lessonRepo;
        private readonly ChapterRepo _chapterRepo;
        public CourseController(IConfiguration configuration, ILogger<CourseController> logger, CourseRepo courseRepo, ChapterRepo chapterRepo, LessonRepo lessonRepo)
        {
            _connectionString = configuration.GetConnectionString("SwpMainContext");
            _logger = logger;
            _courseRepo = courseRepo;
            _chapterRepo = chapterRepo;
            _lessonRepo = lessonRepo;
        }

        public async Task<ActionResult> AddCourse()
        {
            try
            {
                var viewModel = new CreateCourseViewModel
                {
                    CourseCategories = await _courseRepo.GetCourseCategoriesAsync()
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while loading the course creation page.");
                return View("Error", "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> AddCourse(CreateCourseViewModel viewModel)
        {
            var userId = Request.Cookies["user_id"];

            viewModel.CourseId = await _courseRepo.GenerateNewCourseIdAsync();

            if (await _courseRepo.IsCourseNameExistsAsync(viewModel.CourseName, viewModel.CourseId))
            {
                ModelState.AddModelError("CourseName", "The Course Name already exists. Please enter a different name.");
                viewModel.CourseCategories = await _courseRepo.GetCourseCategoriesAsync();
                return View(viewModel);
            }

            if (viewModel.CategoryIds == null || !viewModel.CategoryIds.Any())
            {
                ModelState.AddModelError("CategoryIds", "Please select at least one category.");
                viewModel.CourseCategories = await _courseRepo.GetCourseCategoriesAsync();
                return View(viewModel);
            }

            if (viewModel.CategoryIds.Count > 5)
            {
                ModelState.AddModelError("CategoryIds", "You can select up to 5 categories.");
                viewModel.CourseCategories = await _courseRepo.GetCourseCategoriesAsync();
                return View(viewModel);
            }

            string coursePicturePath = null;
            if (viewModel.CoursePicture != null && viewModel.CoursePicture.Length > 0)
            {
                if (viewModel.CoursePicture.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("CoursePicture", "File size should not exceed 10MB.");
                    return View(viewModel);
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "Course-img");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var fileName = Path.GetFileName(viewModel.CoursePicture.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await viewModel.CoursePicture.CopyToAsync(stream);
                }

                coursePicturePath = $"/uploads/Course-img/{fileName}";
            }

            var newCourse = new Models.Course
            {
                CourseId = viewModel.CourseId,
                CourseName = viewModel.CourseName,
                CourseDescription = viewModel.CourseDescription,
                CourseStatus = 3,
                CreatedBy = userId,
                CoursePicture = coursePicturePath,
                Price = viewModel.Price
            };

            await _courseRepo.AddCourseAsync(newCourse);
            await _courseRepo.AddCourseCategoriesAsync(viewModel.CourseId, viewModel.CategoryIds);

            return RedirectToAction("CourseManagement");
        }


        [HttpGet]
        public async Task<ActionResult> EditCourse()
        {
            var courseId = HttpContext.Request.Cookies["CourseId"];

            if (string.IsNullOrEmpty(courseId))
            {
                return RedirectToAction("CourseManagement");
            }

            var course = await _courseRepo.GetCourseByIdAsync(courseId);

            if (course == null)
            {
                return RedirectToAction("CourseManagement");
            }

            // Check if the course is already approved
            if (course.CourseStatus == 2) // Assuming 2 is the status for approved courses
            {
                TempData["ErrorMessage"] = "This course has already been approved and cannot be edited.";
                return RedirectToAction("CourseManagement");
            }

            var allCategories = await _courseRepo.GetCourseCategoriesAsync(); // Get all categories
            var selectedCategories = await _courseRepo.GetCourseCategoriesByCourseIdAsync(courseId); // Get selected categories for the course

            var viewModel = new EditCourseViewModel
            {
                CourseId = course.CourseId,
                CourseName = course.CourseName,
                CourseCategories = allCategories,           // All categories
                SelectedCategories = selectedCategories,     // Only selected categories
                CourseDescription = course.CourseDescription,
                CoursePictureFile = course.CoursePicture,
                Price = course.Price
            };

            return View(viewModel);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditCourse(EditCourseViewModel viewModel)
        {
            var course = await _courseRepo.GetCourseByIdAsync(viewModel.CourseId);
            var courseId = HttpContext.Request.Cookies["CourseId"];
            if (course == null)
            {
                return RedirectToAction("CourseManagement");
            }

            // Check if the course is already approved
            if (course.CourseStatus == 2) // Assuming 2 is the status for approved courses
            {
                TempData["ErrorMessage"] = "This course has already been approved and cannot be edited.";
                return RedirectToAction("CourseManagement");
            }

            // Check if the course name already exists
            if (await _courseRepo.IsCourseNameExistsAsync(viewModel.CourseName, viewModel.CourseId))
            {
                ModelState.AddModelError("CourseName", "The Course Name already exists. Please enter a different name.");
                viewModel.CourseCategories = await _courseRepo.GetCourseCategoriesAsync();
                var selectedCategories = await _courseRepo.GetCourseCategoriesByCourseIdAsync(courseId); // Get selected categories for the course
                viewModel.SelectedCategories = selectedCategories;   // Only selected categories
                return View(viewModel);
            }

            // Check if categories are selected
            if (viewModel.CategoryIds == null || !viewModel.CategoryIds.Any())
            {
                ModelState.AddModelError("CategoryIds", "Please select at least one category.");
                viewModel.CourseCategories = await _courseRepo.GetCourseCategoriesAsync();
                var selectedCategories = await _courseRepo.GetCourseCategoriesByCourseIdAsync(courseId); // Get selected categories for the course
                viewModel.SelectedCategories = selectedCategories;   // Only selected categories
                return View(viewModel);
            }

            if (viewModel.CategoryIds.Count > 5)
            {
                ModelState.AddModelError("CategoryIds", "You can select up to 5 categories.");
                viewModel.CourseCategories = await _courseRepo.GetCourseCategoriesAsync();
                var selectedCategories = await _courseRepo.GetCourseCategoriesByCourseIdAsync(courseId); // Get selected categories for the course
                viewModel.SelectedCategories = selectedCategories;   // Only selected categories
                return View(viewModel);
            }

            // Handle course picture upload
            string coursePicturePath = course.CoursePicture;
            if (viewModel.CoursePicture != null && viewModel.CoursePicture.Length > 0)
            {
                if (viewModel.CoursePicture.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("CoursePicture", "File size should not exceed 2MB.");
                    viewModel.CourseCategories = await _courseRepo.GetCourseCategoriesAsync();
                    var selectedCategories = await _courseRepo.GetCourseCategoriesByCourseIdAsync(courseId); // Get selected categories for the course
                    viewModel.SelectedCategories = selectedCategories;   // Only selected categories
                    return View(viewModel);
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "Course-img");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Path.GetFileName(viewModel.CoursePicture.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await viewModel.CoursePicture.CopyToAsync(stream);
                }

                coursePicturePath = $"/uploads/Course-img/{fileName}";
            }

            // Update course information
            await _courseRepo.UpdateCourseAsync(viewModel, coursePicturePath);

            // Update course categories
            await _courseRepo.UpdateCourseCategoriesAsync(viewModel.CourseId, viewModel.CategoryIds);

            return RedirectToAction("CourseManagement");
        }



        // Helper method to get a course by ID
        private async Task<Models.Course> GetCourseByIdAsync(string courseId)
        {
            return await _courseRepo.GetCourseByIdAsync(courseId);
        }

        private async Task<List<CourseCategory>> GetCourseCategoriesByCourseIdAsync(string courseId)
        {
            return await _courseRepo.GetCourseCategoriesByCourseIdAsync(courseId);
        }

        public async Task<ActionResult> CourseManagement()
        {
            var userId = HttpContext.Request.Cookies["user_id"];
            var userRole = HttpContext.Request.Cookies["user_role"];

            List<ManagementCourseViewModel> coursesViewModel = new List<ManagementCourseViewModel>();
            var categories = await _courseRepo.GetTopCourseCategoriesAsync();
            var categoryCounts = new Dictionary<string, int>();

            foreach (var category in categories)
            {
                int courseCount = await _courseRepo.GetCourseCountByCategoryAsync(category.CourseCategoryId);
                categoryCounts[category.CourseCategoryId] = courseCount;
            }

            ViewBag.Categories = categories;
            ViewBag.CategoryCounts = categoryCounts;

            if (userRole == "2")
            {
                // Lấy danh sách khóa học của giảng viên
                var courses = await _courseRepo.GetInstructorCoursesAsync(userId);
                foreach (var course in courses)
                {
                    // Lấy rating trung bình
                    double averageRating = await _courseRepo.GetAverageRatingAsync(course.CourseId);

                    // Lấy danh sách các category
                    var courseCategories = await _courseRepo.GetCourseCategoriesByCourseIdAsync(course.CourseId);

                    coursesViewModel.Add(new ManagementCourseViewModel
                    {
                        CourseId = course.CourseId,
                        CourseName = course.CourseName,
                        CourseDescription = course.CourseDescription,
                        CourseStatus = course.CourseStatus,
                        CoursePicture = course.CoursePicture,
                        Price = course.Price,
                        CourseCreatedAt = course.CourseCreatedAt,
                        StarRating = (byte)averageRating,
                        CourseCategories = courseCategories,
                        CreatedBy = course.CreatedBy
                    });
                }
            }
            else
            {
                // Lấy tất cả khóa học đang active
                var courses = await _courseRepo.GetAllActiveCoursesAsync();
                foreach (var course in courses)
                {
                    // Lấy rating trung bình
                    double averageRating = await _courseRepo.GetAverageRatingAsync(course.CourseId);

                    // Lấy danh sách các category
                    var courseCategories = await _courseRepo.GetCourseCategoriesByCourseIdAsync(course.CourseId);
                    string creatorName = await _courseRepo.GetCourseCreatorNameAsync(course.CourseId);
                    coursesViewModel.Add(new ManagementCourseViewModel
                    {
                        CourseId = course.CourseId,
                        CourseName = course.CourseName,
                        CourseDescription = course.CourseDescription,
                        CourseStatus = course.CourseStatus,
                        CoursePicture = course.CoursePicture,
                        Price = course.Price,
                        CourseCreatedAt = course.CourseCreatedAt,
                        StarRating = (byte)averageRating,
                        CourseCategories = courseCategories,
                        CreatedBy = creatorName
                    });
                }
            }

            return View("CourseManagement", coursesViewModel);
        }
        public async Task<ActionResult> FilterCoursesByCategoryAsync()
        {
            var userId = HttpContext.Request.Cookies["user_id"];
            var userRole = HttpContext.Request.Cookies["user_role"];
            var categoryId = HttpContext.Request.Cookies["CategoryId"];

            var coursesViewModel = new List<ManagementCourseViewModel>();
            var categories = await _courseRepo.GetTopCourseCategoriesAsync();

            // Prepare a dictionary to store course counts per category
            var categoryCounts = new Dictionary<string, int>();
            foreach (var category in categories)
            {
                int count = await _courseRepo.GetCourseCountByCategoryAsync(category.CourseCategoryId);
                categoryCounts[category.CourseCategoryId] = count;
            }

            int totalCourseCount = await _courseRepo.GetTotalCourseCountAsync();
            int pendingCourseCount = await _courseRepo.GetPendingCourseCountAsync();
            int acceptedCourseCount = await _courseRepo.GetAcceptedCourseCountAsync();
            int rejectedCourseCount = await _courseRepo.GetRejectedCourseCountAsync();
            int notApprovedCourseCount = await _courseRepo.GetNotApprovedCourseCountAsync();

            ViewBag.TotalCourseCount = totalCourseCount;
            ViewBag.PendingCourseCount = pendingCourseCount;
            ViewBag.AcceptedCourseCount = acceptedCourseCount;
            ViewBag.RejectedCourseCount = rejectedCourseCount;
            ViewBag.NotApprovedCourseCount = notApprovedCourseCount;

            // Fetch courses filtered by category
            List<Models.Course> courses;
            if (userRole == "2")
            {
                courses = _courseRepo.GetInstructorCoursesByCategory(userId, categoryId);
            }
            else
            {
                courses = _courseRepo.GetActiveCoursesByCategory(categoryId);
            }

            foreach (var course in courses)
            {
                double averageRating = await _courseRepo.GetAverageRatingAsync(course.CourseId);
                var courseCategories = await _courseRepo.GetCourseCategoriesByCourseIdAsync(course.CourseId);

                coursesViewModel.Add(new ManagementCourseViewModel
                {
                    CourseId = course.CourseId,
                    CourseName = course.CourseName,
                    CourseDescription = course.CourseDescription,
                    CourseStatus = course.CourseStatus,
                    CoursePicture = course.CoursePicture,
                    Price = course.Price,
                    CourseCreatedAt = course.CourseCreatedAt,
                    StarRating = (byte)averageRating,
                    CourseCategories = courseCategories,
                    CreatedBy = course.CreatedBy
                });
            }

            // Pass category and course counts
            ViewBag.Categories = categories;
            ViewBag.CategoryCounts = categoryCounts;
            ViewBag.SelectedCategoryId = categoryId;

            if (userRole == "1")
            {
                return View("CourseAcceptance", coursesViewModel);
            }
            else if (userRole == "2")
            {
                return View("CourseManagement", coursesViewModel);
            }

            return View("CourseManagement", coursesViewModel);
        }


        public async Task<ActionResult> ConfirmDelete()
        {
            var courseId = HttpContext.Request.Cookies["course_id"];

            var course = await _courseRepo.GetCourseByIdAsync(courseId);

            if (course == null) return RedirectToAction("ErrorPage", "Home");

            var viewModel = new DeleteCourseViewModel
            {
                CourseId = course.CourseId,
                CourseName = course.CourseName,
                CourseCategories = await _courseRepo.GetCourseCategoriesAsync(),
                CourseCategoryId = (await _courseRepo.GetCourseCategoriesByCourseIdAsync(courseId)).FirstOrDefault()?.CourseCategoryId,
                CourseDescription = course.CourseDescription,
                CoursePictureFile = course.CoursePicture,
                Price = course.Price
            };
            return View("DeleteCourse", viewModel);
        }

        [HttpPost]
        public async Task<ActionResult> DeleteCourse()
        {
            var courseId = HttpContext.Request.Cookies["CourseId"];
            var course = await _courseRepo.GetCourseByIdAsync(courseId);

            if (course != null)
            {
                var deleted = await _courseRepo.DeleteCourseAsync(courseId);
                if (deleted)
                {
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
            }
            return RedirectToAction("ErrorPage", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> CourseDetail(int page = 1, int pageSize = 10)
        {
            var courseId = Request.Cookies["CourseId"];
            var userId = Request.Cookies["user_id"];
            string userRole = Request.Cookies["user_role"];
            bool isLoggedIn = !string.IsNullOrEmpty(userId);

            if (string.IsNullOrEmpty(courseId))
            {
                _logger.LogWarning("Course ID is null or empty.");
                return View("ErrorPage", "Course ID not found in cookies.");
            }

            ViewBag.CreatedBy = await _courseRepo.GetCourseCreatorNameAsync(courseId);
            if (isLoggedIn)
            {
                var (isEnrolled, isBanned) = await _courseRepo.CheckEnrollmentStatusAsync(userId, courseId);
                double progress = isLoggedIn ? await _courseRepo.GetCourseProgressAsync(userId, courseId) : 0;
                ViewBag.Progress = progress;
                ViewBag.IsEnrolled = isEnrolled;
                ViewBag.IsBanned = isBanned;
                ViewBag.IsLoggedIn = isLoggedIn;
            }
            else
            {
                ViewBag.IsLoggedIn = false;
                ViewBag.IsEnrolled = false;
                ViewBag.IsBanned = false;
                ViewBag.Progress = 0;
            }

            var course = await _courseRepo.GetCourseByIdAsync(courseId);
            if (course == null)
            {
                _logger.LogError($"Course not found with ID: {courseId}");
                return View("ErrorPage", "Course not found.");
            }

            ViewBag.CourseCategories = await _courseRepo.GetCourseCategoriesAsync(courseId);
            ViewBag.LearnersCount = await _courseRepo.GetLearnersCountAsync(courseId);

            var chapters = await _chapterRepo.GetChaptersByCourseIdAsync(courseId);
            int totalLessons = 0;
            foreach (var chapter in chapters)
            {
                chapter.Lessons = await _lessonRepo.GetLessonsByChapterIdAsync(chapter.ChapterId);
                totalLessons += chapter.Lessons.Count;
            }
            course.Chapters = chapters;

            ViewBag.TotalLessons = totalLessons;

            int offset = (page - 1) * pageSize;
            ViewBag.Comments = await _courseRepo.GetFeedbacksAsync(courseId, userRole, offset, pageSize);
            ViewBag.TotalComments = await _courseRepo.GetTotalFeedbackCountAsync(courseId);
            ViewBag.AverageRating = await _courseRepo.GetAverageRatingAsync(courseId);
            ViewBag.RatingPercentages = await _courseRepo.GetRatingPercentagesAsync(courseId, ViewBag.TotalComments);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)ViewBag.TotalComments / pageSize);

            return View(course);
        }



        public async Task<ActionResult> CourseAcceptance()
        {
            var pendingCourses = await _courseRepo.GetPendingCoursesAsync();
            var coursesViewModel = new List<ManagementCourseViewModel>();
            var topCategories = await _courseRepo.GetTopCourseCategoriesAsync();

            ViewBag.Categories = topCategories;

            var categoryCounts = new Dictionary<string, int>();
            foreach (var category in topCategories)
            {
                int count = await _courseRepo.GetCourseCountByCategoryAsync(category.CourseCategoryId);
                categoryCounts[category.CourseCategoryId] = count;
            }

            ViewBag.CategoryCounts = categoryCounts;

            int totalCourseCount = await _courseRepo.GetTotalCourseCountAsync();
            int pendingCourseCount = await _courseRepo.GetPendingCourseCountAsync();
            int acceptedCourseCount = await _courseRepo.GetAcceptedCourseCountAsync();
            int rejectedCourseCount = await _courseRepo.GetRejectedCourseCountAsync();
            int notApprovedCourseCount = await _courseRepo.GetNotApprovedCourseCountAsync();

            ViewBag.TotalCourseCount = totalCourseCount;
            ViewBag.PendingCourseCount = pendingCourseCount;
            ViewBag.AcceptedCourseCount = acceptedCourseCount;
            ViewBag.RejectedCourseCount = rejectedCourseCount;
            ViewBag.NotApprovedCourseCount = notApprovedCourseCount;

            foreach (var course in pendingCourses)
            {
                var courseCategories = await _courseRepo.GetCourseCategoriesByCourseIdAsync(course.CourseId);
                string creatorName = await _courseRepo.GetCourseCreatorNameAsync(course.CourseId);

                coursesViewModel.Add(new ManagementCourseViewModel
                {
                    CourseId = course.CourseId,
                    CourseName = course.CourseName,
                    CourseDescription = course.CourseDescription,
                    CourseStatus = course.CourseStatus,
                    CoursePicture = course.CoursePicture,
                    Price = course.Price,
                    CourseCreatedAt = course.CourseCreatedAt,
                    CourseCategories = courseCategories,
                    CreatedBy = creatorName
                });
            }

            return View("CourseAcceptance", coursesViewModel);
        }


        [HttpGet]
        public async Task<IActionResult> ReviewCourse(int page = 1, int pageSize = 4)
        {
            try
            {
                var courseId = Request.Cookies["course_id"];
                var userRole = Request.Cookies["user_role"];
                if (string.IsNullOrEmpty(courseId))
                {
                    _logger.LogWarning("Course ID is null or empty.");
                    return View("ErrorPage", "Course ID not found in cookies.");
                }

                var course = await _courseRepo.GetCourseByIdAsync(courseId);
                if (course == null)
                {
                    _logger.LogError($"Course not found with ID: {courseId}");
                    return View("ErrorPage", "Course not found.");
                }

                ViewBag.LearnersCount = await _courseRepo.GetLearnersCountAsync(courseId);

                int offset = (page - 1) * pageSize;
                ViewBag.Comments = await _courseRepo.GetFeedbacksAsync(courseId, userRole, offset, pageSize);
                ViewBag.TotalComments = await _courseRepo.GetTotalFeedbackCountAsync(courseId);
                ViewBag.AverageRating = await _courseRepo.GetAverageRatingAsync(courseId);
                ViewBag.RatingPercentages = await _courseRepo.GetRatingPercentagesAsync(courseId, ViewBag.TotalComments);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)ViewBag.TotalComments / pageSize);

                ViewBag.CreatedBy = await _courseRepo.GetCourseCreatorNameAsync(courseId);
                ViewBag.CourseCategories = await _courseRepo.GetCourseCategoriesAsync(courseId);

                return View(course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while loading the course.");
                return View("ErrorPage", "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(string courseId, int status)
        {
            var result = await _courseRepo.UpdateCourseStatusAsync(courseId, status);
            if (!result)
            {
                return View("ErrorPage", "Failed to update course status.");
            }
            return RedirectToAction("CourseAcceptance", "Course");
        }

        [HttpPost]
        public async Task<IActionResult> Enroll(string courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _courseRepo.GetUserByIdAsync(userId);
            var course = await _courseRepo.GetCourseByIdAsync(courseId);

            if (user == null || course == null)
            {
                return View("ErrorPage", "User or Course not found.");
            }

            if (user.PaymentPoint >= course.Price)
            {
                // Sử dụng GenerateNewEnrollmentIdAsync từ CourseRepo để tạo EnrollmentId mới
                string newEnrollmentId = await _courseRepo.GenerateNewEnrollmentIdAsync();

                // Ghi danh người dùng vào khóa học
                var enrolled = await _courseRepo.EnrollUserInCourseAsync(newEnrollmentId, userId, courseId, DateTime.Now);

                if (enrolled)
                {
                    // Trừ điểm của người dùng sau khi ghi danh thành công
                    await _courseRepo.UpdateUserPaymentPointsAsync(userId, course.Price);
                    return RedirectToAction("CourseDetail");
                }
            }
            else
            {
                TempData["ErrorMessage"] = "You do not have enough points to enroll in this course.";
            }

            return RedirectToAction("CourseDetail");
        }

        [HttpPost]
        public async Task<IActionResult> RequestToAdmin(string courseId)
        {
            bool hasChaptersAndLessons = await _courseRepo.HasChaptersAndLessonsAsync(courseId);

            if (hasChaptersAndLessons)
            {
                bool updated = await _courseRepo.UpdateCourseStatusAsync(courseId, 1);
                if (updated)
                {
                    return Json(new { success = true, message = "Request sent to Admin successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update course status." });
                }
            }
            else
            {
                return Json(new { success = false, message = "The course must have at least 1 chapter and 1 lesson. Back to edit and add more" });
            }
        }

    }
}
