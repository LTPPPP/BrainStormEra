using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BrainStormEra.Controllers.Lesson
{
    public class LessonController : Controller
    {
        private readonly SwpMainContext _context;

        public LessonController(SwpMainContext context)
        {
            _context = context;
        }

        // GET: View Lessons
        [HttpGet]
        public IActionResult LessonManagement()
        {
            if (Request.Cookies.TryGetValue("ChapterId", out string chapterId))
            {
                // Lấy danh sách bài học của chương
                List<BrainStormEra.Models.Lesson> lessons = _context.Lessons
                    .Where(l => l.ChapterId == chapterId)
                    .ToList();

                // Lấy tên của Chapter từ database (giả sử bạn có bảng Chapter với cột ChapterName)
                var chapter = _context.Chapters.FirstOrDefault(c => c.ChapterId == chapterId);
                if (chapter != null)
                {
                    ViewBag.ChapterName = chapter.ChapterName;  // Truyền tên chapter vào ViewBag
                }
                else
                {
                    ViewBag.ChapterName = "Chapter Not Found";
                }

                return View("LessonManagement", lessons);
            }
            else
            {
                return BadRequest("Chapter ID is missing.");
            }
        }

        // GET: Delete Lesson
        [HttpGet]
        public IActionResult DeleteLesson()
        {
            // Lấy ChapterId từ cookie
            if (!Request.Cookies.TryGetValue("ChapterId", out string chapterId))
            {
                return RedirectToAction("LessonManagement");
            }

            // Lấy danh sách bài học thuộc ChapterId từ cookie
            var lessons = _context.Lessons
                .Where(l => l.ChapterId == chapterId)
                .OrderBy(l => l.LessonOrder) // Sắp xếp theo LessonOrder
                .ToList();

            // Xác định Lesson có LessonOrder cao nhất
            var maxOrderLessonId = lessons.OrderByDescending(l => l.LessonOrder).FirstOrDefault()?.LessonId;
            ViewBag.MaxOrderLessonId = maxOrderLessonId;

            return View(lessons);
        }


        // POST: Delete Lesson
        [HttpPost, ActionName("DeleteLesson")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteLesson(List<string> LessonIds)
        {
            if (LessonIds != null && LessonIds.Any())
            {
                var lessonsToDelete = _context.Lessons
                    .Where(l => LessonIds.Contains(l.LessonId))
                    .ToList();

                _context.Lessons.RemoveRange(lessonsToDelete);
                _context.SaveChanges();
            }

            return RedirectToAction("DeleteLesson");
        }

        // GET: Create Lesson
        [HttpGet]
        public IActionResult AddLesson()
        {
            ViewBag.Chapters = new SelectList(_context.Chapters, "ChapterId", "ChapterName");
            return View();
        }

        // POST: Create Lesson
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddLesson(BrainStormEra.Models.Lesson model, IFormFile? LessonContentFile, string? LessonLink)
        {
            // Lấy ChapterId từ cookie nếu model không có giá trị
            if (string.IsNullOrEmpty(model.ChapterId) && Request.Cookies.TryGetValue("ChapterId", out string chapterIdFromCookie))
            {
                model.ChapterId = chapterIdFromCookie;
            }

            // Kiểm tra lại nếu ChapterId vẫn null sau khi thử lấy từ cookie
            if (string.IsNullOrEmpty(model.ChapterId))
            {
                ModelState.AddModelError("ChapterId", "Chapter ID is required.");
            }

            // Tìm LessonId lớn nhất hiện có và tăng lên 1 với tiền tố "LE"
            var maxId = _context.Lessons
                .AsEnumerable() // Tải dữ liệu vào bộ nhớ để xử lý phía client
                .Where(l => l.LessonId.StartsWith("LE"))
                .Select(l => int.TryParse(l.LessonId.Substring(2), out int id) ? id : 0) // Bỏ tiền tố "LE" và chuyển thành số nguyên, nếu không chuyển được thì mặc định là 0
                .DefaultIfEmpty(0) // Nếu không có LessonId nào, trả về 0
                .Max();

            // Tạo LessonId mới với tiền tố "LE" và tăng giá trị số lên 1
            model.LessonId = "LE" + (maxId + 1).ToString("D3"); // D3 sẽ đảm bảo rằng số có 3 chữ số (ví dụ: 004)
            ModelState.Remove("LessonId");

            // Thiết lập LessonCreatedAt là thời gian hiện tại
            model.LessonCreatedAt = DateTime.Now;

            // Thiết lập LessonStatus mặc định là 4
            model.LessonStatus = 4;

            // Automatically set LessonOrder to the next available order in the same ChapterId
            model.LessonOrder = _context.Lessons
                                .Where(l => l.ChapterId == model.ChapterId)
                                .OrderByDescending(l => l.LessonOrder)
                                .Select(l => l.LessonOrder)
                                .FirstOrDefault() + 1;
            ModelState.Remove("LessonOrder");

            // Check for duplicate LessonName within the same ChapterId
            var isDuplicateName = _context.Lessons
                .Any(l => l.ChapterId == model.ChapterId && l.LessonName == model.LessonName);

            if (isDuplicateName)
            {
                ModelState.AddModelError("LessonName", "Lesson name already exists in this chapter. Please choose a different name.");
            }

            // Validate LessonTypeId and lesson content based on type
            if (model.LessonTypeId == 1) // Video
            {
                if (string.IsNullOrEmpty(LessonLink))
                {
                    ModelState.AddModelError("LessonLink", "Please enter a YouTube link for the video lesson.");
                }
                else if (!LessonLink.StartsWith("https://www.youtube.com") && !LessonLink.StartsWith("https://youtu.be"))
                {
                    ModelState.AddModelError("LessonLink", "Invalid YouTube link.");
                }
                else
                {
                    model.LessonContent = LessonLink;
                    ModelState.Remove("LessonContent");
                }
            }
            else if (model.LessonTypeId == 2) // Reading
            {
                if (LessonContentFile != null && LessonContentFile.Length > 0)
                {
                    try
                    {
                        var fileName = Path.GetFileName(LessonContentFile.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "document", fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            LessonContentFile.CopyTo(stream);
                        }

                        model.LessonContent = "/lib/document/" + fileName;
                        ModelState.Remove("LessonContent");
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Error uploading file: " + ex.Message);
                    }
                }
                else
                {
                    ModelState.AddModelError("LessonContentFile", "Please upload a file for the reading lesson.");
                }
            }

            // Ensure all fields are not empty
            if (string.IsNullOrEmpty(model.LessonName))
            {
                ModelState.AddModelError("LessonName", "Please enter a lesson name.");
            }
            if (string.IsNullOrEmpty(model.LessonDescription))
            {
                ModelState.AddModelError("LessonDescription", "Please enter a lesson description.");
            }
            if (model.LessonOrder == 0)
            {
                ModelState.AddModelError("LessonOrder", "Please enter a valid lesson order.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Lessons.Add(model);
                    _context.SaveChanges();
                    return RedirectToAction("LessonManagement");
                }
                catch (DbUpdateException ex)
                {
                    Console.WriteLine(ex.InnerException?.Message);
                    ModelState.AddModelError("", "An error occurred while saving the lesson.");
                }
            }

            ViewBag.Chapters = new SelectList(_context.Chapters, "ChapterId", "ChapterName");
            return View(model);
        }


        // GET: Edit Lesson
        [HttpGet]
        public IActionResult EditLesson()
        {
            // Lấy LessonId từ Cookie
            var lessonId = HttpContext.Request.Cookies["LessonId"];

            if (string.IsNullOrEmpty(lessonId))
            {
                // Nếu LessonId không tồn tại trong cookie, chuyển hướng về trang ViewLesson
                return RedirectToAction("LessonManagement");
            }

            // Lấy dữ liệu bài học dựa trên LessonId
            var lesson = _context.Lessons.FirstOrDefault(l => l.LessonId == lessonId);

            if (lesson == null)
            {
                // Nếu không tìm thấy bài học, trả về lỗi NotFound
                return NotFound();
            }

            // Trả về dữ liệu đường dẫn tệp nếu có
            if (!string.IsNullOrEmpty(lesson.LessonContent))
            {
                ViewBag.ExistingFilePath = lesson.LessonContent;
            }

            return View(lesson);
        }

        // POST: Edit Lesson
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditLesson(BrainStormEra.Models.Lesson model, IFormFile? LessonContentFile, string? LessonLink)
        {
            // Lấy dữ liệu bài học hiện tại từ cơ sở dữ liệu dựa trên LessonId
            var existingLesson = _context.Lessons.FirstOrDefault(l => l.LessonId == model.LessonId);
            if (existingLesson == null)
            {
                return NotFound();
            }

            // Nếu ChapterId của model trống, lấy từ cookie
            if (string.IsNullOrEmpty(model.ChapterId) && Request.Cookies.TryGetValue("ChapterId", out string chapterIdFromCookie))
            {
                model.ChapterId = chapterIdFromCookie;
            }
            else
            {
                // Giữ nguyên ChapterId từ existingLesson nếu không có trong cookie
                model.ChapterId = existingLesson.ChapterId;
            }

            // Giữ nguyên LessonStatus
            model.LessonStatus = existingLesson.LessonStatus;

            // Kiểm tra trùng lặp LessonName trong cùng một chương (ChapterId) khi chỉnh sửa
            var duplicateLesson = _context.Lessons
                .FirstOrDefault(l => l.LessonName == model.LessonName
                                     && l.ChapterId == model.ChapterId
                                     && l.LessonId != model.LessonId);

            if (duplicateLesson != null)
            {
                ModelState.AddModelError("LessonName", "Lesson name already exists in this chapter. Please choose a different name.");
            }

            // Kiểm tra loại bài học Video
            if (model.LessonTypeId == 1) // Video
            {
                if (string.IsNullOrEmpty(LessonLink))
                {
                    ModelState.AddModelError("LessonLink", "Please enter a YouTube link for video lessons.");
                }
                else if (!LessonLink.StartsWith("https://www.youtube.com") && !LessonLink.StartsWith("https://youtu.be"))
                {
                    ModelState.AddModelError("LessonLink", "Invalid YouTube link.");
                }
                else
                {
                    model.LessonContent = LessonLink; // Gán URL vào LessonContent
                    ModelState.Remove("LessonContent"); // Xóa yêu cầu LessonContent
                }
            }
            else if (model.LessonTypeId == 2) // Reading
            {
                if (LessonContentFile != null && LessonContentFile.Length > 0)
                {
                    try
                    {
                        var fileName = Path.GetFileName(LessonContentFile.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "document", fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            LessonContentFile.CopyTo(stream);
                        }

                        model.LessonContent = "/lib/document/" + fileName;
                        ModelState.Remove("LessonContent");
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Error uploading the file: " + ex.Message);
                    }
                }
                else
                {
                    ModelState.AddModelError("LessonContentFile", "Please upload a file for reading lessons.");
                }
            }

            if (ModelState.IsValid)
            {
                // Giữ nguyên các thông tin không chỉnh sửa
                model.LessonCreatedAt = existingLesson.LessonCreatedAt;
                model.LessonOrder = existingLesson.LessonOrder; // giữ nguyên LessonOrder

                // Cập nhật thực thể với các giá trị đã chỉnh sửa
                _context.Entry(existingLesson).State = EntityState.Detached; // Gỡ theo dõi thực thể cũ
                _context.Lessons.Update(model);
                _context.SaveChanges();

                return RedirectToAction("LessonManagement");
            }

            return View(model); // Trả về view nếu có lỗi
        }

        [HttpGet]
        public IActionResult ViewLessonLearner(string lessonId)
        {
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string courseId = Request.Cookies["CourseId"];

            if (string.IsNullOrEmpty(courseId) || string.IsNullOrEmpty(userId))
            {
                return BadRequest("CourseId or UserId is missing.");
            }

            // Nếu không có bài học nào được chọn, lấy bài học đầu tiên trong chương đầu tiên của khóa học
            if (string.IsNullOrEmpty(lessonId))
            {
                var firstChapter = _context.Chapters
                    .Where(c => c.CourseId == courseId)
                    .OrderBy(c => c.ChapterOrder)
                    .FirstOrDefault();

                if (firstChapter != null)
                {
                    var firstLesson = _context.Lessons
                        .Where(l => l.ChapterId == firstChapter.ChapterId)
                        .OrderBy(l => l.LessonOrder)
                        .FirstOrDefault();
                    lessonId = firstLesson?.LessonId;
                }
            }

            // Lấy thông tin bài học được chọn
            var lesson = _context.Lessons.FirstOrDefault(l => l.LessonId == lessonId && l.Chapter.CourseId == courseId);
            if (lesson == null) return NotFound();

            // Kiểm tra xem bài học hiện tại đã được đánh dấu hoàn thành chưa
            bool isCompleted = _context.LessonCompletions.Any(lc => lc.UserId == userId && lc.LessonId == lessonId);

            // Lấy danh sách tất cả các bài học đã hoàn thành của người dùng trong khóa học này
            var completedLessonIds = _context.LessonCompletions
                                              .Where(lc => lc.UserId == userId && lc.Lesson.Chapter.CourseId == courseId)
                                              .Select(lc => lc.LessonId)
                                              .ToList();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    lessonName = lesson.LessonName,
                    lessonDescription = lesson.LessonDescription,
                    lessonContent = lesson.LessonTypeId == 1 ? FormatYoutubeUrl(lesson.LessonContent) : lesson.LessonContent,
                    lessonTypeId = lesson.LessonTypeId,
                    isCompleted = isCompleted
                });
            }

            // Truyền dữ liệu tới view
            ViewBag.Lessons = _context.Lessons.Where(l => l.Chapter.CourseId == courseId).ToList();
            ViewBag.Chapters = _context.Chapters.Where(c => c.CourseId == courseId).ToList();
            ViewBag.CompletedLessons = completedLessonIds; // Truyền danh sách các bài học đã hoàn thành
            ViewBag.IsCompleted = isCompleted;

            return View(lesson);
        }

        // Hàm định dạng URL của YouTube nếu là video
        private string FormatYoutubeUrl(string url)
        {
            if (url.Contains("youtu.be"))
            {
                return url.Replace("youtu.be/", "www.youtube.com/embed/");
            }
            else if (url.Contains("watch?v="))
            {
                return url.Replace("watch?v=", "embed/");
            }
            return url;
        }

        [HttpPost]
        public JsonResult MarkLessonCompleted([FromBody] LessonCompletionRequest request)
        {
            // Lấy user_id từ thông tin người dùng đăng nhập
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not logged in." });
            }

            var lessonId = request.LessonId;

            // Kiểm tra xem bài học này đã được đánh dấu hoàn thành chưa
            var existingCompletion = _context.LessonCompletions
                                             .FirstOrDefault(lc => lc.UserId == userId && lc.LessonId == lessonId);

            if (existingCompletion == null)
            {
                // Tạo LessonCompleteId mới
                var maxCompletionId = _context.LessonCompletions
                                              .Where(lc => lc.CompletionId.StartsWith("LC"))
                                              .OrderByDescending(lc => lc.CompletionId)
                                              .Select(lc => lc.CompletionId)
                                              .FirstOrDefault();

                int maxId = 0;
                if (!string.IsNullOrEmpty(maxCompletionId))
                {
                    int.TryParse(maxCompletionId.Substring(2), out maxId);
                }

                var newCompletionId = "LC" + (maxId + 1).ToString("D3");

                // Thêm bản ghi hoàn thành mới
                _context.LessonCompletions.Add(new LessonCompletion
                {
                    CompletionId = newCompletionId,
                    UserId = userId,
                    LessonId = lessonId,
                    CompletionDate = DateTime.Now
                });
                _context.SaveChanges();

                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Lesson already marked as completed." });
        }

        public class LessonCompletionRequest
        {
            public string LessonId { get; set; }
        }
    }
}