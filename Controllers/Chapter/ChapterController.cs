using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BrainStormEra.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace BrainStormEra.Controllers.Chapter
{
    public class ChapterController : Controller
    {
        private readonly SwpMainContext _context;

        public ChapterController(SwpMainContext context)
        {
            _context = context;
        }

        // GET: EditChapter
        public IActionResult EditChapter()
        {
            var chapterId = HttpContext.Request.Cookies["ChapterId"];
            var sqlQuery = "SELECT * FROM chapter WHERE chapter_id = {0} ORDER BY chapter_order";
            var chapters = _context.Chapters.FromSqlRaw(sqlQuery, chapterId).ToList();

            // Check if result is not empty
            var chapter = chapters.Count > 0 ? chapters[0] : null;

            return View(chapter);
        }

        // POST: EditChapter
        [HttpPost]
        public IActionResult EditChapter(BrainStormEra.Models.Chapter chapter)
        {
            var sqlCheckDuplicate = "SELECT * FROM chapter WHERE chapter_name = {0} AND chapter_id != {1}";
            var duplicateChapters = _context.Chapters.FromSqlRaw(sqlCheckDuplicate, chapter.ChapterName, chapter.ChapterId).ToList();

            if (duplicateChapters.Count > 0)
            {
                ModelState.AddModelError("ChapterName", "Chapter name already exists. Please choose a different name.");
                return View(chapter);
            }

            var sqlUpdate = "UPDATE chapter SET chapter_name = {0}, chapter_description = {1} WHERE chapter_id = {2}";
            _context.Database.ExecuteSqlRaw(sqlUpdate, chapter.ChapterName, chapter.ChapterDescription, chapter.ChapterId);
            _context.SaveChanges();

            return RedirectToAction("ViewChapters");
        }

        // GET: ChapterManagement
        [HttpGet]
        public IActionResult ChapterManagement()
        {
            var courseId = HttpContext.Request.Cookies["CourseId"];
            var sqlCourse = "SELECT * FROM course WHERE course_id = {0}";
            var courses = _context.Courses.FromSqlRaw(sqlCourse, courseId).ToList();
            var course = courses.Count > 0 ? courses[0] : null;

            ViewBag.courseName = course?.CourseName;

            var sqlChapters = "SELECT * FROM chapter WHERE course_id = {0} ORDER BY chapter_order";
            var chapters = _context.Chapters.FromSqlRaw(sqlChapters, courseId).ToList();

            return View(chapters);
        }

        // GET: CreateChapter
        [HttpGet]
        public async Task<IActionResult> CreateChapter(string courseId)
        {
            var sqlQuery = "SELECT * FROM chapter WHERE course_id = {0}";
            var existingChapters = await _context.Chapters.FromSqlRaw(sqlQuery, courseId).ToListAsync();
            return View(existingChapters);
        }

        // GET: DeleteChapter
        [HttpGet]
        public IActionResult DeleteChapter()
        {
            var courseId = HttpContext.Request.Cookies["CourseId"];
            var sqlQuery = "SELECT * FROM chapter WHERE course_id = {0} ORDER BY chapter_order";
            var chapters = _context.Chapters.FromSqlRaw(sqlQuery, courseId).ToList();

            var maxOrderChapter = chapters.Count > 0 ? chapters[^1] : null;
            ViewBag.MaxOrderChapterId = maxOrderChapter?.ChapterId;

            return View(chapters);
        }

        // POST: DeleteChapter
        [HttpPost]
        public IActionResult DeleteChapter(List<string> ChapterIds)
        {
            if (ChapterIds.Count > 0)
            {
                var sqlDelete = $"DELETE FROM chapter WHERE chapter_id IN ({string.Join(",", ChapterIds.Select(id => $"'{id}'"))})";
                _context.Database.ExecuteSqlRaw(sqlDelete);
                _context.SaveChanges();
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
        // POST: AddChapter
        [HttpPost]
        public IActionResult AddChapter(BrainStormEra.Models.Chapter chapter)
        {
            // Retrieve CourseId from cookies
            var courseId = HttpContext.Request.Cookies["CourseId"];

            // Check if a chapter with the same name already exists in this course
            var sqlCheckExisting = "SELECT * FROM chapter WHERE course_id = {0} AND chapter_name = {1}";
            var existingChapters = _context.Chapters.FromSqlRaw(sqlCheckExisting, courseId, chapter.ChapterName).ToList();

            if (existingChapters.Count > 0)
            {
                ModelState.AddModelError("ChapterName", "Chapter name already exists in this course. Please choose a different name.");
                return View(chapter);
            }

            // Get the last chapter_order in the course for ordering the new chapter
            var sqlLastOrder = "SELECT TOP 1 * FROM chapter WHERE course_id = {0} ORDER BY chapter_order DESC";
            var lastChapterOrders = _context.Chapters.FromSqlRaw(sqlLastOrder, courseId).ToList();
            var lastChapterOrder = lastChapterOrders.Count > 0 ? lastChapterOrders[0].ChapterOrder : 0;
            var newChapterOrder = (lastChapterOrder == 0) ? 1 : lastChapterOrder + 1;

            // Generate a new chapter_id based on the last chapter_id in the table
            var sqlLastChapterId = "SELECT TOP 1 * FROM chapter ORDER BY chapter_id DESC";
            var lastChapters = _context.Chapters.FromSqlRaw(sqlLastChapterId).ToList();
            var lastChapter = lastChapters.Count > 0 ? lastChapters[0] : null;
            var newChapterId = lastChapter == null ? "CH001" : "CH" + (int.Parse(lastChapter.ChapterId.Substring(2)) + 1).ToString("D3");

            // Insert the new chapter into the database
            var sqlInsert = "INSERT INTO chapter (chapter_id, chapter_name, chapter_description, chapter_order, course_id, chapter_status) " +
                            "VALUES ({0}, {1}, {2}, {3}, {4}, {5})";
            _context.Database.ExecuteSqlRaw(sqlInsert, newChapterId, chapter.ChapterName, chapter.ChapterDescription, newChapterOrder, courseId, 0);
            _context.SaveChanges();

            return RedirectToAction("ChapterManagement");
        }

    }
}
