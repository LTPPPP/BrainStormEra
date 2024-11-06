using BrainStormEra.Models;
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

		public LessonController(IConfiguration configuration, SwpMainContext context)
		{
			_connectionString = configuration.GetConnectionString("SwpMainContext");
			_context = context;
		}

		// GET: View Lessons
		[HttpGet]
		public IActionResult LessonManagement()
		{
			if (Request.Cookies.TryGetValue("ChapterId", out string chapterId))
			{
				List<Models.Lesson> lessons = new List<Models.Lesson>();

				using (SqlConnection conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = "SELECT * FROM lesson WHERE chapter_id = @chapter_id";
					using (SqlCommand cmd = new SqlCommand(query, conn))
					{
						cmd.Parameters.AddWithValue("@chapter_id", chapterId);
						using (SqlDataReader reader = cmd.ExecuteReader())
						{
							while (reader.Read())
							{
								lessons.Add(new Models.Lesson
								{
									LessonId = reader["lesson_id"].ToString(),
									ChapterId = reader["chapter_id"].ToString(),
									LessonName = reader["lesson_name"].ToString(),
									LessonDescription = reader["lesson_description"].ToString(),
									LessonContent = reader["lesson_content"].ToString(),
									LessonOrder = (int)reader["lesson_order"],
									LessonStatus = (int)reader["lesson_status"],
									LessonTypeId = (int)reader["lesson_type_id"],
									LessonCreatedAt = (DateTime)reader["lesson_created_at"]
								});
							}
						}
					}
				}

				string chapterName = "Chapter Not Found";
				using (SqlConnection conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = "SELECT chapter_name FROM chapter WHERE chapter_id = @chapter_id";
					using (SqlCommand cmd = new SqlCommand(query, conn))
					{
						cmd.Parameters.AddWithValue("@chapter_id", chapterId);
						var result = cmd.ExecuteScalar();
						if (result != null)
						{
							chapterName = result.ToString();
						}
					}
				}

				ViewBag.ChapterName = chapterName;
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
			if (!Request.Cookies.TryGetValue("ChapterId", out string chapterId))
			{
				return RedirectToAction("LessonManagement");
			}

			List<Models.Lesson> lessons = new List<Models.Lesson>();
			string maxOrderLessonId = null;

			using (SqlConnection conn = new SqlConnection(_connectionString))
			{
				conn.Open();
				string query = "SELECT * FROM lesson WHERE chapter_id = @chapter_id ORDER BY lesson_order";
				using (SqlCommand cmd = new SqlCommand(query, conn))
				{
					cmd.Parameters.AddWithValue("@chapter_id", chapterId);
					using (SqlDataReader reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							lessons.Add(new Models.Lesson
							{
								LessonId = reader["lesson_id"].ToString(),
								ChapterId = reader["chapter_id"].ToString(),
								LessonName = reader["lesson_name"].ToString(),
								LessonDescription = reader["lesson_description"].ToString(),
								LessonOrder = (int)reader["lesson_order"],
								LessonStatus = (int)reader["lesson_status"],
								LessonTypeId = (int)reader["lesson_type_id"],
								LessonCreatedAt = (DateTime)reader["lesson_created_at"]
							});
						}
					}
				}

				if (lessons.Any())
				{
					maxOrderLessonId = lessons.OrderByDescending(l => l.LessonOrder).First().LessonId;
				}
			}

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
				using (SqlConnection conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = $"DELETE FROM lesson WHERE lesson_id IN ({string.Join(",", LessonIds.Select(id => $"'{id}'"))})";
					using (SqlCommand cmd = new SqlCommand(query, conn))
					{
						cmd.ExecuteNonQuery();
					}
				}
			}

			return RedirectToAction("DeleteLesson");
		}

		// GET: Create Lesson
		// GET: Create Lesson
		[HttpGet]
		public IActionResult AddLesson()
		{
			// Generate the new LessonId here based on maxId
			int maxId = 0;
			using (SqlConnection conn = new SqlConnection(_connectionString))
			{
				conn.Open();
				string query = "SELECT MAX(CAST(SUBSTRING(lesson_id, 3, LEN(lesson_id)-2) AS INT)) FROM lesson WHERE lesson_id LIKE 'LE%'";
				using (SqlCommand cmd = new SqlCommand(query, conn))
				{
					var result = cmd.ExecuteScalar();
					maxId = result != DBNull.Value ? (int)result : 0;
				}
			}

			var lessonModel = new Models.Lesson
			{
				LessonId = "LE" + (maxId + 1).ToString("D3")
			};

			List<SelectListItem> chapters = new List<SelectListItem>();
			using (SqlConnection conn = new SqlConnection(_connectionString))
			{
				conn.Open();
				string query = "SELECT chapter_id, chapter_name FROM chapter";
				using (SqlCommand cmd = new SqlCommand(query, conn))
				{
					using (SqlDataReader reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							chapters.Add(new SelectListItem
							{
								Value = reader["chapter_id"].ToString(),
								Text = reader["chapter_name"].ToString()
							});
						}
					}
				}
			}

			ViewBag.Chapters = new SelectList(chapters, "Value", "Text");
			return View(lessonModel);
		}


		// POST: Create Lesson
		// POST: Create Lesson
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult AddLesson(Models.Lesson model, IFormFile? LessonContentFile, string? LessonLink)
		{
			if (string.IsNullOrEmpty(model.ChapterId) && Request.Cookies.TryGetValue("ChapterId", out string chapterIdFromCookie))
			{
				model.ChapterId = chapterIdFromCookie;
			}

			if (string.IsNullOrEmpty(model.ChapterId))
			{
				ModelState.AddModelError("ChapterId", "Chapter ID is required.");
			}

			// Assign LessonContent based on LessonType
			if (model.LessonTypeId == 1) // Video lesson type
			{
				if (string.IsNullOrEmpty(LessonLink))
				{
					ModelState.AddModelError("LessonContent", "Please provide a YouTube link for video lessons.");
				}
				else
				{
					model.LessonContent = LessonLink; // Set LessonContent as YouTube link
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
					// Save the uploaded file to a designated directory
					var filePath = Path.Combine("wwwroot/uploads/lessons", LessonContentFile.FileName);
					using (var stream = new FileStream(filePath, FileMode.Create))
					{
						LessonContentFile.CopyTo(stream);
					}
					model.LessonContent = "/uploads/lessons/" + LessonContentFile.FileName; // Set LessonContent as file path
				}
			}
			if (!ModelState.IsValid)
			{
				foreach (var modelState in ModelState.Values)
				{
					foreach (var error in modelState.Errors)
					{
						Console.WriteLine(error.ErrorMessage);
					}
				}
			}

			if (!ModelState.IsValid)
			{
				// Re-load chapters if model state is invalid
				ViewBag.Chapters = new SelectList(_context.Chapters, "chapter_id", "chapter_name");
				return View(model);
			}

			// Generate LessonId
			int maxId = 0;
			using (SqlConnection conn = new SqlConnection(_connectionString))
			{
				conn.Open();
				string query = "SELECT MAX(CAST(SUBSTRING(lesson_id, 3, LEN(lesson_id)-2) AS INT)) FROM lesson WHERE lesson_id LIKE 'LE%'";
				using (SqlCommand cmd = new SqlCommand(query, conn))
				{
					var result = cmd.ExecuteScalar();
					maxId = result != DBNull.Value ? (int)result : 0;
				}
			}
			model.LessonId = "LE" + (maxId + 1).ToString("D3");

			// Insert lesson details into the database
			using (SqlConnection conn = new SqlConnection(_connectionString))
			{
				conn.Open();
				string query = "INSERT INTO lesson (lesson_id, chapter_id, lesson_name, lesson_description, lesson_content, lesson_order, lesson_status, lesson_type_id, lesson_created_at) " +
							   "VALUES (@lesson_id, @chapter_id, @lesson_name, @lesson_description, @lesson_content, @lesson_order, @lesson_status, @lesson_type_id, @lesson_created_at)";
				using (SqlCommand cmd = new SqlCommand(query, conn))
				{
					cmd.Parameters.AddWithValue("@lesson_id", model.LessonId);
					cmd.Parameters.AddWithValue("@chapter_id", model.ChapterId);
					cmd.Parameters.AddWithValue("@lesson_name", model.LessonName);
					cmd.Parameters.AddWithValue("@lesson_description", model.LessonDescription);
					cmd.Parameters.AddWithValue("@lesson_content", model.LessonContent);
					cmd.Parameters.AddWithValue("@lesson_order", model.LessonOrder);
					cmd.Parameters.AddWithValue("@lesson_status", 4);
					cmd.Parameters.AddWithValue("@lesson_type_id", model.LessonTypeId);
					cmd.Parameters.AddWithValue("@lesson_created_at", DateTime.Now);

					cmd.ExecuteNonQuery();
				}
			}

			return RedirectToAction("LessonManagement");
		}


		// GET: Edit Lesson
		[HttpGet]
		public IActionResult EditLesson()
		{
			var lessonId = HttpContext.Request.Cookies["LessonId"];

			if (string.IsNullOrEmpty(lessonId))
			{
				return RedirectToAction("LessonManagement");
			}

			Models.Lesson lesson = null;
			using (SqlConnection conn = new SqlConnection(_connectionString))
			{
				conn.Open();
				string query = "SELECT * FROM lesson WHERE lesson_id = @lesson_id";
				using (SqlCommand cmd = new SqlCommand(query, conn))
				{
					cmd.Parameters.AddWithValue("@lesson_id", lessonId);
					using (SqlDataReader reader = cmd.ExecuteReader())
					{
						if (reader.Read())
						{
							lesson = new Models.Lesson
							{
								LessonId = reader["lesson_id"].ToString(),
								ChapterId = reader["chapter_id"].ToString(),
								LessonName = reader["lesson_name"].ToString(),
								LessonDescription = reader["lesson_description"].ToString(),
								LessonContent = reader["lesson_content"].ToString(),
								LessonOrder = (int)reader["lesson_order"],
								LessonStatus = (int)reader["lesson_status"],
								LessonTypeId = (int)reader["lesson_type_id"],
								LessonCreatedAt = (DateTime)reader["lesson_created_at"]
							};
						}
					}
				}
			}

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
		public IActionResult EditLesson(Models.Lesson model, IFormFile? LessonContentFile, string? LessonLink)
		{
			using (SqlConnection conn = new SqlConnection(_connectionString))
			{
				conn.Open();
				string query = "UPDATE lesson SET lesson_name = @lesson_name, lesson_description = @lesson_description, lesson_content = @lesson_content " +
							   "WHERE lesson_id = @lesson_id";
				using (SqlCommand cmd = new SqlCommand(query, conn))
				{
					cmd.Parameters.AddWithValue("@lesson_name", model.LessonName);
					cmd.Parameters.AddWithValue("@lesson_description", model.LessonDescription);
					cmd.Parameters.AddWithValue("@lesson_content", model.LessonContent);
					cmd.Parameters.AddWithValue("@lesson_id", model.LessonId);

					cmd.ExecuteNonQuery();
				}
			}

			return RedirectToAction("LessonManagement");
		}

		// GET: View Lesson Learner
		[HttpGet]
		public IActionResult ViewLessonLearner(string lessonId)
		{
			string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			string courseId = Request.Cookies["CourseId"];

			if (string.IsNullOrEmpty(courseId) || string.IsNullOrEmpty(userId))
			{
				return BadRequest("CourseId or UserId is missing.");
			}

			// If no lessonId is specified, load the first lesson in the first chapter for the course
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

			// Fetch the specific lesson based on lessonId and courseId
			var lesson = _context.Lessons
				.Include(l => l.Chapter)
				.FirstOrDefault(l => l.LessonId == lessonId && l.Chapter.CourseId == courseId);

			if (lesson == null)
			{
				return NotFound();
			}

			// Check if the lesson is already completed by the user
			bool isCompleted = _context.LessonCompletions.Any(lc => lc.UserId == userId && lc.LessonId == lessonId);

			// Fetch all completed lesson IDs for the current user in this course
			var completedLessonIds = _context.LessonCompletions
				.Where(lc => lc.UserId == userId && lc.Lesson.Chapter.CourseId == courseId)
				.Select(lc => lc.LessonId)
				.ToList();

			// If the request is AJAX (for dynamic lesson loading), return JSON
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

			// Pass all necessary data to the view
			ViewBag.Lessons = _context.Lessons.Where(l => l.Chapter.CourseId == courseId).ToList();
			ViewBag.Chapters = _context.Chapters.Where(c => c.CourseId == courseId).ToList();
			ViewBag.CompletedLessons = completedLessonIds;
			ViewBag.IsCompleted = isCompleted;

			return View(lesson);
		}

		// POST: Mark Lesson Completed
		[HttpPost]
		public JsonResult MarkLessonCompleted([FromBody] LessonCompletionRequest request)
		{
			string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			if (string.IsNullOrEmpty(userId))
			{
				return Json(new { success = false, message = "User not logged in." });
			}

			using (SqlConnection conn = new SqlConnection(_connectionString))
			{
				conn.Open();
				string query = "INSERT INTO lesson_completion (completion_id, user_id, lesson_id, completion_date) " +
							   "VALUES (@completion_id, @user_id, @lesson_id, @completion_date)";
				using (SqlCommand cmd = new SqlCommand(query, conn))
				{
					cmd.Parameters.AddWithValue("@completion_id", Guid.NewGuid().ToString());
					cmd.Parameters.AddWithValue("@user_id", userId);
					cmd.Parameters.AddWithValue("@lesson_id", request.LessonId);
					cmd.Parameters.AddWithValue("@completion_date", DateTime.Now);

					cmd.ExecuteNonQuery();
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
		public class LessonCompletionRequest
		{
			public string LessonId { get; set; }
		}
	}
}
