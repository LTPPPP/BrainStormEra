using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        public async Task<IActionResult> LessonManagement()
        {
            if (Request.Cookies.TryGetValue("ChapterId", out string chapterId))
            {
                List<Models.Lesson> lessons = await _context.Lessons
                    .Where(l => l.ChapterId == chapterId)
                    .ToListAsync();

                ViewBag.ChapterName = await _context.Chapters
                    .Where(c => c.ChapterId == chapterId)
                    .Select(c => c.ChapterName)
                    .FirstOrDefaultAsync();

                return View("LessonManagement", lessons);
            }
            else
            {
                return BadRequest("Chapter ID is missing.");
            }
        }

        // GET: Delete Lesson
        [HttpGet]
        public async Task<IActionResult> DeleteLesson()
        {
            if (!Request.Cookies.TryGetValue("ChapterId", out string chapterId))
            {
                return RedirectToAction("LessonManagement");
            }

            List<Models.Lesson> lessons = await _context.Lessons
                .Where(l => l.ChapterId == chapterId)
                .ToListAsync();

            ViewBag.MaxOrderLessonId = await _context.Lessons
                .Where(l => l.ChapterId == chapterId)
                .OrderByDescending(l => l.LessonOrder)
                .Select(l => l.LessonId)
                .FirstOrDefaultAsync();

            return View(lessons);
        }

        // POST: Delete Lesson
        [HttpPost, ActionName("DeleteLesson")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLesson(List<string> LessonIds)
        {
            var lessonsToDelete = await _context.Lessons
                .Where(l => LessonIds.Contains(l.LessonId))
                .ToListAsync();

            _context.Lessons.RemoveRange(lessonsToDelete);
            await _context.SaveChangesAsync();

            return RedirectToAction("DeleteLesson");
        }

        // GET: Create Lesson
        [HttpGet]
        public async Task<IActionResult> AddLesson()
        {
            var lessonModel = new Models.Lesson
            {
                LessonId = await GenerateNewLessonIdAsync()
            };

            ViewBag.Chapters = new SelectList(await _context.Chapters.Select(c => new { c.ChapterId, c.ChapterName }).ToListAsync(), "ChapterId", "ChapterName");
            return View(lessonModel);
        }

        // POST: Create Lesson
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLesson(Models.Lesson model, IFormFile? LessonContentFile, string? LessonLink)
        {
            if (string.IsNullOrEmpty(model.ChapterId) && Request.Cookies.TryGetValue("ChapterId", out string chapterIdFromCookie))
            {
                model.ChapterId = chapterIdFromCookie;
            }

            if (string.IsNullOrEmpty(model.ChapterId))
            {
                ModelState.AddModelError("ChapterId", "Chapter ID is required.");
            }

            if (model.LessonTypeId == 1) // Video lesson type
            {
                if (string.IsNullOrEmpty(LessonLink))
                {
                    ModelState.AddModelError("LessonContent", "Please provide a YouTube link for video lessons.");
                }
                else
                {
                    model.LessonContent = LessonLink;
                }
            }
            else if (model.LessonTypeId == 2) // Reading lesson type
            {
                if (LessonContentFile == null || LessonContentFile.Length == 0)
                {
                    ModelState.AddModelError("LessonContent", "Please upload a document for reading lessons.");
                }
                else
                {
                    var allowedExtensions = new[] { ".doc", ".docx", ".pdf" };
                    var fileExtension = Path.GetExtension(LessonContentFile.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("LessonContent", "Only .pdf files are allowed.");
                    }
                    else
                    {
                        var filePath = Path.Combine("wwwroot/uploads/lessons", LessonContentFile.FileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            LessonContentFile.CopyTo(stream);
                        }
                        model.LessonContent = "/uploads/lessons/" + LessonContentFile.FileName;
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Chapters = new SelectList(await _context.Chapters.Select(c => new { c.ChapterId, c.ChapterName }).ToListAsync(), "ChapterId", "ChapterName");
                return View(model);
            }

            model.LessonOrder = await GetNextLessonOrderAsync(model.ChapterId);
            model.LessonId = await GenerateNewLessonIdAsync();

            _context.Lessons.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("LessonManagement");
        }

        // GET: Edit Lesson
        [HttpGet]
        public async Task<IActionResult> EditLesson()
        {
            var lessonId = HttpContext.Request.Cookies["LessonId"];

            if (string.IsNullOrEmpty(lessonId))
            {
                return RedirectToAction("LessonManagement");
            }

            var lesson = await _context.Lessons.FindAsync(lessonId);

            if (lesson == null)
            {
                return NotFound();
            }

            ViewBag.ExistingFilePath = lesson.LessonContent;
            return View(lesson);
        }

        // POST: Edit Lesson
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLesson(Models.Lesson model, IFormFile? LessonContentFile, string? LessonLink)
        {
            if (model.LessonTypeId == 1) // Video lesson type
            {
                if (string.IsNullOrEmpty(LessonLink))
                {
                    ModelState.AddModelError("LessonContent", "Please provide a YouTube link for video lessons.");
                }
                else
                {
                    model.LessonContent = LessonLink;
                }
            }
            else if (model.LessonTypeId == 2) // Reading lesson type
            {
                if (LessonContentFile != null && LessonContentFile.Length > 0)
                {
                    try
                    {
                        model.LessonContent = await SaveLessonFileAsync(LessonContentFile);
                    }
                    catch (InvalidOperationException ex)
                    {
                        ModelState.AddModelError("LessonContent", ex.Message);
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _context.Lessons.Update(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("LessonManagement");
        }

        [HttpGet]
        public async Task<IActionResult> ViewLessonLearner(string lessonId)
        {
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            string courseId = Request.Cookies["CourseId"];
            var course = await _context.Courses.FindAsync(courseId);

            if (string.IsNullOrEmpty(courseId) || string.IsNullOrEmpty(userId))
            {
                return BadRequest("CourseId or UserId is missing.");
            }

            if (string.IsNullOrEmpty(lessonId))
            {
                var firstLesson = await _context.Lessons
                    .Include(l => l.Chapter)
                    .Where(l => l.Chapter.CourseId == courseId)
                    .OrderBy(l => l.Chapter.ChapterOrder)
                    .ThenBy(l => l.LessonOrder)
                    .FirstOrDefaultAsync();

                lessonId = firstLesson?.LessonId;

                if (!string.IsNullOrEmpty(lessonId))
                {
                    Response.Cookies.Append("LessonId", lessonId, new CookieOptions { Path = "/" });
                }
            }

            var lesson = await _context.Lessons
                .Include(l => l.Chapter)
                .ThenInclude(c => c.Course)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId && l.Chapter.CourseId == courseId);

            var chapterId = lesson?.ChapterId;

            // Thêm cookie ChapterId
            if (!string.IsNullOrEmpty(chapterId))
            {
                Response.Cookies.Append("ChapterId", chapterId, new CookieOptions { Path = "/" });
            }

            var chapter = await _context.Chapters.FindAsync(chapterId);

            if (lesson == null)
            {
                return NotFound();
            }

            bool isCompleted = await _context.LessonCompletions
                .AnyAsync(lc => lc.UserId == userId && lc.LessonId == lessonId);

            var completedLessonIds = await _context.LessonCompletions
                .Where(lc => lc.UserId == userId && lc.Lesson.Chapter.CourseId == courseId)
                .Select(lc => lc.LessonId)
                .ToListAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    lessonName = lesson.LessonName,
                    lessonDescription = lesson.LessonDescription,
                    lessonContent = FormatYoutubeUrl(lesson.LessonContent, lesson.LessonTypeId),
                    lessonTypeId = lesson.LessonTypeId,
                    isCompleted = isCompleted
                });
            }

            ViewBag.Lessons = await _context.Lessons
                .Where(l => l.Chapter.CourseId == courseId)
                .ToListAsync();

            ViewBag.Chapters = await _context.Chapters
                .Where(c => c.CourseId == courseId)
                .ToListAsync();

            ViewBag.CompletedLessons = completedLessonIds;
            ViewBag.IsCompleted = isCompleted;

            ViewBag.CourseName = course?.CourseName ?? "Unknown Course";
            ViewBag.ChapterName = chapter?.ChapterName ?? "Unknown Chapter";
            ViewBag.LessonName = lesson?.LessonName ?? "Unknown Lesson";

            return View(lesson);
        }

        // POST: Mark Lesson Completed
        [HttpPost]
        public async Task<JsonResult> MarkLessonCompleted([FromBody] LessonCompletionRequest request)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not logged in." });
            }

            var completion = new LessonCompletion
            {
                CompletionId = await GenerateNewCompletionIdAsync(),
                UserId = userId,
                LessonId = request.LessonId,
                CompletionDate = DateTime.Now
            };

            _context.LessonCompletions.Add(completion);
            await _context.SaveChangesAsync();

            string courseId = await _context.Lessons
                .Where(l => l.LessonId == request.LessonId)
                .Select(l => l.Chapter.CourseId)
                .FirstOrDefaultAsync();

            bool allLessonsCompleted = await _context.Lessons
                .Where(l => l.Chapter.CourseId == courseId)
                .AllAsync(l => _context.LessonCompletions.Any(lc => lc.UserId == userId && lc.LessonId == l.LessonId));

            if (allLessonsCompleted)
            {
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

                if (enrollment != null)
                {
                    enrollment.EnrollmentStatus = 5;
                    enrollment.CertificateIssuedDate = DateTime.Now;

                    var notification = new Notification
                    {
                        NotificationId = await GenerateNewNotificationIdAsync(),
                        UserId = userId,
                        CourseId = courseId,
                        NotificationTitle = "Congratulations",
                        NotificationContent = "Congratulations, you have received a new certificate!",
                        NotificationType = "Info",
                        NotificationCreatedAt = DateTime.Now,
                        CreatedBy = userId
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }
            }

            return Json(new { success = true });
        }

        private string FormatYoutubeUrl(string url, int lessonTypeId)
        {
            if (lessonTypeId == 1 && !string.IsNullOrEmpty(url))
            {
                if (url.Contains("youtu.be"))
                {
                    return url.Replace("youtu.be/", "www.youtube.com/embed/");
                }
                else if (url.Contains("watch?v="))
                {
                    return url.Replace("watch?v=", "embed/");
                }
            }
            return url;
        }

        private async Task<string> GenerateNewLessonIdAsync()
        {
            int maxId = await _context.Lessons
                .Where(l => l.LessonId.StartsWith("LE"))
                .Select(l => int.Parse(l.LessonId.Substring(2)))
                .DefaultIfEmpty(0)
                .MaxAsync();

            return "LE" + (maxId + 1).ToString("D3");
        }

        private async Task<string> GenerateNewCompletionIdAsync()
        {
            int maxId = await _context.LessonCompletions
                .Where(lc => lc.CompletionId.StartsWith("LC"))
                .Select(lc => int.Parse(lc.CompletionId.Substring(2)))
                .DefaultIfEmpty(0)
                .MaxAsync();

            return "LC" + (maxId + 1).ToString("D3");
        }

        private async Task<string> GenerateNewNotificationIdAsync()
        {
            int maxId = await _context.Notifications
                .Where(n => n.NotificationId.StartsWith("N"))
                .Select(n => int.Parse(n.NotificationId.Substring(1)))
                .DefaultIfEmpty(0)
                .MaxAsync();

            return "N" + (maxId + 1).ToString("D3");
        }

        private async Task<int> GetNextLessonOrderAsync(string chapterId)
        {
            int lessonOrder = await _context.Lessons
                .Where(l => l.ChapterId == chapterId)
                .Select(l => l.LessonOrder)
                .DefaultIfEmpty(0)
                .MaxAsync();

            return lessonOrder + 1;
        }

        private async Task<string> SaveLessonFileAsync(IFormFile file)
        {
            var allowedExtensions = new[] { ".doc", ".docx", ".pdf" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new InvalidOperationException("Only .doc, .docx, and .pdf files are allowed.");
            }

            var filePath = Path.Combine("wwwroot/uploads/lessons", file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return "/uploads/lessons/" + file.FileName;
        }

        public class LessonCompletionRequest
        {
            public string LessonId { get; set; }
        }
    }
}
