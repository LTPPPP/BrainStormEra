using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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

            var maxOrderLessonId = lessons.OrderByDescending(l => l.LessonOrder).FirstOrDefault()?.LessonId;
            ViewBag.MaxOrderLessonId = maxOrderLessonId;

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
            // Tìm LessonId lớn nhất và tạo LessonId mới
            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();

                string maxOrderQuery = @"
            SELECT ISNULL(MAX(lesson_order), 0) + 1
            FROM lesson
            WHERE chapter_id = @chapterId";

                using (SqlCommand cmd = new(maxOrderQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@chapterId", chapterID);
                    viewModel.Lesson.LessonOrder = (int)cmd.ExecuteScalar(); // Set the lesson order to max + 1
                }

                string maxIdQuery = "SELECT MAX(lesson_id) FROM lesson WHERE lesson_id LIKE 'LE%'";
                using (SqlCommand cmd = new(maxIdQuery, conn))
                {
                    object result = cmd.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                    {
                        // Lấy số từ LessonId hiện tại
                        string maxId = result.ToString();
                        int currentNumber = int.Parse(maxId.Substring(2)); // Bỏ "LE" và chỉ lấy phần số
                        int newNumber = currentNumber + 1;
                        viewModel.Lesson.LessonId = "LE" + newNumber.ToString("D3");
                    }
                    else
                    {
                        viewModel.Lesson.LessonId = "LE001";
                    }
                }
            }
            if (viewModel.Lesson.LessonTypeId == 1) // Video
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
            else if (viewModel.Lesson.LessonTypeId == 2) // Reading
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

                    // Ensure the directory exists
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

            if (ModelState.IsValid)
            {
                // Re-populate the chapters in case of an error
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

                return View(viewModel);
            }

            // Lưu lesson vào cơ sở dữ liệu
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
                    Console.WriteLine("id : "+viewModel.Lesson.LessonId);
                    Console.WriteLine("chapterid : "+viewModel.Lesson.ChapterId);
                    Console.WriteLine("lesson name : "+viewModel.Lesson.LessonName);
                    Console.WriteLine("des : "+viewModel.Lesson.LessonDescription);
                    Console.WriteLine("content : "+viewModel.Lesson.LessonContent);
                    Console.WriteLine("order : "+viewModel.Lesson.LessonOrder);
                    Console.WriteLine("type id : "+viewModel.Lesson.LessonTypeId);
                    Console.WriteLine("status : "+viewModel.Lesson.LessonStatus);
                    Console.WriteLine("date : "+DateTime.Now);

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

                    // Re-populate the chapters in case of an error
                    using (SqlConnection conn2 = new(_connectionString))
                    {
                        conn2.Open();
                        List<SelectListItem> chapters = new();
                        string chaptersQuery = "SELECT chapter_id, chapter_name FROM chapter";
                        using (SqlCommand cmd = new(chaptersQuery, conn2))
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
            BrainStormEra.Models.Lesson existingLesson = null;
            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();
                string lessonQuery = "SELECT * FROM lesson WHERE lesson_id = @lessonId";
                using SqlCommand cmd = new(lessonQuery, conn);
                cmd.Parameters.AddWithValue("@lessonId", model.LessonId);
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

            if (existingLesson == null)
            {
                return NotFound();
            }

            model.ChapterId ??= existingLesson.ChapterId;
            model.LessonStatus = existingLesson.LessonStatus;

            // Handle LessonContent based on LessonType
            if (model.LessonTypeId == 1) // Video
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
            else if (model.LessonTypeId == 2) // Reading
            {
                if (LessonContentFile != null && LessonContentFile.Length > 0)
                {
                    var fileName = Path.GetFileName(LessonContentFile.FileName);
                    var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "document", uniqueFileName);

                    // Ensure the directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        LessonContentFile.CopyTo(stream);
                    }

                    model.LessonContent = "/lib/document/" + uniqueFileName;
                }
                else
                {
                    model.LessonContent = existingLesson.LessonContent; // Retain existing content if no new file is uploaded
                }
            }
            else
            {
                ModelState.AddModelError("LessonTypeId", "Invalid lesson type.");
            }

            if (ModelState.IsValid)
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
                return RedirectToAction("LessonManagement");
            }

            return View(model);
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

            if (string.IsNullOrEmpty(lessonId))
            {
                using (SqlConnection conn = new(_connectionString))
                {
                    conn.Open();
                    string firstLessonQuery = "SELECT TOP 1 l.lesson_id FROM lesson l JOIN chapter c ON l.chapter_id = c.chapter_id WHERE c.course_id = @courseId ORDER BY c.chapter_order, l.lesson_order";
                    using SqlCommand cmd = new(firstLessonQuery, conn);
                    cmd.Parameters.AddWithValue("@courseId", courseId);
                    lessonId = cmd.ExecuteScalar()?.ToString();
                }
            }

            BrainStormEra.Models.Lesson lesson = null;
            bool isCompleted = false;
            List<string> completedLessonIds = new();

            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();
                string lessonQuery = "SELECT * FROM lesson WHERE lesson_id = @lessonId AND chapter_id IN (SELECT chapter_id FROM chapter WHERE course_id = @courseId)";
                using SqlCommand cmd = new(lessonQuery, conn);
                cmd.Parameters.AddWithValue("@lessonId", lessonId);
                cmd.Parameters.AddWithValue("@courseId", courseId);
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

                string completionQuery = "SELECT COUNT(1) FROM lesson_completion WHERE user_id = @userId AND lesson_id = @lessonId";
                using (SqlCommand cmdCompletion = new(completionQuery, conn))
                {
                    cmdCompletion.Parameters.AddWithValue("@userId", userId);
                    cmdCompletion.Parameters.AddWithValue("@lessonId", lessonId);
                    isCompleted = (int)cmdCompletion.ExecuteScalar() > 0;
                }

                string completedLessonsQuery = "SELECT lesson_id FROM lesson_completion lc JOIN lesson l ON lc.lesson_id = l.lesson_id JOIN chapter c ON l.chapter_id = c.chapter_id WHERE lc.user_id = @userId AND c.course_id = @courseId";
                using (SqlCommand cmdCompleted = new(completedLessonsQuery, conn))
                {
                    cmdCompleted.Parameters.AddWithValue("@userId", userId);
                    cmdCompleted.Parameters.AddWithValue("@courseId", courseId);
                    using SqlDataReader readerCompleted = cmdCompleted.ExecuteReader();
                    while (readerCompleted.Read())
                    {
                        completedLessonIds.Add(readerCompleted["lesson_id"].ToString());
                    }
                }
            }

            if (lesson == null) return NotFound();

            ViewBag.CompletedLessons = completedLessonIds;
            ViewBag.IsCompleted = isCompleted;

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

            bool alreadyCompleted;
            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();
                string checkCompletionQuery = "SELECT COUNT(1) FROM lesson_completion WHERE user_id = @userId AND lesson_id = @lessonId";
                using (SqlCommand cmd = new(checkCompletionQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@lessonId", request.LessonId);
                    alreadyCompleted = (int)cmd.ExecuteScalar() > 0;
                }
            }

            if (!alreadyCompleted)
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
                        cmd.Parameters.AddWithValue("@lessonId", request.LessonId);
                        cmd.Parameters.AddWithValue("@completionDate", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Lesson already marked as completed." });
        }

        public class LessonCompletionRequest
        {
            public string LessonId { get; set; }
        }
    }
}
