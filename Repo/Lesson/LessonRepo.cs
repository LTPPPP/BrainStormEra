using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BrainStormEra.Repo
{
    public class LessonRepo
    {
        private readonly SwpMainContext _context;
        private readonly string _connectionString;

        public LessonRepo(SwpMainContext context, IConfiguration configuration)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("SwpMainContext");
        }

        public async Task<List<Lesson>> GetLessonsByChapterIdAsync(string chapterId)
        {
            return await _context.Lessons
                .Where(l => l.ChapterId == chapterId)
                .ToListAsync();
        }

        public async Task<string> GetChapterNameByIdAsync(string chapterId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT chapter_name FROM chapter WHERE chapter_id = @chapter_id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@chapter_id", chapterId);
                    var result = await cmd.ExecuteScalarAsync();
                    return result?.ToString() ?? "Chapter Not Found";
                }
            }
        }

        public async Task<string> GetMaxOrderLessonIdByChapterIdAsync(string chapterId)
        {
            var lesson = await _context.Lessons
                .Where(l => l.ChapterId == chapterId)
                .OrderByDescending(l => l.LessonOrder)
                .FirstOrDefaultAsync();

            return lesson?.LessonId;
        }

        public async Task DeleteLessonsAsync(List<string> lessonIds)
        {
            if (lessonIds == null || !lessonIds.Any()) return;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = $"DELETE FROM lesson WHERE lesson_id IN ({string.Join(",", lessonIds.Select(id => $"'{id}'"))})";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<string> GenerateNewLessonIdAsync()
        {
            int maxId = 0;
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT MAX(CAST(SUBSTRING(lesson_id, 3, LEN(lesson_id)-2) AS INT)) FROM lesson WHERE lesson_id LIKE 'LE%'";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    var result = await cmd.ExecuteScalarAsync();
                    maxId = result != DBNull.Value ? (int)result : 0;
                }
            }
            return "LE" + (maxId + 1).ToString("D3");
        }

        public async Task<List<SelectListItem>> GetChaptersAsync()
        {
            var chapters = new List<SelectListItem>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT chapter_id, chapter_name FROM chapter";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
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
            return chapters;
        }

        public async Task<int> GetNextLessonOrderAsync(string chapterId)
        {
            int lessonOrder = 1;
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT MAX(lesson_order) FROM lesson WHERE chapter_id = @chapter_id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@chapter_id", chapterId);
                    var result = await cmd.ExecuteScalarAsync();
                    lessonOrder = result != DBNull.Value ? ((int)result + 1) : 1;
                }
            }
            return lessonOrder;
        }
        public async Task AddLessonAsync(Lesson model)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
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

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<string> SaveLessonFileAsync(IFormFile file)
        {
            var allowedExtensions = new[] { ".doc", ".docx", ".pdf" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new InvalidOperationException("Only .doc, .docx, and .pdf files are allowed.");
            }

            var filePath = Path.Combine("wwwroot/uploads/lessons", file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return "/uploads/lessons/" + file.FileName;
        }

        public async Task<Lesson> GetLessonByIdAsync(string lessonId)
        {
            Lesson lesson = null;
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT * FROM lesson WHERE lesson_id = @lesson_id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@lesson_id", lessonId);
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            lesson = new Lesson
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
            return lesson;
        }

        public async Task UpdateLessonAsync(Lesson lesson)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "UPDATE lesson SET lesson_name = @lesson_name, lesson_description = @lesson_description, lesson_content = @lesson_content " +
                               "WHERE lesson_id = @lesson_id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@lesson_name", lesson.LessonName);
                    cmd.Parameters.AddWithValue("@lesson_description", lesson.LessonDescription);
                    cmd.Parameters.AddWithValue("@lesson_content", lesson.LessonContent);
                    cmd.Parameters.AddWithValue("@lesson_id", lesson.LessonId);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
        public async Task<Lesson> GetFirstLessonInCourseAsync(string courseId)
        {
            Lesson lesson = null;
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string chapterQuery = "SELECT TOP 1 chapter_id FROM chapter WHERE course_id = @course_id ORDER BY chapter_order";
                string lessonQuery = "SELECT TOP 1 * FROM lesson WHERE chapter_id = @chapter_id ORDER BY lesson_order";

                using (SqlCommand chapterCmd = new SqlCommand(chapterQuery, conn))
                {
                    chapterCmd.Parameters.AddWithValue("@course_id", courseId);
                    var chapterId = await chapterCmd.ExecuteScalarAsync() as string;

                    if (chapterId != null)
                    {
                        using (SqlCommand lessonCmd = new SqlCommand(lessonQuery, conn))
                        {
                            lessonCmd.Parameters.AddWithValue("@chapter_id", chapterId);
                            using (SqlDataReader reader = await lessonCmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    lesson = new Lesson
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
                }
            }
            return lesson;
        }

        public async Task<Lesson> GetLessonByIdAndCourseAsync(string lessonId, string courseId)
        {
            Lesson lesson = null;
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string query = "SELECT * FROM lesson l JOIN chapter c ON l.chapter_id = c.chapter_id WHERE l.lesson_id = @lesson_id AND c.course_id = @course_id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@lesson_id", lessonId);
                    cmd.Parameters.AddWithValue("@course_id", courseId);
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            lesson = new Lesson
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
            return lesson;
        }

        public async Task<bool> IsLessonCompletedAsync(string userId, string lessonId)
        {
            bool isCompleted = false;
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT COUNT(1) FROM lesson_completion WHERE user_id = @user_id AND lesson_id = @lesson_id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@user_id", userId);
                    cmd.Parameters.AddWithValue("@lesson_id", lessonId);
                    isCompleted = (int)await cmd.ExecuteScalarAsync() > 0;
                }
            }
            return isCompleted;
        }


        public async Task<List<string>> GetCompletedLessonIdsAsync(string userId, string courseId)
        {
            var completedLessonIds = new List<string>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT lc.lesson_id FROM lesson_completion lc " +
                               "JOIN lesson l ON lc.lesson_id = l.lesson_id " +
                               "JOIN chapter c ON l.chapter_id = c.chapter_id " +
                               "WHERE lc.user_id = @user_id AND c.course_id = @course_id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@user_id", userId);
                    cmd.Parameters.AddWithValue("@course_id", courseId);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            completedLessonIds.Add(reader["lesson_id"].ToString());
                        }
                    }
                }
            }
            return completedLessonIds;
        }

        public async Task MarkLessonCompletedAsync(string userId, string lessonId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();


                string maxIdQuery = "SELECT MAX(CAST(SUBSTRING(completion_id, 3, LEN(completion_id)-2) AS INT)) FROM lesson_completion WHERE completion_id LIKE 'LC%'";
                int maxId = 0;

                using (SqlCommand cmd = new SqlCommand(maxIdQuery, conn))
                {
                    var result = await cmd.ExecuteScalarAsync();
                    maxId = result != DBNull.Value ? (int)result : 0;
                }


                string newCompletionId = "LC" + (maxId + 1).ToString("D3");


                string insertQuery = "INSERT INTO lesson_completion (completion_id, user_id, lesson_id, completion_date) " +
                                     "VALUES (@completion_id, @user_id, @lesson_id, @completion_date)";

                using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@completion_id", newCompletionId);
                    cmd.Parameters.AddWithValue("@user_id", userId);
                    cmd.Parameters.AddWithValue("@lesson_id", lessonId);
                    cmd.Parameters.AddWithValue("@completion_date", DateTime.Now);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public string FormatYoutubeUrl(string url, int lessonTypeId)
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



        public async Task<string> GetCourseIdByLessonIdAsync(string lessonId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT c.course_id FROM lesson l JOIN chapter ch ON l.chapter_id = ch.chapter_id JOIN course c ON ch.course_id = c.course_id WHERE l.lesson_id = @lessonId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@lessonId", lessonId);
                    return (string)await cmd.ExecuteScalarAsync();
                }
            }
        }


        public async Task<bool> AreAllLessonsCompletedAsync(string userId, string courseId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = @"
            SELECT COUNT(*) 
            FROM lesson l
            JOIN chapter ch ON l.chapter_id = ch.chapter_id
            WHERE ch.course_id = @courseId
            AND l.lesson_id NOT IN (
                SELECT lesson_id FROM lesson_completion WHERE user_id = @userId
            )";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@courseId", courseId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    int remainingLessons = (int)await cmd.ExecuteScalarAsync();
                    return remainingLessons == 0;
                }
            }
        }


        public async Task UpdateEnrollmentStatusAsync(string userId, string courseId, int status)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                // Update enrollment status and certificate issue date
                string updateQuery = @"
            UPDATE enrollment 
            SET enrollment_status = @status, 
                certificate_issued_date = CASE WHEN @status = 5 THEN GETDATE() ELSE certificate_issued_date END
            WHERE user_id = @userId AND course_id = @courseId";

                using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                {
                    updateCmd.Parameters.AddWithValue("@status", status);
                    updateCmd.Parameters.AddWithValue("@userId", userId);
                    updateCmd.Parameters.AddWithValue("@courseId", courseId);
                    await updateCmd.ExecuteNonQueryAsync();
                }

                // Insert notification if the status indicates course completion
                if (status == 5)
                {
                    // Fetch the highest notification ID
                    string getMaxIdQuery = "SELECT MAX(CAST(SUBSTRING(notification_id, 2, LEN(notification_id) - 1) AS INT)) FROM notification";
                    int maxId = 0;

                    using (SqlCommand getMaxIdCmd = new SqlCommand(getMaxIdQuery, conn))
                    {
                        var result = await getMaxIdCmd.ExecuteScalarAsync();
                        if (result != DBNull.Value)
                        {
                            maxId = Convert.ToInt32(result);
                        }
                    }

                    // Increment the ID and format it as "N" followed by the new number with leading zeros
                    string newNotificationId = "N" + (maxId + 1).ToString("D3");

                    // Insert the new notification
                    string notificationQuery = @"
                INSERT INTO notification (notification_id, user_id, course_id, notification_title, notification_content, notification_type, notification_created_at, created_by)
                VALUES (@notificationId, @userId, @courseId, 'Congratulations', 'Congratulations, you have received a new certificate!', 'Info', GETDATE(), @userId)";

                    using (SqlCommand notificationCmd = new SqlCommand(notificationQuery, conn))
                    {
                        notificationCmd.Parameters.AddWithValue("@notificationId", newNotificationId);
                        notificationCmd.Parameters.AddWithValue("@userId", userId);
                        notificationCmd.Parameters.AddWithValue("@courseId", courseId);
                        await notificationCmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }
        public async Task<string> GetChapterIdByLessonIdAsync(string lessonId)
        {
            System.Console.WriteLine(lessonId);
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT chapter_id FROM lesson WHERE lesson_id = @lesson_id";
                System.Console.WriteLine("query : " + query);
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {

                    cmd.Parameters.AddWithValue("@lesson_id", lessonId);
                    var result = await cmd.ExecuteScalarAsync();
                    return result?.ToString();
                }
            }
        }


    }
}
