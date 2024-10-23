using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BrainStormEra.Views;
using BrainStormEra.Views.Chapter;
using BrainStormEra.Models;


namespace BrainStormEra.Controllers.Chapter
{
    public class ChapterController : Controller
    {

        private readonly SwpDb7Context _context; // Define the context as a private field

        public ChapterController(SwpDb7Context context)
        {
            _context = context; // Properly assign the injected context to the private field
        }


        [HttpGet("/Chapter/EditChapter/{chapterId}")]
        public IActionResult EditChapter(String chapterId)
        {
            var chapter = _context.Chapters
                .Where(ch => ch.ChapterId == chapterId)
                .Select(ch => new BrainStormEra.Models.Chapter
                {
                    ChapterId = ch.ChapterId,
                    CourseId = ch.CourseId,

                    ChapterName = ch.ChapterName,
                    ChapterDescription = ch.ChapterDescription,
                    ChapterOrder = ch.ChapterOrder,
                    ChapterStatus = ch.ChapterStatus,
                    ChapterCreatedAt = ch.ChapterCreatedAt
                })
                .FirstOrDefault();
            if (chapter == null)
            {
                return NotFound();
            }
            return View(chapter);
        }


        [HttpPost]
        public IActionResult EditChapter(BrainStormEra.Models.Chapter chapter)
        {
            var existingChapter = _context.Chapters.Find(chapter.ChapterId);


            existingChapter.ChapterName = chapter.ChapterName;
            existingChapter.ChapterDescription = chapter.ChapterDescription;
            _context.SaveChanges();

            return RedirectToAction("ViewChapters", new { courseId = chapter.CourseId });
        }


        [HttpGet]
        public IActionResult ViewChapters(string courseId)
        {
            var chapters = _context.Chapters
                .Where(ch => ch.CourseId == courseId)
                .Select(ch => new BrainStormEra.Models.Chapter
                {
                    ChapterId = ch.ChapterId,
                    CourseId = ch.CourseId,
                    ChapterName = ch.ChapterName,
                    ChapterDescription = ch.ChapterDescription,
                    ChapterOrder = ch.ChapterOrder,
                    ChapterStatus = ch.ChapterStatus,
                    ChapterCreatedAt = ch.ChapterCreatedAt
                })
                .ToList();

            return View(chapters);
        }


        [HttpGet]
        public async Task<IActionResult> CreateChapter(string courseId)
        {
            if (string.IsNullOrEmpty(courseId))
            {
                return BadRequest("Course ID is required.");
            }
            var existingChapters = await _context.Chapters
                .Where(ch => ch.CourseId == courseId)
                .ToListAsync();
            return View(existingChapters);
        }
        public IActionResult EditChapter()
        {
            return View();
        }


        [HttpGet]
        public IActionResult DeleteChapter(string courseId)
        {
            var chapters = _context.Chapters
                .Where(ch => ch.CourseId == courseId)
                .Select(ch => new BrainStormEra.Models.Chapter
                {
                    ChapterId = ch.ChapterId,
                    CourseId = ch.CourseId,
                    ChapterName = ch.ChapterName,
                    ChapterDescription = ch.ChapterDescription,
                    ChapterOrder = ch.ChapterOrder,
                    ChapterStatus = ch.ChapterStatus,
                    ChapterCreatedAt = ch.ChapterCreatedAt
                })
                .ToList();
            return View(chapters);
        }


        [HttpPost]
        public IActionResult DeleteChapter(List<string> ChapterIds)
        {
            var chaptersToDelete = _context.Chapters.Where(ch => ChapterIds.Contains(ch.ChapterId)).ToList();
            var courseId = chaptersToDelete.FirstOrDefault()?.CourseId;
            if (ChapterIds == null || !ChapterIds.Any())
            {
                return RedirectToAction("DeleteChapter", new { courseId = courseId });
            }

            // Xóa các chương
            _context.Chapters.RemoveRange(chaptersToDelete);
            _context.SaveChanges();
            return RedirectToAction("DeleteChapter", new { courseId = courseId });
        }


        [HttpGet]
        public IActionResult ChapterManagement(String courseId)
        {
            if (string.IsNullOrEmpty(courseId))
            {
                RedirectToAction("ViewChapters");
            }
            return View();
        }


        // POST: ChapterManagement
        [HttpPost]
        public IActionResult ChapterManagement(BrainStormEra.Models.Chapter chapter)
        {
            if (!ModelState.IsValid)
            {
                return View(chapter);
            }

            if (string.IsNullOrEmpty(chapter.CourseId))
            {
                return View(chapter);
            }

            var lastChapterOrder = _context.Chapters
           .Where(c => c.CourseId == chapter.CourseId)
           .OrderByDescending(c => c.ChapterOrder)
           .Select(c => c.ChapterOrder)
           .FirstOrDefault();

            chapter.ChapterOrder = (lastChapterOrder == null || lastChapterOrder == 0) ? 1 : lastChapterOrder + 1;
            var newChapter = new BrainStormEra.Models.Chapter
            {
                ChapterId = chapter.ChapterId,
                ChapterName = chapter.ChapterName,
                ChapterDescription = chapter.ChapterDescription,
                ChapterOrder = chapter.ChapterOrder,
                CourseId = chapter.CourseId,
                ChapterStatus = 0
            };
            _context.Chapters.Add(newChapter);
            _context.SaveChanges();

            return RedirectToAction("ViewChapters", new { courseId = newChapter.CourseId });
        }

    }
}
