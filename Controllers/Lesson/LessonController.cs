using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BrainStormEra.Controllers.Lesson
{
    public class LessonController : Controller
    {
        private readonly string _connectionString;

        public LessonController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SwpMainContext");
        }

        [HttpGet]
        public IActionResult LessonManagement()
        {
            if (Request.Cookies.TryGetValue("ChapterId", out string chapterId))
            {
                List<BrainStormEra.Models.Lesson> lessons = new();
                using (SqlConnection conn = new(_connectionString))
                {
                    conn.Open();
                    string lessonsQuery = "SELECT * FROM lesson WHERE chapter_id = @chapterId";
                    using (SqlCommand cmd = new(lessonsQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@chapterId", chapterId);
                        using SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            lessons.Add(new BrainStormEra.Models.Lesson
                            {
                                LessonId = reader["lesson_id"].ToString(),
                                ChapterId = reader["chapter_id"].ToString(),
                                LessonName = reader["lesson_name"].ToString(),
                                LessonDescription = reader["lesson_description"].ToString(),
                                LessonContent = reader["lesson_content"].ToString(),
                                LessonOrder = Convert.ToInt32(reader["lesson_order"]),
                                LessonTypeId = Convert.ToInt32(reader["lesson_type_id"]),
                                LessonStatus = Convert.ToInt32(reader["lesson_status"]),
                                LessonCreatedAt = Convert.ToDateTime(reader["lesson_created_at"])
                            });
                        }
                    }

                    string chapterQuery = "SELECT chapter_name FROM chapter WHERE chapter_id = @chapterId";
                    using (SqlCommand cmd = new(chapterQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@chapterId", chapterId);
                        ViewBag.ChapterName = cmd.ExecuteScalar()?.ToString() ?? "Chapter Not Found";
                    }
                }

                return View("LessonManagement", lessons);
            }
            else
            {
                return BadRequest("Chapter ID is missing.");
            }
        }

        [HttpGet]
        public IActionResult DeleteLesson()
        {
            if (!Request.Cookies.TryGetValue("ChapterId", out string chapterId))
            {
                return RedirectToAction("LessonManagement");
            }

            List<BrainStormEra.Models.Lesson> lessons = new();
            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();
                string lessonsQuery = "SELECT * FROM lesson WHERE chapter_id = @chapterId ORDER BY lesson_order";
                using (SqlCommand cmd = new(lessonsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@chapterId", chapterId);
                    using SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        lessons.Add(new BrainStormEra.Models.Lesson
                        {
                            LessonId = reader["lesson_id"].ToString(),
                            ChapterId = reader["chapter_id"].ToString(),
                            LessonName = reader["lesson_name"].ToString(),
                            LessonDescription = reader["lesson_description"].ToString(),
                            LessonContent = reader["lesson_content"].ToString(),
                            LessonOrder = Convert.ToInt32(reader["lesson_order"]),
                            LessonTypeId = Convert.ToInt32(reader["lesson_type_id"]),
                            LessonStatus = Convert.ToInt32(reader["lesson_status"]),
                            LessonCreatedAt = Convert.ToDateTime(reader["lesson_created_at"])
                        });
                    }
                }
            }

            ViewBag.MaxOrderLessonId = lessons.OrderByDescending(l => l.LessonOrder).FirstOrDefault()?.LessonId;
            return View(lessons);
        }

        [HttpPost, ActionName("DeleteLesson")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteLesson(List<string> LessonIds)
        {
            if (LessonIds != null && LessonIds.Any())
            {
                using (SqlConnection conn = new(_connectionString))
                {
                    conn.Open();
                    var ids = string.Join(", ", LessonIds.Select(id => $"'{id}'"));
                    string deleteQuery = $"DELETE FROM lesson WHERE lesson_id IN ({ids})";
                    using SqlCommand cmd = new(deleteQuery, conn);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("DeleteLesson");
        }

        [HttpGet]
        public IActionResult AddLesson()
        {
            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();
                List<SelectListItem> chapters = new();
                string chaptersQuery = "SELECT chapter_id, chapter_name FROM chapter";
                using (SqlCommand cmd = new(chaptersQuery, conn))
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
                ViewBag.Chapters = chapters;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddLesson(AddLessonViewModel viewModel)
        {
            var chapterID = Request.Cookies["ChapterId"];
            viewModel.Lesson ??= new BrainStormEra.Models.Lesson();
            viewModel.Lesson.LessonStatus = 0;
            viewModel.Lesson.ChapterId = chapterID;

            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();

                // Determine Lesson Order
                string maxOrderQuery = @"SELECT ISNULL(MAX(lesson_order), 0) + 1 FROM lesson WHERE chapter_id = @chapterId";
                using (SqlCommand cmd = new(maxOrderQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@chapterId", chapterID);
                    viewModel.Lesson.LessonOrder = (int)cmd.ExecuteScalar();
                }

                // Determine new Lesson ID
                string maxIdQuery = "SELECT MAX(lesson_id) FROM lesson WHERE lesson_id LIKE 'LE%'";
                using (SqlCommand cmd = new(maxIdQuery, conn))
                {
                    object result = cmd.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                    {
                        string maxId = result.ToString();
                        int currentNumber = int.Parse(maxId.Substring(2));
                        int newNumber = currentNumber + 1;
                        viewModel.Lesson.LessonId = "LE" + newNumber.ToString("D3");
                    }
                    else
                    {
                        viewModel.Lesson.LessonId = "LE001";
                    }
                }
            }

            // Set Lesson Content based on type
            if (viewModel.Lesson.LessonTypeId == 1)
            {
                if (string.IsNullOrEmpty(viewModel.LessonLink))
                {
                    ModelState.AddModelError("LessonLink", "Please enter a YouTube link for the video lesson.");
                }
                else
                {
                    viewModel.Lesson.LessonContent = viewModel.LessonLink;
                }
            }
            else if (viewModel.Lesson.LessonTypeId == 2)
            {
                if (viewModel.LessonContentFile == null || viewModel.LessonContentFile.Length == 0)
                {
                    ModelState.AddModelError("LessonContentFile", "Please upload a file for the reading lesson.");
                }
                else
                {
                    var fileName = Path.GetFileName(viewModel.LessonContentFile.FileName);
                    var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "document", uniqueFileName);

                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        viewModel.LessonContentFile.CopyTo(stream);
                    }

                    viewModel.Lesson.LessonContent = "/lib/document/" + uniqueFileName;
                }
            }
            else
            {
                ModelState.AddModelError("Lesson.LessonTypeId", "Invalid lesson type.");
            }

            if (!ModelState.IsValid)
            {
                RepopulateChapters();
                return View(viewModel);
            }

            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();
                using var transaction = conn.BeginTransaction();

                try
                {
                    string insertQuery = @"
                    INSERT INTO lesson (
                        lesson_id, chapter_id, lesson_name, lesson_description, 
                        lesson_content, lesson_order, lesson_type_id, 
                        lesson_status, lesson_created_at
                    ) 
                    VALUES (
                        @lessonId, @chapterId, @lessonName, @lessonDescription,
                        @lessonContent, @lessonOrder, @lessonTypeId,
                        @lessonStatus, @lessonCreatedAt
                    )";
                    using SqlCommand cmd = new(insertQuery, conn, transaction);
                    cmd.Parameters.AddWithValue("@lessonId", viewModel.Lesson.LessonId);
                    cmd.Parameters.AddWithValue("@chapterId", viewModel.Lesson.ChapterId);
                    cmd.Parameters.AddWithValue("@lessonName", viewModel.Lesson.LessonName);
                    cmd.Parameters.AddWithValue("@lessonDescription", viewModel.Lesson.LessonDescription);
                    cmd.Parameters.AddWithValue("@lessonContent", viewModel.Lesson.LessonContent);
                    cmd.Parameters.AddWithValue("@lessonOrder", viewModel.Lesson.LessonOrder);
                    cmd.Parameters.AddWithValue("@lessonTypeId", viewModel.Lesson.LessonTypeId);
                    cmd.Parameters.AddWithValue("@lessonStatus", viewModel.Lesson.LessonStatus);
                    cmd.Parameters.AddWithValue("@lessonCreatedAt", DateTime.Now);

                    cmd.ExecuteNonQuery();
                    transaction.Commit();

                    TempData["SuccessMessage"] = "Lesson added successfully!";
                    return RedirectToAction("LessonManagement");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", $"Error saving lesson: {ex.Message}");
                    RepopulateChapters();
                    return View(viewModel);
                }
            }
        }

        [HttpGet]
        public IActionResult EditLesson()
        {
            var lessonId = HttpContext.Request.Cookies["LessonId"];
            if (string.IsNullOrEmpty(lessonId))
            {
                return RedirectToAction("LessonManagement");
            }

            BrainStormEra.Models.Lesson lesson = null;
            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();
                string lessonQuery = "SELECT * FROM lesson WHERE lesson_id = @lessonId";
                using SqlCommand cmd = new(lessonQuery, conn);
                cmd.Parameters.AddWithValue("@lessonId", lessonId);
                using SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    lesson = new BrainStormEra.Models.Lesson
                    {
                        LessonId = reader["lesson_id"].ToString(),
                        LessonName = reader["lesson_name"].ToString(),
                        LessonDescription = reader["lesson_description"].ToString(),
                        LessonContent = reader["lesson_content"].ToString(),
                        LessonTypeId = Convert.ToInt32(reader["lesson_type_id"])
                    };
                }
            }

            if (lesson == null)
            {
                return NotFound();
            }

            ViewBag.ExistingFilePath = lesson.LessonContent;
            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditLesson(BrainStormEra.Models.Lesson model, IFormFile? LessonContentFile, string? LessonLink)
        {
            BrainStormEra.Models.Lesson existingLesson = GetExistingLesson(model.LessonId);
            if (existingLesson == null)
            {
                return NotFound();
            }

            model.ChapterId ??= existingLesson.ChapterId;
            model.LessonStatus = existingLesson.LessonStatus;

            if (model.LessonTypeId == 1)
            {
                if (string.IsNullOrEmpty(LessonLink))
                {
                    ModelState.AddModelError("LessonLink", "Please enter a YouTube link for the video lesson.");
                }
                else
                {
                    model.LessonContent = LessonLink;
                }
            }
            else if (model.LessonTypeId == 2)
            {
                if (LessonContentFile != null && LessonContentFile.Length > 0)
                {
                    var fileName = Path.GetFileName(LessonContentFile.FileName);
                    var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "document", uniqueFileName);

                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        LessonContentFile.CopyTo(stream);
                    }

                    model.LessonContent = "/lib/document/" + uniqueFileName;
                }
                else
                {
                    model.LessonContent = existingLesson.LessonContent;
                }
            }
            else
            {
                ModelState.AddModelError("LessonTypeId", "Invalid lesson type.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            UpdateLessonInDatabase(model);
            return RedirectToAction("LessonManagement");
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
            List<BrainStormEra.Models.Chapter> courseChapters = GetChaptersForCourse(courseId);
            List<BrainStormEra.Models.Lesson> chapterLessons = GetLessonsForChapters(courseId);

            BrainStormEra.Models.Lesson lesson = FetchLessonData(lessonId, courseId);
            if (lesson == null) return NotFound();

            ViewBag.CompletedLessons = GetCompletedLessons(userId, courseId);
            ViewBag.IsCompleted = CheckLessonCompletion(userId, lessonId);

            return View(lesson);
        }

        [HttpPost]
        public JsonResult MarkLessonCompleted([FromBody] LessonCompletionRequest request)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not logged in." });
            }

            if (!CheckLessonCompletion(userId, request.LessonId))
            {
                InsertLessonCompletion(userId, request.LessonId);
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Lesson already marked as completed." });
        }

        public class LessonCompletionRequest
        {
            public string LessonId { get; set; }
        }

        private void RepopulateChapters()
        {
            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();
                List<SelectListItem> chapters = new();
                string chaptersQuery = "SELECT chapter_id, chapter_name FROM chapter";
                using (SqlCommand cmd = new(chaptersQuery, conn))
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
                ViewBag.Chapters = chapters;
            }
        }

        private BrainStormEra.Models.Lesson GetExistingLesson(string lessonId)
        {
            BrainStormEra.Models.Lesson existingLesson = null;
            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();
                string lessonQuery = "SELECT * FROM lesson WHERE lesson_id = @lessonId";
                using SqlCommand cmd = new(lessonQuery, conn);
                cmd.Parameters.AddWithValue("@lessonId", lessonId);
                using SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    existingLesson = new BrainStormEra.Models.Lesson
                    {
                        LessonId = reader["lesson_id"].ToString(),
                        ChapterId = reader["chapter_id"].ToString(),
                        LessonStatus = Convert.ToInt32(reader["lesson_status"]),
                        LessonContent = reader["lesson_content"].ToString()
                    };
                }
            }
            return existingLesson;
        }

        private void UpdateLessonInDatabase(BrainStormEra.Models.Lesson model)
        {
            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();
                string updateQuery = "UPDATE lesson SET lesson_name = @lessonName, lesson_description = @lessonDescription, lesson_content = @lessonContent, lesson_type_id = @lessonTypeId WHERE lesson_id = @lessonId";
                using SqlCommand cmd = new(updateQuery, conn);
                cmd.Parameters.AddWithValue("@lessonName", model.LessonName);
                cmd.Parameters.AddWithValue("@lessonDescription", model.LessonDescription);
                cmd.Parameters.AddWithValue("@lessonContent", model.LessonContent);
                cmd.Parameters.AddWithValue("@lessonTypeId", model.LessonTypeId);
                cmd.Parameters.AddWithValue("@lessonId", model.LessonId);
                cmd.ExecuteNonQuery();
            }
        }

        private List<BrainStormEra.Models.Chapter> GetChaptersForCourse(string courseId)
        {
            List<BrainStormEra.Models.Chapter> chapters = new();
            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();
                string chaptersQuery = "SELECT * FROM chapter WHERE course_id = @courseId";
                using (SqlCommand cmd = new(chaptersQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@courseId", courseId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            chapters.Add(new BrainStormEra.Models.Chapter
                            {
                                ChapterId = reader["chapter_id"].ToString(),
                                ChapterName = reader["chapter_name"].ToString()
                            });
                        }
                    }
                }
            }
            return chapters;
        }


        private List<BrainStormEra.Models.Lesson> GetLessonsForChapters(string courseId)
        {
            List<BrainStormEra.Models.Lesson> lessons = new();
            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();
                string lessonsQuery = "SELECT * FROM lesson WHERE chapter_id IN (SELECT chapter_id FROM chapter WHERE course_id = @courseId)";
                using (SqlCommand cmd = new(lessonsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@courseId", courseId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lessons.Add(new BrainStormEra.Models.Lesson
                            {
                                LessonId = reader["lesson_id"].ToString(),
                                ChapterId = reader["chapter_id"].ToString(),
                                LessonName = reader["lesson_name"].ToString(),
                                LessonTypeId = Convert.ToInt32(reader["lesson_type_id"])
                            });
                        }
                    }
                }
            }
            return lessons;
        }


        private BrainStormEra.Models.Lesson FetchLessonData(string lessonId, string courseId)
        {
            // Check if lessonId or courseId is null or empty
            if (string.IsNullOrEmpty(courseId))
            {
                throw new ArgumentException("lessonId and courseId must be provided.");
            }

            BrainStormEra.Models.Lesson lesson = null;
            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();
                string lessonQuery = "SELECT * FROM lesson WHERE lesson_id = @lessonId AND chapter_id IN (SELECT chapter_id FROM chapter WHERE course_id = @courseId)";

                using (SqlCommand cmd = new(lessonQuery, conn))
                {
                    // Adding parameters to avoid missing parameter issue
                    cmd.Parameters.AddWithValue("@lessonId", lessonId);
                    cmd.Parameters.AddWithValue("@courseId", courseId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            lesson = new BrainStormEra.Models.Lesson
                            {
                                LessonId = reader["lesson_id"].ToString(),
                                LessonName = reader["lesson_name"].ToString(),
                                LessonDescription = reader["lesson_description"].ToString(),
                                LessonContent = reader["lesson_content"].ToString(),
                                LessonTypeId = Convert.ToInt32(reader["lesson_type_id"])
                            };
                        }
                    }
                }
            }
            return lesson;
        }


        private List<string> GetCompletedLessons(string userId, string courseId)
        {
            List<string> completedLessonIds = new();
            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();
                string completedLessonsQuery = @"
                    SELECT lc.lesson_id 
                    FROM lesson_completion lc 
                    JOIN lesson l ON lc.lesson_id = l.lesson_id 
                    JOIN chapter c ON l.chapter_id = c.chapter_id 
                    WHERE lc.user_id = @userId AND c.course_id = @courseId";
                using (SqlCommand cmd = new(completedLessonsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@courseId", courseId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            completedLessonIds.Add(reader["lesson_id"].ToString());
                        }
                    }
                }
            }
            return completedLessonIds;
        }

        private bool CheckLessonCompletion(string userId, string lessonId)
        {
            bool isCompleted = false;
            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();
                string completionQuery = "SELECT COUNT(1) FROM lesson_completion WHERE user_id = @userId AND lesson_id = @lessonId";
                using (SqlCommand cmdCompletion = new(completionQuery, conn))
                {
                    cmdCompletion.Parameters.AddWithValue("@userId", userId);
                    cmdCompletion.Parameters.AddWithValue("@lessonId", lessonId);
                    isCompleted = (int)cmdCompletion.ExecuteScalar() > 0;
                }
            }
            return isCompleted;
        }

        private void InsertLessonCompletion(string userId, string lessonId)
        {
            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();
                string maxCompletionIdQuery = "SELECT ISNULL(MAX(CAST(SUBSTRING(completion_id, 3, LEN(completion_id) - 2) AS INT)), 0) + 1 FROM lesson_completion WHERE completion_id LIKE 'LC%'";
                string newCompletionId;

                using (SqlCommand cmd = new(maxCompletionIdQuery, conn))
                {
                    int maxId = (int)cmd.ExecuteScalar();
                    newCompletionId = "LC" + maxId.ToString("D3");
                }

                string insertCompletionQuery = "INSERT INTO lesson_completion (completion_id, user_id, lesson_id, completion_date) VALUES (@completionId, @userId, @lessonId, @completionDate)";
                using (SqlCommand cmd = new(insertCompletionQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@completionId", newCompletionId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@lessonId", lessonId);
                    cmd.Parameters.AddWithValue("@completionDate", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
