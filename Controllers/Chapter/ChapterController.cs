using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BrainStormEra.Views;
using BrainStormEra.Views.Chapter;
using BrainStormEra.Models;


namespace BrainStormEra.Controllers.Chapter
{
    public class ChapterController : Controller
    {
        private readonly SwpDb7Context context;
        public ChapterController(SwpDb7Context context)
        {
            this.context = context;
        }

        [HttpGet("/Chapter/EditChapter/{chapterId}")]
        public ActionResult EditChapter(String chapterId)
        {
            var chapter = context.Chapters
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
        public ActionResult EditChapter(BrainStormEra.Models.Chapter chapter)
        {
            var existingChapter = context.Chapters.Find(chapter.ChapterId);

            if (existingChapter == null)
            {
                return NotFound();
            }


            existingChapter.ChapterName = chapter.ChapterName;
            existingChapter.ChapterDescription = chapter.ChapterDescription;


            context.SaveChanges();

            return RedirectToAction("ViewChapters", new { courseId = chapter.CourseId });
        }


        [HttpGet("/Chapter/ViewChapters/{courseId}")]
        public ActionResult ViewChapters(string courseId)
        {

            var chapters = context.Chapters
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

            var existingChapters = await context.Chapters
                .Where(ch => ch.CourseId == courseId)
                .ToListAsync();


            return View(existingChapters);
        }

        public IActionResult EditChapter()
        {
            return View();
        }




        //DELETE


        [HttpGet("/Chapter/DeleteChapter/{courseId}")]
        public ActionResult DeleteChapter(string courseId)
        {
            var chapters = context.Chapters
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


            var chaptersToDelete = context.Chapters.Where(ch => ChapterIds.Contains(ch.ChapterId)).ToList();

            var courseId = chaptersToDelete.FirstOrDefault()?.CourseId;

            if (ChapterIds == null || !ChapterIds.Any())
            {
                return RedirectToAction("DeleteChapter", new { courseId = courseId });


            }


            if (!chaptersToDelete.Any())
            {
                return NotFound("Không tìm thấy chương nào với các ID được chọn.");
            }

            // Xóa các chương
            context.Chapters.RemoveRange(chaptersToDelete);
            context.SaveChanges();

            return RedirectToAction("DeleteChapter", new { courseId = courseId });
        }



        [HttpGet]
        public ActionResult ChapterManagement(String courseId)
        {

            if (string.IsNullOrEmpty(courseId))
            {
                return View("~/Views/Chapter/ChapterManagement.cshtml");
            }

            return View();
        }




        // POST: ChapterManagement
        [HttpPost]
        public ActionResult ChapterManagement(BrainStormEra.Models.Chapter chapter)
        {

            if (!ModelState.IsValid)
            {
                return View(chapter);
            }

            if (string.IsNullOrEmpty(chapter.CourseId))
            {
                ModelState.AddModelError("", "Course ID is required.");
                return View(chapter);
            }


            var lastChapterOrder = context.Chapters
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

            context.Chapters.Add(newChapter);
            context.SaveChanges();

            return RedirectToAction("ViewChapters", new { courseId = newChapter.CourseId });
        }




    }
}
