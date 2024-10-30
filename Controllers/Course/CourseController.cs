using BrainStormEra.Models;
using BrainStormEra.Views.Course;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium.BiDi.Modules.Script;
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
                CourseCategories = _context.CourseCategories.ToList()
            };
            return View(viewModel);
        }


        //CREATE 
        [HttpPost]
        public ActionResult AddCourse(CreateCourseViewModel viewmodel)
        {
            var userId = Request.Cookies["user_id"];
            var lastCourse = _context.Courses.OrderByDescending(c => c.CourseId).FirstOrDefault();
            var newCourseId = lastCourse == null ? "CO001" : "CO" + (int.Parse(lastCourse.CourseId.Substring(2)) + 1).ToString("D3");
            viewmodel.CourseId = newCourseId;

            if (!ModelState.IsValid)
            {
                // CheckDuplicate() namecourse
                var existingCourse = _context.Courses.FirstOrDefault(c => c.CourseName == viewmodel.CourseName && c.CourseId != viewmodel.CourseId);
                if (existingCourse != null)
                {
                    ModelState.AddModelError("CourseName", "The Course Name already exists. Please enter a different name.");
                    viewmodel.CourseCategories = _context.CourseCategories.ToList();
                    return View(viewmodel);
                }


                // check caterogy
                if (viewmodel.CategoryIds == null || !viewmodel.CategoryIds.Any())
                {
                    ModelState.AddModelError("CategoryIds", "Please select at least one category.");
                    viewmodel.CourseCategories = _context.CourseCategories.ToList();
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



                // Xử lý upload ảnh cho CoursePicture
                if (viewmodel.CoursePicture != null && viewmodel.CoursePicture.Length > 0)
                {
                    // Kiểm tra kích thước file (giới hạn 2MB)
                    if (viewmodel.CoursePicture.Length > 2 * 1024 * 1024)
                    {
                        ModelState.AddModelError("CoursePicture", "Kích thước tệp không được vượt quá 2MB.");
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

                    // Cập nhật đường dẫn ảnh vào database
                    obj.CoursePicture = $"/uploads/Course-img/{fileName}";
                }


                // Thêm nhiều category vào khóa học
                foreach (var categoryId in viewmodel.CategoryIds)
                {
                    var category = _context.CourseCategories.Find(categoryId);
                    if (category != null)
                    {
                        obj.CourseCategories.Add(category);  // Sử dụng `obj` thay vì `course`
                    }
                }


                _context.Courses.Add(obj);
                _context.SaveChanges();
                viewmodel.CourseCategories = _context.CourseCategories.ToList();
                return RedirectToAction("CourseManagement");
            }
            return RedirectToAction("CourseManagement");

        }

        // EditCourse
        [HttpGet]
        public ActionResult EditCourse()
        {
            var courseId = HttpContext.Request.Cookies["CourseId"];

            var course = _context.Courses
                    .Include(c => c.CourseCategories)
                    .FirstOrDefault(c => c.CourseId == courseId);

            HttpContext.Response.Cookies.Append("CourseName", course.CourseName);

            if (course == null)
            {
                return RedirectToAction("CourseManagement");
            }
            var viewModel = new EditCourseViewModel
            {
                CourseId = course.CourseId,
                CourseName = course.CourseName,
                CourseCategories = _context.CourseCategories.ToList(),
                SelectedCategories = course.CourseCategories.ToList(), // Danh mục đã chọn cho khóa học
                CourseDescription = course.CourseDescription,
                CoursePictureFile = course.CoursePicture,
                Price = course.Price
            };
            return View(viewModel);
        }
        //

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCourse(EditCourseViewModel viewmodel)
        {
            var course = _context.Courses
                 .Include(c => c.CourseCategories)
                 .FirstOrDefault(c => c.CourseId == viewmodel.CourseId);

            if (course == null)
            {
                return RedirectToAction("CourseManagement");
            }
            var existingCourse = _context.Courses.FirstOrDefault(c => c.CourseName == viewmodel.CourseName);
            if (existingCourse != null)
            {
                ModelState.AddModelError("CourseName", "The Course Name already exists. Please enter a different name.");
                viewmodel.CourseCategories = _context.CourseCategories.ToList();
            }


            // check caterogy
            if (viewmodel.CategoryIds == null || !viewmodel.CategoryIds.Any())
            {
                ModelState.AddModelError("CategoryIds", "Please select at least one category.");
                viewmodel.CourseCategories = _context.CourseCategories.ToList();
                return View(viewmodel);
            }

            course.CourseName = viewmodel.CourseName;
            course.CourseDescription = viewmodel.CourseDescription;
            course.Price = viewmodel.Price;


            // Kiểm tra xem người dùng có chọn thay đổi ảnh không
            if (viewmodel.CoursePicture != null && viewmodel.CoursePicture.Length > 0)
            {
                // Người dùng chọn ảnh mới, xử lý upload ảnh
                if (viewmodel.CoursePicture.Length > 2 * 1024 * 1024) // Giới hạn 2MB
                {
                    ModelState.AddModelError("CoursePicture", "Kích thước tệp không được vượt quá 2MB.");
                    viewmodel.CourseCategories = _context.CourseCategories.ToList();
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

                // Cập nhật đường dẫn ảnh vào database
                course.CoursePicture = $"/uploads/Course-img/{fileName}";
            }
            else
            {
                // Nếu người dùng không chọn ảnh mới, giữ lại ảnh hiện tại
                course.CoursePicture = course.CoursePicture;
            }


            // Xóa các category không còn chọn trong bảng course_category_mapping
            var selectedCategoryIds = viewmodel.CategoryIds ?? new List<string>();
            var existingCategoryIds = course.CourseCategories.Select(c => c.CourseCategoryId).ToList();

            // Xóa category không còn trong danh sách chọn
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

            // Thêm mới category chưa có
            foreach (var categoryId in selectedCategoryIds)
            {
                if (!existingCategoryIds.Contains(categoryId))
                {
                    var categoryToAdd = _context.CourseCategories.FirstOrDefault(c => c.CourseCategoryId == categoryId);
                    if (categoryToAdd != null)
                    {
                        course.CourseCategories.Add(categoryToAdd);
                    }
                }
            }


            _context.SaveChanges();
            return RedirectToAction("CourseManagement");


            //viewmodel.CourseCategories = _context.CourseCategories.ToList();
            //return View(viewmodel);
        }

        // GET: ManagementCourse
        public ActionResult CourseManagement()
        {
            var userId = Request.Cookies["user_id"];
            var user_role = Request.Cookies["user_role"];


            switch (user_role)
            {
                // Role Instructor
                case "2":
                    // Lấy thông tin người tạo khóa học

                    var instructorCourses = _context.Courses
                        .Include(c => c.CourseCategories)
                        .Include(c => c.Enrollments)
                        .Include(c => c.CreatedByNavigation) // Include thông tin người tạo từ bảng Account
                        .Where(c => c.CreatedBy == userId)  // Lọc khóa học có status là 2 và được tạo bởi người dùng hiện tại
                        .OrderBy(c => c.CourseStatus == 2 ? 0 :
                  c.CourseStatus == 1 ? 1 :
                  c.CourseStatus == 3 ? 2 : 3) // Sắp xếp theo thứ tự ưu tiên 2, 1, 3, 0
                        .ThenByDescending(c => c.CourseCreatedAt) // Tiếp tục sắp xếp giảm dần theo thời gian tạo
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
                                _context.Feedbacks
                                .Where(f => f.CourseId == course.CourseId)
                                .Average(f => (double?)f.StarRating) ?? 0)
                        })
                        .ToList();



                    return View("CourseManagement", instructorCourses);

                // Default case: Khi không khớp vai trò nào
                default:
                    var Course = _context.Courses
                        .Include(c => c.CourseCategories)
                        .Include(c => c.Enrollments)
                        .Where(c => c.CourseStatus == 2)  // Lọc khóa học có status là 2
                        .OrderByDescending(c => c.CourseCreatedAt)
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
                                _context.Feedbacks
                                .Where(f => f.CourseId == course.CourseId)
                                .Average(f => (double?)f.StarRating) ?? 0)
                        })
                        .ToList();

                    return View("CourseManagement", Course);
            }
        }


        //DELETE
        public ActionResult ConfirmDelete()
        {
            var courseId = HttpContext.Request.Cookies["CourseId"];

            var course = _context.Courses
                   .Include(c => c.CourseCategories)
                   .FirstOrDefault(c => c.CourseId == courseId);
            var viewModel = new DeleteCourseViewModel
            {
                CourseId = course.CourseId,
                CourseName = course.CourseName,
                CourseCategories = _context.CourseCategories.ToList(),
                CourseCategoryId = course.CourseCategories.FirstOrDefault()?.CourseCategoryId, // Chọn Category hiện tại nếu có
                CourseDescription = course.CourseDescription,
                CoursePictureFile = course.CoursePicture,
                Price = course.Price
            };
            return View("DeleteCourse", viewModel);
        }



        [HttpPost]
        public ActionResult DeleteCourse()
        {

            var courseID = HttpContext.Request.Cookies["CourseId"];


            var course = _context.Courses.Find(courseID);

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
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Lấy UserId từ Claims
                var userRole = Request.Cookies["user_role"]; // Lấy user_role từ Cookie

                if (string.IsNullOrEmpty(courseId))
                {
                    _logger.LogWarning("Course ID is null or empty.");
                    return View("ErrorPage", "Course ID not found in cookies.");
                }

                var enrollment = _context.Enrollments.FirstOrDefault(e => e.UserId == userId && e.CourseId == courseId);

                ViewBag.IsEnrolled = enrollment != null;
                ViewBag.IsBanned = enrollment != null && !enrollment.Approved.GetValueOrDefault();
                ViewBag.IsBanned ??= false;

                // Lấy thông tin khóa học cùng với các danh mục (categories)
                var course = _context.Courses
                              .Include(c => c.Chapters)
                              .ThenInclude(ch => ch.Lessons)
                              .Include(c => c.CourseCategories) // Lấy danh sách category của khóa học
                              .FirstOrDefault(c => c.CourseId == courseId);

                if (course == null)
                {
                    _logger.LogError($"Course not found with ID: {courseId}");
                    return View("ErrorPage", "Course not found.");
                }

                // Lấy tên của các category cho khóa học này
                var categories = course.CourseCategories.Select(cc => cc.CourseCategoryName).ToList();
                ViewBag.CourseCategories = categories;

                // Kiểm tra nếu người dùng là learner và đã đăng ký khóa học
                bool isEnrolled = false;
                if (userRole == "3" && !string.IsNullOrEmpty(userId))
                {
                    isEnrolled = _context.Enrollments.Any(e => e.UserId == userId && e.CourseId == courseId);
                }

                ViewBag.IsEnrolled = isEnrolled;

                // Tính toán số lượng học viên đã đăng ký (enrollments)
                var learnersCount = _context.Enrollments
                                    .Where(e => e.CourseId == courseId && e.Approved == true)
                                    .Count();
                ViewBag.LearnersCount = learnersCount;

                // Lấy danh sách feedback (comment và rating) theo phân trang
                var feedbacks = _context.Feedbacks
                                .Where(f => f.CourseId == courseId && f.HiddenStatus == false)
                                .Include(f => f.User)
                                .OrderByDescending(f => f.FeedbackDate)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

                // Tính tổng số comment và rating trung bình
                var totalComments = _context.Feedbacks.Count(f => f.CourseId == courseId && f.HiddenStatus == false);
                var averageRating = totalComments > 0 ? _context.Feedbacks
                                                        .Where(f => f.CourseId == courseId && f.HiddenStatus == false)
                                                        .Average(f => f.StarRating) : 0;

                ViewBag.TotalComments = totalComments;
                ViewBag.AverageRating = averageRating;
                ViewBag.Comments = feedbacks;

                // Phân trang
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalComments / pageSize);

                // Lấy thông tin người tạo khóa học
                var createdBy = _context.Accounts.FirstOrDefault(a => a.UserId == course.CreatedBy);
                ViewBag.CreatedBy = createdBy?.FullName ?? "Unknown";

                // Tính phần trăm mỗi mức rating (1-5 sao)
                var ratingPercentages = new Dictionary<int, double>();
                for (int i = 1; i <= 5; i++)
                {
                    var count = feedbacks.Count(f => f.StarRating == i);
                    ratingPercentages[i] = feedbacks.Count > 0 ? (double)count / feedbacks.Count() : 0;
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


        public ActionResult CourseAcceptance()
        {
            var pendingCourses = _context.Courses
                .Where(c => c.CourseStatus == 0 || c.CourseStatus == 1 || c.CourseStatus == 2)
                .Include(c => c.CourseCategories)
                .Include(c => c.CreatedByNavigation) // Include bảng liên quan để lấy tên người tạo
                .OrderByDescending(c => c.CourseStatus == 1)
                .ThenBy(c => c.CourseCreatedAt)
                .ToList();

            return View("CourseAcceptance", pendingCourses);
        }

        [HttpGet]
        public IActionResult ReviewCourse(int page = 1, int pageSize = 4)
        {
            try
            {
                var courseId = Request.Cookies["CourseId"];

                if (string.IsNullOrEmpty(courseId))
                {
                    _logger.LogWarning("Course ID is null or empty.");
                    return View("ErrorPage", "Course ID not found in cookies.");
                }

                // Lấy thông tin khóa học
                var course = _context.Courses
                                     .Include(c => c.Chapters)
                                     .ThenInclude(ch => ch.Lessons)
                                     .FirstOrDefault(c => c.CourseId == courseId);

                if (course == null)
                {
                    _logger.LogError($"Course not found with ID: {courseId}");
                    return View("ErrorPage", "Course not found.");
                }

                // Tính toán số lượng học viên đã đăng ký (enrollments)
                var learnersCount = _context.Enrollments
                                            .Where(e => e.CourseId == courseId && e.Approved == true)
                                            .Count();
                ViewBag.LearnersCount = learnersCount;

                // Lấy danh sách feedback (comment và rating) theo phân trang
                var feedbacks = _context.Feedbacks
                                        .Where(f => f.CourseId == courseId && f.HiddenStatus == false)
                                        .Include(f => f.User)
                                        .OrderByDescending(f => f.FeedbackDate) // Sắp xếp feedback mới nhất lên trên
                                        .Skip((page - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToList();

                // Tính tổng số comment và rating trung bình
                var totalComments = _context.Feedbacks.Count(f => f.CourseId == courseId && f.HiddenStatus == false);
                var averageRating = totalComments > 0 ? _context.Feedbacks
                                                        .Where(f => f.CourseId == courseId && f.HiddenStatus == false)
                                                        .Average(f => f.StarRating) : 0;

                ViewBag.TotalComments = totalComments;
                ViewBag.AverageRating = averageRating;
                ViewBag.Comments = feedbacks;

                // Phân trang
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalComments / pageSize);

                // Lấy thông tin người tạo khóa học
                var createdBy = _context.Accounts.FirstOrDefault(a => a.UserId == course.CreatedBy);
                if (createdBy != null)
                {
                    ViewBag.CreatedBy = createdBy.FullName;
                }
                else
                {
                    ViewBag.CreatedBy = "Unknown";
                }

                // Truyền CourseStatus vào ViewBag
                ViewBag.CourseStatus = course.CourseStatus;

                // Tính phần trăm mỗi mức rating (1-5 sao)
                var ratingPercentages = new Dictionary<int, double>();

                for (int i = 1; i <= 5; i++)
                {
                    var count = feedbacks.Count(f => f.StarRating == i);
                    ratingPercentages[i] = feedbacks.Count > 0 ? (double)count / feedbacks.Count() : 0;
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
            var course = _context.Courses.FirstOrDefault(c => c.CourseId == courseId);
            if (course != null)
            {
                course.CourseStatus = status;
                _context.SaveChanges();
                // Redirect về trang trước hoặc trang danh sách mà không có ID trên URL
                return RedirectToAction("CourseAcceptance", "Course");
            }
            return View("Error");
        }


        ///

        [HttpPost]
        public IActionResult Enroll(string courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account"); // Chuyển hướng đến trang login nếu chưa đăng nhập
            }

            // Lấy thông tin người dùng và khóa học
            var user = _context.Accounts.FirstOrDefault(u => u.UserId == userId);
            var course = _context.Courses.FirstOrDefault(c => c.CourseId == courseId);

            if (user == null || course == null)
            {
                return View("ErrorPage", "User or Course not found.");
            }

            // Kiểm tra nếu điểm của người dùng đủ để đăng ký khóa học
            if (user.PaymentPoint >= course.Price)
            {
                // Tạo EnrollmentId mới
                var maxEnrollmentId = _context.Enrollments
                                      .OrderByDescending(e => e.EnrollmentId)
                                      .Select(e => e.EnrollmentId)
                                      .FirstOrDefault();

                int newIdNumber = 1; // Mặc định là 1 nếu không có bản ghi nào

                if (!string.IsNullOrEmpty(maxEnrollmentId) && maxEnrollmentId.Length > 2)
                {
                    // Tách phần số và tăng lên 1
                    newIdNumber = int.Parse(maxEnrollmentId.Substring(2)) + 1;
                }

                string newEnrollmentId = "EN" + newIdNumber.ToString("D3"); // Định dạng ID với 3 chữ số

                // Tạo bản ghi mới trong bảng enrollment với status và approved là 1
                var enrollment = new Enrollment
                {
                    EnrollmentId = newEnrollmentId,
                    UserId = userId,
                    CourseId = courseId,
                    EnrollmentStatus = 1, // Đặt status là 1
                    Approved = true, // Đặt approved là 1
                    EnrollmentCreatedAt = DateTime.Now
                };

                _context.Enrollments.Add(enrollment);

                // Trừ điểm của người dùng sau khi đăng ký khóa học thành công
                user.PaymentPoint -= course.Price;
                _context.SaveChanges();

                return RedirectToAction("CourseDetail", new { id = courseId });
            }
            else
            {
                // Nếu không đủ điểm, trả về thông báo lỗi
                TempData["ErrorMessage"] = "You do not have enough points to enroll in this course.";
                return RedirectToAction("CourseDetail", new { id = courseId });
            }
        }



        [HttpPost]
        public IActionResult RequestToAdmin(string courseId)
        {
            // Lấy khóa học dựa trên courseId
            var course = _context.Courses
                .Include(c => c.Chapters)
                .ThenInclude(ch => ch.Lessons)
                .FirstOrDefault(c => c.CourseId == courseId);

            // Kiểm tra điều kiện: có ít nhất 1 chương và 1 bài học
            if (course != null && course.Chapters.Any(ch => ch.Lessons.Any()))
            {
                // Cập nhật status của khóa học thành 1 (Pending)
                course.CourseStatus = 1;
                _context.SaveChanges();

                return Json(new { success = true, message = "Request sent to Admin successfully." });
            }
            else
            {
                return Json(new { success = false, message = "The course must have at least 1 chapter and 1 lesson. Back to edit and add more" });
            }
        }

    }
}
