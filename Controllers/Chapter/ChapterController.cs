using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using BrainStormEra.Repo.Chapter;
using System.Collections.Generic;
using System.Threading.Tasks;
using BrainStormEra.Repo.Course;

namespace BrainStormEra.Controllers
{
    public class ChapterController : Controller
    {
        private readonly ChapterRepo _chapterRepo;
        private readonly CourseRepo _courseRepo;

        public ChapterController(ChapterRepo chapterRepo, CourseRepo courseRepo)
        {
            _chapterRepo = chapterRepo;
            _courseRepo = courseRepo;
        }

        // GET: EditChapter
        public async Task<IActionResult> EditChapter()
        {
            var chapterId = HttpContext.Request.Cookies["ChapterId"];
            var chapter = await _chapterRepo.GetChapterByIdAsync(chapterId);

            return View(chapter);
        }

        // POST: EditChapter
        [HttpPost]
        public async Task<IActionResult> EditChapter(BrainStormEra.Models.Chapter chapter)
        {
            var isDuplicate = await _chapterRepo.IsChapterNameDuplicateAsync(chapter.ChapterName, chapter.ChapterId);

            if (isDuplicate)
            {
                ModelState.AddModelError("ChapterName", "Chapter name already exists. Please choose a different name.");
                return View(chapter);
            }

            await _chapterRepo.UpdateChapterAsync(chapter);

            return RedirectToAction("ChapterManagement");
        }

        // GET: ChapterManagement
        [HttpGet]
        public async Task<IActionResult> ChapterManagement()
        {
            var courseId = HttpContext.Request.Cookies["CourseId"];
            var chapters = await _chapterRepo.GetAllChaptersByCourseIdAsync(courseId);

            var course = await _courseRepo.GetCourseByIdAsync(courseId);
            ViewBag.courseName = course?.CourseName;

            return View(chapters);
        }

        // GET: CreateChapter
        [HttpGet]
        public async Task<IActionResult> CreateChapter(string courseId)
        {
            var existingChapters = await _chapterRepo.GetAllChaptersByCourseIdAsync(courseId);
            return View(existingChapters);
        }

        // GET: DeleteChapter
        [HttpGet]
        public async Task<IActionResult> DeleteChapter()
        {
            var courseId = HttpContext.Request.Cookies["CourseId"];
            var chapters = await _chapterRepo.GetAllChaptersByCourseIdAsync(courseId);

            var maxOrderChapter = await _chapterRepo.GetLastChapterInCourseAsync(courseId);
            ViewBag.MaxOrderChapterId = maxOrderChapter?.ChapterId;

            return View(chapters);
        }

        // POST: DeleteChapter
        [HttpPost]
        public async Task<IActionResult> DeleteChapter(List<string> chapterIds)
        {
            if (chapterIds.Count > 0)
            {
                await _chapterRepo.DeleteChaptersAsync(chapterIds);
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
        public async Task<IActionResult> AddChapter(BrainStormEra.Models.Chapter chapter)
        {
            var courseId = HttpContext.Request.Cookies["CourseId"];

            var isDuplicate = await _chapterRepo.IsChapterNameDuplicateAsync(chapter.ChapterName, courseId);
            if (isDuplicate)
            {
                ModelState.AddModelError("ChapterName", "Chapter name already exists in this course. Please choose a different name.");
                return View(chapter);
            }

            var lastChapter = await _chapterRepo.GetLastChapterInCourseAsync(courseId);
            var newChapterOrder = (lastChapter?.ChapterOrder ?? 0) + 1;

            var newChapterId = await _chapterRepo.GenerateNewChapterIdAsync();

            chapter.ChapterId = newChapterId;
            chapter.CourseId = courseId;
            chapter.ChapterOrder = newChapterOrder;
            chapter.ChapterStatus = 0;

            await _chapterRepo.AddChapterAsync(chapter);

            return RedirectToAction("ChapterManagement");
        }
    }
}
