using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra.Controllers
{
    public class ChapterController : Controller
    {
        private readonly SwpMainContext _context;

        public ChapterController(SwpMainContext context)
        {
            _context = context;
        }

        // GET: EditChapter
        public async Task<IActionResult> EditChapter()
        {
            var chapterId = HttpContext.Request.Cookies["ChapterId"];
            var chapter = await _context.Chapters.FindAsync(chapterId);

            return View(chapter);
        }

        // POST: EditChapter
        [HttpPost]
        public async Task<IActionResult> EditChapter(Chapter chapter)
        {
            var isDuplicate = await _context.Chapters
                .AnyAsync(c => c.ChapterName == chapter.ChapterName && c.ChapterId != chapter.ChapterId);

            if (isDuplicate)
            {
                ModelState.AddModelError("ChapterName", "Chapter name already exists. Please choose a different name.");
                return View(chapter);
            }

            _context.Chapters.Update(chapter);
            await _context.SaveChangesAsync();

            return RedirectToAction("ChapterManagement");
        }

        // GET: ChapterManagement
        [HttpGet]
        public async Task<IActionResult> ChapterManagement()
        {
            var courseId = HttpContext.Request.Cookies["CourseId"];
            var chapters = await _context.Chapters
                .Where(c => c.CourseId == courseId)
                .OrderBy(c => c.ChapterOrder)
                .ToListAsync();

            var course = await _context.Courses.FindAsync(courseId);
            ViewBag.courseName = course?.CourseName;

            return View(chapters);
        }

        // GET: CreateChapter
        [HttpGet]
        public async Task<IActionResult> CreateChapter(string courseId)
        {
            var existingChapters = await _context.Chapters
                .Where(c => c.CourseId == courseId)
                .OrderBy(c => c.ChapterOrder)
                .ToListAsync();
            return View(existingChapters);
        }

        // GET: DeleteChapter
        [HttpGet]
        public async Task<IActionResult> DeleteChapter()
        {
            var courseId = HttpContext.Request.Cookies["CourseId"];
            var chapters = await _context.Chapters
                .Where(c => c.CourseId == courseId)
                .OrderBy(c => c.ChapterOrder)
                .ToListAsync();

            var maxOrderChapter = chapters.OrderByDescending(c => c.ChapterOrder).FirstOrDefault();
            ViewBag.MaxOrderChapterId = maxOrderChapter?.ChapterId;

            return View(chapters);
        }

        // POST: DeleteChapter
        [HttpPost]
        public async Task<IActionResult> DeleteChapter(List<string> chapterIds)
        {
            if (chapterIds.Count > 0)
            {
                var chaptersToDelete = await _context.Chapters
                    .Where(c => chapterIds.Contains(c.ChapterId))
                    .ToListAsync();

                _context.Chapters.RemoveRange(chaptersToDelete);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("DeleteChapter");
        }

        // GET: AddChapter
        [HttpGet]
        public IActionResult AddChapter()
        {
            return View("AddChapter");
        }

        // POST: AddChapter
        [HttpPost]
        public async Task<IActionResult> AddChapter(Chapter chapter)
        {
            var courseId = HttpContext.Request.Cookies["CourseId"];

            var isDuplicate = await _context.Chapters
                .AnyAsync(c => c.ChapterName == chapter.ChapterName && c.CourseId == courseId);
            if (isDuplicate)
            {
                ModelState.AddModelError("ChapterName", "Chapter name already exists in this course. Please choose a different name.");
                return View(chapter);
            }

            var lastChapter = await _context.Chapters
                .Where(c => c.CourseId == courseId)
                .OrderByDescending(c => c.ChapterOrder)
                .FirstOrDefaultAsync();
            var newChapterOrder = (lastChapter?.ChapterOrder ?? 0) + 1;

            var newChapterId = (await _context.Chapters
                .OrderByDescending(c => c.ChapterId)
                .Select(c => c.ChapterId)
                .FirstOrDefaultAsync()) ?? "CH000";
            newChapterId = "CH" + (int.Parse(newChapterId.Substring(2)) + 1).ToString("D3");

            chapter.ChapterId = newChapterId;
            chapter.CourseId = courseId;
            chapter.ChapterOrder = newChapterOrder;
            chapter.ChapterStatus = 0;

            _context.Chapters.Add(chapter);
            await _context.SaveChangesAsync();

            return RedirectToAction("ChapterManagement");
        }
    }
}
