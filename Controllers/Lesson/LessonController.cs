using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra.Controllers.Lessons
{
	public class LessonController : Controller
	{

		private readonly SwpDb7Context _context;

		public LessonController(SwpDb7Context context)
		{
			_context = context;
		}



		// GET: View Lessons
		[HttpGet]
		public IActionResult ViewLesson()
		{
			var lessons = _context.Lessons.ToList();
			return View(lessons);
		}

		/// GET: Delete Lesson
		[HttpGet]
		public IActionResult DeleteLesson(string id)
		{
			var lesson = _context.Lessons.FirstOrDefault(l => l.LessonId == id);
			if (lesson == null)
			{
				return NotFound();
			}

			return View(lesson);
		}

		// POST: Delete Lesson
		[HttpPost, ActionName("DeleteLesson")]
		[ValidateAntiForgeryToken]
		public IActionResult DeleteConfirmed(string id)
		{
			var lesson = _context.Lessons.FirstOrDefault(l => l.LessonId == id);
			if (lesson == null)
			{
				return NotFound();
			}

			_context.Lessons.Remove(lesson);
			_context.SaveChanges();

			return RedirectToAction("ViewLesson");
		}

		// GET: Create Lesson
		[HttpGet]
		public IActionResult CreateLesson()
		{
			ViewBag.Chapters = new SelectList(_context.Chapters, "ChapterId", "ChapterName");
			return View();
		}

		//POST: Create Lesson
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult CreateLesson(Lesson model, IFormFile? LessonContentFile, string? LessonLink)
		{
			var existingLesson = _context.Lessons.FirstOrDefault(l => l.LessonId == model.LessonId);
			if (existingLesson != null)
			{
				ModelState.AddModelError("LessonId", "The Lesson ID already exists. Please choose a different ID.");
			}

			if (ModelState.IsValid)
			{
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
						model.LessonContent = LessonLink;
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
					if (string.IsNullOrEmpty(model.LessonId))
					{
						model.LessonId = Guid.NewGuid().ToString();
					}

					try
					{
						_context.Lessons.Add(model);
						_context.SaveChanges();
						return RedirectToAction("ViewLesson");
					}
					catch (DbUpdateException ex)
					{
						Console.WriteLine(ex.InnerException?.Message);
						ModelState.AddModelError("", "An error occurred while saving the lesson.");
					}
				}
			}

			return View(model);
		}

		// GET: Edit Lesson
		[HttpGet]
		public IActionResult EditLesson(string id)
		{
			var lesson = _context.Lessons.FirstOrDefault(l => l.LessonId == id);
			if (lesson == null)
			{
				return NotFound();
			}
			return View(lesson);
		}

		//POST: Edit lesson
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult EditLesson(Lesson model, IFormFile? LessonContentFile)
		{
			if (model.LessonTypeId == 2 && string.IsNullOrWhiteSpace(model.LessonContent) && LessonContentFile == null)
			{
				ModelState.AddModelError("LessonContent", "Lesson Content is required for Reading type.");
			}

			if (ModelState.IsValid)
			{
				if (model.LessonCreatedAt == DateTime.MinValue)
				{
					model.LessonCreatedAt = DateTime.Now;
				}

				if (model.LessonTypeId == 1 && LessonContentFile != null && LessonContentFile.Length > 0)
				{
					string videoDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "video");
					if (!Directory.Exists(videoDirectory))
					{
						Directory.CreateDirectory(videoDirectory);
					}

					var videoPath = Path.Combine(videoDirectory, LessonContentFile.FileName);
					try
					{
						using (var stream = new FileStream(videoPath, FileMode.Create))
						{
							LessonContentFile.CopyTo(stream);
						}
						model.LessonContent = "/lib/video/" + LessonContentFile.FileName;
					}
					catch (Exception ex)
					{
						ModelState.AddModelError("", $"Error uploading video: {ex.Message}");
						return View(model);
					}
				}
				else if (model.LessonTypeId == 2 && LessonContentFile != null && LessonContentFile.Length > 0)
				{
					string documentDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "document");
					if (!Directory.Exists(documentDirectory))
					{
						Directory.CreateDirectory(documentDirectory);
					}

					var documentPath = Path.Combine(documentDirectory, LessonContentFile.FileName);
					try
					{
						using (var stream = new FileStream(documentPath, FileMode.Create))
						{
							LessonContentFile.CopyTo(stream);
						}
						model.LessonContent = "/lib/document/" + LessonContentFile.FileName;
					}
					catch (Exception ex)
					{
						ModelState.AddModelError("", $"Error uploading document: {ex.Message}");
						return View(model);
					}
				}

				_context.Lessons.Update(model);
				_context.SaveChanges();

				return RedirectToAction("ViewLesson");
			}

			return View(model);
		}
	}
}
