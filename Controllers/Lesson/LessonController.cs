using BrainStormEra.Models;
using BrainStormEra.Repo;
using BrainStormEra.Repo.Chapter;
using BrainStormEra.Repo.Course;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace BrainStormEra.Controllers.Lesson
{
    public class LessonController : Controller
    {
        private readonly string _connectionString;
        private readonly SwpMainContext _context;
        private readonly LessonRepo _lessonRepo;
        private readonly ChapterRepo _chapterRepo;
        private readonly CourseRepo _courseRepo;
        public LessonController(IConfiguration configuration, SwpMainContext context, CourseRepo courseRepo, ChapterRepo chapterRepo, LessonRepo lessonRepo)
        {
            _connectionString = configuration.GetConnectionString("SwpMainContext");
            _context = context;
            _courseRepo = courseRepo;
            _chapterRepo = chapterRepo;
            _lessonRepo = lessonRepo;
        }

        // GET: View Lessons
        [HttpGet]
        public async Task<IActionResult> LessonManagement()
        {
            if (Request.Cookies.TryGetValue("ChapterId", out string chapterId))
            {
                List<Models.Lesson> lessons = await _lessonRepo.GetLessonsByChapterIdAsync(chapterId);
                ViewBag.ChapterName = await _lessonRepo.GetChapterNameByIdAsync(chapterId);

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

            List<Models.Lesson> lessons = await _lessonRepo.GetLessonsByChapterIdAsync(chapterId);
            ViewBag.MaxOrderLessonId = await _lessonRepo.GetMaxOrderLessonIdByChapterIdAsync(chapterId);

            return View(lessons);
        }


        // POST: Delete Lesson
        [HttpPost, ActionName("DeleteLesson")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLesson(List<string> LessonIds)
        {
            await _lessonRepo.DeleteLessonsAsync(LessonIds);
            return RedirectToAction("DeleteLesson");
        }

        // GET: Create Lesson
        [HttpGet]
        public async Task<IActionResult> AddLesson()
        {
            var lessonModel = new Models.Lesson
            {
                LessonId = await _lessonRepo.GenerateNewLessonIdAsync()
            };

            ViewBag.Chapters = new SelectList(await _lessonRepo.GetChaptersAsync(), "Value", "Text");
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
                ViewBag.Chapters = new SelectList(await _lessonRepo.GetChaptersAsync(), "Value", "Text");
                return View(model);
            }

            model.LessonOrder = await _lessonRepo.GetNextLessonOrderAsync(model.ChapterId);
            model.LessonId = await _lessonRepo.GenerateNewLessonIdAsync();

            await _lessonRepo.AddLessonAsync(model);
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

            var lesson = await _lessonRepo.GetLessonByIdAsync(lessonId);

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
                        model.LessonContent = await _lessonRepo.SaveLessonFileAsync(LessonContentFile);
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

            await _lessonRepo.UpdateLessonAsync(model);
            return RedirectToAction("LessonManagement");
        }

        [HttpGet]
        public async Task<IActionResult> ViewLessonLearner(string lessonId)
        {
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            string courseId = Request.Cookies["CourseId"];
            var course = await _courseRepo.GetCourseByIdAsync(courseId);

            if (string.IsNullOrEmpty(courseId) || string.IsNullOrEmpty(userId))
            {
                return BadRequest("CourseId or UserId is missing.");
            }

            if (string.IsNullOrEmpty(lessonId))
            {
                var firstLesson = await _lessonRepo.GetFirstLessonInCourseAsync(courseId);
                lessonId = firstLesson?.LessonId;

                if (!string.IsNullOrEmpty(lessonId))
                {
                    Response.Cookies.Append("LessonId", lessonId, new CookieOptions { Path = "/" });
                }
            }

            var lesson = await _lessonRepo.GetLessonByIdAndCourseAsync(lessonId, courseId);

            var chapterId = await _lessonRepo.GetChapterIdByLessonIdAsync(lessonId);

            // Thêm cookie ChapterId
            if (!string.IsNullOrEmpty(chapterId))
            {
                Response.Cookies.Append("ChapterId", chapterId, new CookieOptions { Path = "/" });
            }

            var chapter = await _chapterRepo.GetChapterByIdAsync(chapterId);

            if (lesson == null)
            {
                return NotFound();
            }

            bool isCompleted = await _lessonRepo.IsLessonCompletedAsync(userId, lessonId);
            var completedLessonIds = await _lessonRepo.GetCompletedLessonIdsAsync(userId, courseId);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    lessonName = lesson.LessonName,
                    lessonDescription = lesson.LessonDescription,
                    lessonContent = _lessonRepo.FormatYoutubeUrl(lesson.LessonContent, lesson.LessonTypeId),
                    lessonTypeId = lesson.LessonTypeId,
                    isCompleted = isCompleted
                });
            }

            ViewBag.Lessons = _context.Lessons.Where(l => l.Chapter.CourseId == courseId).ToList();
            ViewBag.Chapters = _context.Chapters.Where(c => c.CourseId == courseId).ToList();
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

            await _lessonRepo.MarkLessonCompletedAsync(userId, request.LessonId);


            string courseId = await _lessonRepo.GetCourseIdByLessonIdAsync(request.LessonId);
            bool allLessonsCompleted = await _lessonRepo.AreAllLessonsCompletedAsync(userId, courseId);

            if (allLessonsCompleted)
            {

                await _lessonRepo.UpdateEnrollmentStatusAsync(userId, courseId, 5);
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

        public class LessonCompletionRequest
        {
            public string LessonId { get; set; }
        }
    }
}
