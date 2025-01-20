using BrainStormEra.Models;
using BrainStormEra.Views.Course;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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

        public async Task<ActionResult> AddCourse()
        {
            try
            {
                var viewModel = new CreateCourseViewModel
                {
                    CourseCategories = await _context.CourseCategories.ToListAsync()
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

            viewModel.CourseId = await GenerateNewCourseIdAsync();

            if (await IsCourseNameExistsAsync(viewModel.CourseName, viewModel.CourseId))
            {
                ModelState.AddModelError("CourseName", "The Course Name already exists. Please enter a different name.");
                viewModel.CourseCategories = await _context.CourseCategories.ToListAsync();
                return View(viewModel);
            }

            if (viewModel.CategoryIds == null || !viewModel.CategoryIds.Any())
            {
                ModelState.AddModelError("CategoryIds", "Please select at least one category.");
                viewModel.CourseCategories = await _context.CourseCategories.ToListAsync();
                return View(viewModel);
            }

            if (viewModel.CategoryIds.Count > 5)
            {
                ModelState.AddModelError("CategoryIds", "You can select up to 5 categories.");
                viewModel.CourseCategories = await _context.CourseCategories.ToListAsync();
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

            var newCourse = new Course
            {
                CourseId = viewModel.CourseId,
                CourseName = viewModel.CourseName,
                CourseDescription = viewModel.CourseDescription,
                CourseStatus = 3,
                CreatedBy = userId,
                CoursePicture = coursePicturePath,
                Price = viewModel.Price
            };

            _context.Courses.Add(newCourse);
            await _context.SaveChangesAsync();

            foreach (var categoryId in viewModel.CategoryIds)
            {
                _context.Set<Dictionary<string, object>>("CourseCategoryMapping").Add(
                    new Dictionary<string, object>
                    {
                        { "CourseId", newCourse.CourseId },
                        { "CourseCategoryId", categoryId }
                    });
            }
            await _context.SaveChangesAsync();

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

            var course = await _context.Courses.FindAsync(courseId);

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

            var allCategories = await _context.CourseCategories.ToListAsync(); // Get all categories
            var selectedCategories = await _context.Set<Dictionary<string, object>>("CourseCategoryMapping")
                .Where(ccm => ccm["CourseId"].ToString() == courseId)
                .Join(_context.CourseCategories,
                      ccm => ccm["CourseCategoryId"].ToString(),
                      cc => cc.CourseCategoryId,
                      (ccm, cc) => cc)
                .ToListAsync(); // Get selected categories for the course

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
            var course = await _context.Courses.FindAsync(viewModel.CourseId);
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
            if (await IsCourseNameExistsAsync(viewModel.CourseName, viewModel.CourseId))
            {
                ModelState.AddModelError("CourseName", "The Course Name already exists. Please enter a different name.");
                viewModel.CourseCategories = await _context.CourseCategories.ToListAsync();
                var selectedCategories = await _context.Set<Dictionary<string, object>>("CourseCategoryMapping")
                    .Where(ccm => ccm["CourseId"].ToString() == courseId)
                    .Join(_context.CourseCategories,
                          ccm => ccm["CourseCategoryId"].ToString(),
                          cc => cc.CourseCategoryId,
                          (ccm, cc) => cc)
                    .ToListAsync(); // Get selected categories for the course
                viewModel.SelectedCategories = selectedCategories;   // Only selected categories
                return View(viewModel);
            }

            // Check if categories are selected
            if (viewModel.CategoryIds == null || !viewModel.CategoryIds.Any())
            {
                ModelState.AddModelError("CategoryIds", "Please select at least one category.");
                viewModel.CourseCategories = await _context.CourseCategories.ToListAsync();
                var selectedCategories = await _context.Set<Dictionary<string, object>>("CourseCategoryMapping")
                    .Where(ccm => ccm["CourseId"].ToString() == courseId)
                    .Join(_context.CourseCategories,
                          ccm => ccm["CourseCategoryId"].ToString(),
                          cc => cc.CourseCategoryId,
                          (ccm, cc) => cc)
                    .ToListAsync(); // Get selected categories for the course
                viewModel.SelectedCategories = selectedCategories;   // Only selected categories
                return View(viewModel);
            }

            if (viewModel.CategoryIds.Count > 5)
            {
                ModelState.AddModelError("CategoryIds", "You can select up to 5 categories.");
                viewModel.CourseCategories = await _context.CourseCategories.ToListAsync();
                var selectedCategories = await _context.Set<Dictionary<string, object>>("CourseCategoryMapping")
                    .Where(ccm => ccm["CourseId"].ToString() == courseId)
                    .Join(_context.CourseCategories,
                          ccm => ccm["CourseCategoryId"].ToString(),
                          cc => cc.CourseCategoryId,
                          (ccm, cc) => cc)
                    .ToListAsync(); // Get selected categories for the course
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
                    viewModel.CourseCategories = await _context.CourseCategories.ToListAsync();
                    var selectedCategories = await _context.Set<Dictionary<string, object>>("CourseCategoryMapping")
                        .Where(ccm => ccm["CourseId"].ToString() == courseId)
                        .Join(_context.CourseCategories,
                              ccm => ccm["CourseCategoryId"].ToString(),
                              cc => cc.CourseCategoryId,
                              (ccm, cc) => cc)
                        .ToListAsync(); // Get selected categories for the course
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
            course.CourseName = viewModel.CourseName;
            course.CourseDescription = viewModel.CourseDescription;
            course.Price = viewModel.Price;
            course.CoursePicture = coursePicturePath;

            await _context.SaveChangesAsync();

            // Update course categories
            var existingCategories = await _context.Set<Dictionary<string, object>>("CourseCategoryMapping")
                .Where(ccm => ccm["CourseId"].ToString() == courseId)
                .ToListAsync();
            _context.Set<Dictionary<string, object>>("CourseCategoryMapping").RemoveRange(existingCategories);

            foreach (var categoryId in viewModel.CategoryIds)
            {
                _context.Set<Dictionary<string, object>>("CourseCategoryMapping").Add(
                    new Dictionary<string, object>
                    {
                        { "CourseId", courseId },
                        { "CourseCategoryId", categoryId }
                    });
            }
            await _context.SaveChangesAsync();

            return RedirectToAction("CourseManagement");
        }

        public async Task<ActionResult> CourseManagement()
        {
            var userId = HttpContext.Request.Cookies["user_id"];
            var userRole = HttpContext.Request.Cookies["user_role"];

            List<ManagementCourseViewModel> coursesViewModel = new List<ManagementCourseViewModel>();
            var categories = await _context.CourseCategories.ToListAsync();
            var categoryCounts = new Dictionary<string, int>();

            foreach (var category in categories)
            {
                int courseCount = await _context.Set<Dictionary<string, object>>("CourseCategoryMapping")
                    .Where(ccm => ccm["CourseCategoryId"].ToString() == category.CourseCategoryId)
                    .Join(_context.Courses,
                          ccm => ccm["CourseId"].ToString(),
                          c => c.CourseId,
                          (ccm, c) => c)
                    .CountAsync();
                categoryCounts[category.CourseCategoryId] = courseCount;
            }

            ViewBag.Categories = categories;
            ViewBag.CategoryCounts = categoryCounts;

            if (userRole == "2")
            {
                // Lấy danh sách khóa học của giảng viên
                var courses = await _context.Courses
                    .Where(c => c.CreatedBy == userId)
                    .ToListAsync();
                foreach (var course in courses)
                {
                    // Lấy rating trung bình
                    double averageRating = await _context.Feedbacks
                        .Where(f => f.CourseId == course.CourseId && f.HiddenStatus == false)
                        .AverageAsync(f => f.StarRating) ?? 0;

                    // Lấy danh sách các category
                    var courseCategories = await _context.Set<Dictionary<string, object>>("CourseCategoryMapping")
                        .Where(ccm => ccm["CourseId"].ToString() == course.CourseId)
                        .Join(_context.CourseCategories,
                              ccm => ccm["CourseCategoryId"].ToString(),
                              cc => cc.CourseCategoryId,
                              (ccm, cc) => cc)
                        .ToListAsync();

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
                var courses = await _context.Courses
                    .Where(c => c.CourseStatus == 2)
                    .OrderByDescending(c => c.CourseCreatedAt)
                    .ToListAsync();
                foreach (var course in courses)
                {
                    // Lấy rating trung bình
                    double averageRating = await _context.Feedbacks
                        .Where(f => f.CourseId == course.CourseId && f.HiddenStatus == false)
                        .AverageAsync(f => f.StarRating) ?? 0;

                    // Lấy danh sách các category
                    var courseCategories = await _context.Set<Dictionary<string, object>>("CourseCategoryMapping")
                        .Where(ccm => ccm["CourseId"].ToString() == course.CourseId)
                        .Join(_context.CourseCategories,
                              ccm => ccm["CourseCategoryId"].ToString(),
                              cc => cc.CourseCategoryId,
                              (ccm, cc) => cc)
                        .ToListAsync();
                    string creatorName = await _context.Accounts
                        .Where(a => a.UserId == course.CreatedBy)
                        .Select(a => a.FullName)
                        .FirstOrDefaultAsync();
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
            var categories = await _context.CourseCategories.ToListAsync();

            // Prepare a dictionary to store course counts per category
            var categoryCounts = new Dictionary<string, int>();
            foreach (var category in categories)
            {
                int count = await _context.Set<Dictionary<string, object>>("CourseCategoryMapping")
                    .Where(ccm => ccm["CourseCategoryId"].ToString() == category.CourseCategoryId)
                    .Join(_context.Courses,
                          ccm => ccm["CourseId"].ToString(),
                          c => c.CourseId,
                          (ccm, c) => c)
                    .CountAsync();
                categoryCounts[category.CourseCategoryId] = count;
            }

            int totalCourseCount = await _context.Courses.CountAsync();
            int pendingCourseCount = await _context.Courses.CountAsync(c => c.CourseStatus == 1);
            int acceptedCourseCount = await _context.Courses.CountAsync(c => c.CourseStatus == 2);
            int rejectedCourseCount = await _context.Courses.CountAsync(c => c.CourseStatus == 0);
            int notApprovedCourseCount = await _context.Courses.CountAsync(c => c.CourseStatus != 0 && c.CourseStatus != 1 && c.CourseStatus != 2);

            ViewBag.TotalCourseCount = totalCourseCount;
            ViewBag.PendingCourseCount = pendingCourseCount;
            ViewBag.AcceptedCourseCount = acceptedCourseCount;
            ViewBag.RejectedCourseCount = rejectedCourseCount;
            ViewBag.NotApprovedCourseCount = notApprovedCourseCount;

            // Fetch courses filtered by category
            List<Course> courses;
            if (userRole == "2")
            {
                courses = await _context.Set<Dictionary<string, object>>("CourseCategoryMapping")
                    .Where(ccm => ccm["CourseCategoryId"].ToString() == categoryId && ccm["CourseId"].ToString() == userId)
                    .Join(_context.Courses,
                          ccm => ccm["CourseId"].ToString(),
                          c => c.CourseId,
                          (ccm, c) => c)
                    .ToListAsync();
            }
            else
            {
                courses = await _context.Set<Dictionary<string, object>>("CourseCategoryMapping")
                    .Where(ccm => ccm["CourseCategoryId"].ToString() == categoryId)
                    .Join(_context.Courses,
                          ccm => ccm["CourseId"].ToString(),
                          c => c.CourseId,
                          (ccm, c) => c)
                    .Where(c => c.CourseStatus == 2)
                    .ToListAsync();
            }

            foreach (var course in courses)
            {
                double averageRating = await _context.Feedbacks
                    .Where(f => f.CourseId == course.CourseId && f.HiddenStatus == false)
                    .AverageAsync(f => f.StarRating) ?? 0;
                var courseCategories = await _context.Set<Dictionary<string, object>>("CourseCategoryMapping")
                    .Where(ccm => ccm["CourseId"].ToString() == course.CourseId)
                    .Join(_context.CourseCategories,
                          ccm => ccm["CourseCategoryId"].ToString(),
                          cc => cc.CourseCategoryId,
                          (ccm, cc) => cc)
                    .ToListAsync();

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

            var course = await _context.Courses.FindAsync(courseId);

            if (course == null) return RedirectToAction("ErrorPage", "Home");

            var viewModel = new DeleteCourseViewModel
            {
                CourseId = course.CourseId,
                CourseName = course.CourseName,
                CourseCategories = await _context.CourseCategories.ToListAsync(),
                CourseCategoryId = (await _context.Set<Dictionary<string, object>>("CourseCategoryMapping")
                    .Where(ccm => ccm["CourseId"].ToString() == courseId)
                    .Join(_context.CourseCategories,
                          ccm => ccm["CourseCategoryId"].ToString(),
                          cc => cc.CourseCategoryId,
                          (ccm, cc) => cc)
                    .FirstOrDefaultAsync())?.CourseCategoryId,
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
            var course = await _context.Courses.FindAsync(courseId);

            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();

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

            ViewBag.CreatedBy = await _context.Accounts
                .Where(a => a.UserId == courseId)
                .Select(a => a.FullName)
                .FirstOrDefaultAsync();
            if (isLoggedIn)
            {
                var (isEnrolled, isBanned) = await CheckEnrollmentStatusAsync(userId, courseId);
                double progress = isLoggedIn ? await GetCourseProgressAsync(userId, courseId) : 0;
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

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                _logger.LogError($"Course not found with ID: {courseId}");
                return View("ErrorPage", "Course not found.");
            }

            ViewBag.CourseCategories = await _context.Set<Dictionary<string, object>>("CourseCategoryMapping")
                .Where(ccm => ccm["CourseId"].ToString() == courseId)
                .Join(_context.CourseCategories,
                      ccm => ccm["CourseCategoryId"].ToString(),
                      cc => cc.CourseCategoryId,
                      (ccm, cc) => cc)
                .ToListAsync();
            ViewBag.LearnersCount = await _context.Enrollments
                .Where(e => e.CourseId == courseId && e.Approved == true)
                .CountAsync();

            var chapters = await _context.Chapters
                .Where(c => c.CourseId == courseId)
                .Include(c => c.Lessons)
                .ToListAsync();
            int totalLessons = chapters.Sum(c => c.Lessons.Count);
            course.Chapters = chapters;

            ViewBag.TotalLessons = totalLessons;

            int offset = (page - 1) * pageSize;
            ViewBag.Comments = await _context.Feedbacks
                .Where(f => f.CourseId == courseId && (userRole != "3" || f.HiddenStatus == false))
                .OrderByDescending(f => f.FeedbackCreatedAt)
                .Skip(offset)
                .Take(pageSize)
                .Select(f => new Feedback
                {
                    FeedbackId = f.FeedbackId,
                    CourseId = f.CourseId,
                    UserId = f.UserId,
                    StarRating = f.StarRating,
                    Comment = f.Comment,
                    FeedbackDate = f.FeedbackDate,
                    User = new Account
                    {
                        FullName = f.User.FullName,
                        UserPicture = f.User.UserPicture
                    },
                    HiddenStatus = f.HiddenStatus
                })
                .ToListAsync();
            ViewBag.TotalComments = await _context.Feedbacks
                .Where(f => f.CourseId == courseId && f.HiddenStatus == false)
                .CountAsync();
            ViewBag.AverageRating = await _context.Feedbacks
                .Where(f => f.CourseId == courseId && f.HiddenStatus == false)
                .AverageAsync(f => f.StarRating) ?? 0;
            ViewBag.RatingPercentages = await GetRatingPercentagesAsync(courseId, ViewBag.TotalComments);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)ViewBag.TotalComments / pageSize);

            return View(course);
        }

        public async Task<ActionResult> CourseAcceptance()
        {
            var pendingCourses = await _context.Courses
                .Where(c => c.CourseStatus == 0 || c.CourseStatus == 1 || c.CourseStatus == 2)
                .OrderBy(c => c.CourseStatus == 1 ? 0 : 1)
                .ThenBy(c => c.CourseCreatedAt)
                .ToListAsync();
            var coursesViewModel = new List<ManagementCourseViewModel>();
            var topCategories = await _context.CourseCategories.ToListAsync();

            ViewBag.Categories = topCategories;

            var categoryCounts = new Dictionary<string, int>();
            foreach (var category in topCategories)
            {
                int count = await _context.Set<Dictionary<string, object>>("CourseCategoryMapping")
                    .Where(ccm => ccm["CourseCategoryId"].ToString() == category.CourseCategoryId)
                    .Join(_context.Courses,
                          ccm => ccm["CourseId"].ToString(),
                          c => c.CourseId,
                          (ccm, c) => c)
                    .CountAsync();
                categoryCounts[category.CourseCategoryId] = count;
            }

            ViewBag.CategoryCounts = categoryCounts;

            int totalCourseCount = await _context.Courses.CountAsync();
            int pendingCourseCount = await _context.Courses.CountAsync(c => c.CourseStatus == 1);
            int acceptedCourseCount = await _context.Courses.CountAsync(c => c.CourseStatus == 2);
            int rejectedCourseCount = await _context.Courses.CountAsync(c => c.CourseStatus == 0);
            int notApprovedCourseCount = await _context.Courses.CountAsync(c => c.CourseStatus != 0 && c.CourseStatus != 1 && c.CourseStatus != 2);

            ViewBag.TotalCourseCount = totalCourseCount;
            ViewBag.PendingCourseCount = pendingCourseCount;
            ViewBag.AcceptedCourseCount = acceptedCourseCount;
            ViewBag.RejectedCourseCount = rejectedCourseCount;
            ViewBag.NotApprovedCourseCount = notApprovedCourseCount;

            foreach (var course in pendingCourses)
            {
                var courseCategories = await _context.Set<Dictionary<string, object>>("CourseCategoryMapping")
                    .Where(ccm => ccm["CourseId"].ToString() == course.CourseId)
                    .Join(_context.CourseCategories,
                          ccm => ccm["CourseCategoryId"].ToString(),
                          cc => cc.CourseCategoryId,
                          (ccm, cc) => cc)
                    .ToListAsync();
                string creatorName = await _context.Accounts
                    .Where(a => a.UserId == course.CreatedBy)
                    .Select(a => a.FullName)
                    .FirstOrDefaultAsync();

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

                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                {
                    _logger.LogError($"Course not found with ID: {courseId}");
                    return View("ErrorPage", "Course not found.");
                }

                ViewBag.LearnersCount = await _context.Enrollments
                    .Where(e => e.CourseId == courseId && e.Approved == true)
                    .CountAsync();

                int offset = (page - 1) * pageSize;
                ViewBag.Comments = await _context.Feedbacks
                    .Where(f => f.CourseId == courseId && (userRole != "3" || f.HiddenStatus == false))
                    .OrderByDescending(f => f.FeedbackCreatedAt)
                    .Skip(offset)
                    .Take(pageSize)
                    .Select(f => new Feedback
                    {
                        FeedbackId = f.FeedbackId,
                        CourseId = f.CourseId,
                        UserId = f.UserId,
                        StarRating = f.StarRating,
                        Comment = f.Comment,
                        FeedbackDate = f.FeedbackDate,
                        User = new Account
                        {
                            FullName = f.User.FullName,
                            UserPicture = f.User.UserPicture
                        },
                        HiddenStatus = f.HiddenStatus
                    })
                    .ToListAsync();
                ViewBag.TotalComments = await _context.Feedbacks
                    .Where(f => f.CourseId == courseId && f.HiddenStatus == false)
                    .CountAsync();
                ViewBag.AverageRating = await _context.Feedbacks
                    .Where(f => f.CourseId == courseId && f.HiddenStatus == false)
                    .AverageAsync(f => f.StarRating) ?? 0;
                ViewBag.RatingPercentages = await GetRatingPercentagesAsync(courseId, ViewBag.TotalComments);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)ViewBag.TotalComments / pageSize);

                ViewBag.CreatedBy = await _context.Accounts
                    .Where(a => a.UserId == courseId)
                    .Select(a => a.FullName)
                    .FirstOrDefaultAsync();
                ViewBag.CourseCategories = await _context.Set<Dictionary<string, object>>("CourseCategoryMapping")
                    .Where(ccm => ccm["CourseId"].ToString() == courseId)
                    .Join(_context.CourseCategories,
                          ccm => ccm["CourseCategoryId"].ToString(),
                          cc => cc.CourseCategoryId,
                          (ccm, cc) => cc)
                    .ToListAsync();

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
            var course = await _context.Courses.FindAsync(courseId);
            if (course != null)
            {
                course.CourseStatus = status;
                await _context.SaveChangesAsync();
                return RedirectToAction("CourseAcceptance", "Course");
            }
            return View("ErrorPage", "Failed to update course status.");
        }

        [HttpPost]
        public async Task<IActionResult> Enroll(string courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Accounts.FindAsync(userId);
            var course = await _context.Courses.FindAsync(courseId);

            if (user == null || course == null)
            {
                return View("ErrorPage", "User or Course not found.");
            }

            if (user.PaymentPoint >= course.Price)
            {
                // Sử dụng GenerateNewEnrollmentIdAsync từ CourseRepo để tạo EnrollmentId mới
                string newEnrollmentId = await GenerateNewEnrollmentIdAsync();

                // Ghi danh người dùng vào khóa học
                var enrollment = new Enrollment
                {
                    EnrollmentId = newEnrollmentId,
                    UserId = userId,
                    CourseId = courseId,
                    EnrollmentStatus = 1,
                    Approved = true,
                    EnrollmentCreatedAt = DateTime.Now
                };
                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                // Trừ điểm của người dùng sau khi ghi danh thành công
                user.PaymentPoint -= course.Price;
                await _context.SaveChangesAsync();

                return RedirectToAction("CourseDetail");
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
            bool hasChaptersAndLessons = await _context.Chapters
                .Where(c => c.CourseId == courseId)
                .AnyAsync(c => c.Lessons.Any());

            if (hasChaptersAndLessons)
            {
                var course = await _context.Courses.FindAsync(courseId);
                if (course != null)
                {
                    course.CourseStatus = 1;
                    await _context.SaveChangesAsync();
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

        private async Task<string> GenerateNewCourseIdAsync()
        {
            var lastCourseId = await _context.Courses
                .OrderByDescending(c => c.CourseId)
                .Select(c => c.CourseId)
                .FirstOrDefaultAsync();
            return lastCourseId == null ? "CO001" : "CO" + (int.Parse(lastCourseId.Substring(2)) + 1).ToString("D3");
        }

        private async Task<bool> IsCourseNameExistsAsync(string courseName, string courseId)
        {
            return await _context.Courses
                .AnyAsync(c => c.CourseName == courseName && c.CourseId != courseId);
        }

        private async Task<(bool IsEnrolled, bool IsBanned)> CheckEnrollmentStatusAsync(string userId, string courseId)
        {
            var enrollment = await _context.Enrollments
                .Where(e => e.UserId == userId && e.CourseId == courseId)
                .FirstOrDefaultAsync();
            if (enrollment != null)
            {
                return (true, !enrollment.Approved);
            }
            return (false, false);
        }

        private async Task<double> GetCourseProgressAsync(string userId, string courseId)
        {
            var completedLessons = await _context.LessonCompletions
                .Where(lc => lc.UserId == userId && lc.Lesson.Chapter.CourseId == courseId)
                .CountAsync();
            var totalLessons = await _context.Lessons
                .Where(l => l.Chapter.CourseId == courseId)
                .CountAsync();
            return totalLessons > 0 ? (double)completedLessons / totalLessons * 100 : 0;
        }

        private async Task<Dictionary<int, double>> GetRatingPercentagesAsync(string courseId, int totalComments)
        {
            var ratingPercentages = new Dictionary<int, double>();
            for (int i = 1; i <= 5; i++)
            {
                int count = await _context.Feedbacks
                    .Where(f => f.CourseId == courseId && f.StarRating == i && f.HiddenStatus == false)
                    .CountAsync();
                ratingPercentages[i] = totalComments > 0 ? (double)count / totalComments : 0;
            }
            return ratingPercentages;
        }

        private async Task<string> GenerateNewEnrollmentIdAsync()
        {
            var maxEnrollmentId = await _context.Enrollments
                .OrderByDescending(e => e.EnrollmentId)
                .Select(e => e.EnrollmentId)
                .FirstOrDefaultAsync();

            int newIdNumber = 1;
            if (!string.IsNullOrEmpty(maxEnrollmentId) && maxEnrollmentId.Length > 2)
            {
                newIdNumber = int.Parse(maxEnrollmentId.Substring(2)) + 1;
            }

            return "EN" + newIdNumber.ToString("D3");
        }
    }
}
