using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BrainStormEra.Models;
using Microsoft.Data.SqlClient;

namespace BrainStormEra.Repo.Chapter
{
    public class ChapterRepo
    {
        private readonly string _connectionString;

        public ChapterRepo(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SwpMainContext");
        }

        public async Task<List<Models.Chapter>> GetChaptersByCourseIdAsync(string courseId)
        {
            var chapters = new List<Models.Chapter>();

            using (var connection = new SqlConnection(_connectionString))
            {
                var command = new SqlCommand("SELECT * FROM chapter WHERE course_id = @CourseId ORDER BY chapter_order", connection);
                command.Parameters.AddWithValue("@CourseId", courseId);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var chapter = new Models.Chapter
                        {
                            ChapterId = reader["chapter_id"].ToString(),
                            CourseId = reader["course_id"].ToString(),
                            ChapterName = reader["chapter_name"].ToString(),
                            ChapterDescription = reader["chapter_description"].ToString(),
                            ChapterOrder = Convert.ToInt32(reader["chapter_order"]),
                            ChapterStatus = reader["chapter_status"] as int?,
                            ChapterCreatedAt = Convert.ToDateTime(reader["chapter_created_at"])
                        };
                        chapters.Add(chapter);
                    }
                }
            }
            return chapters;
        }


        public async Task<Models.Chapter> GetChapterByIdAsync(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                throw new ArgumentException("Chapter ID cannot be null or empty.", nameof(chapterId));
            }

            Models.Chapter chapter = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                var command = new SqlCommand("SELECT * FROM chapter WHERE chapter_id = @ChapterId", connection);
                command.Parameters.AddWithValue("@ChapterId", chapterId);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        chapter = new Models.Chapter
                        {
                            ChapterId = reader["chapter_id"].ToString(),
                            CourseId = reader["course_id"].ToString(),
                            ChapterName = reader["chapter_name"].ToString(),
                            ChapterDescription = reader["chapter_description"].ToString(),
                            ChapterOrder = Convert.ToInt32(reader["chapter_order"]),
                            ChapterStatus = reader["chapter_status"] as int?,
                            ChapterCreatedAt = Convert.ToDateTime(reader["chapter_created_at"])
                        };
                    }
                }
            }
            return chapter;
        }


        public async Task<bool> IsChapterNameDuplicateAsync(string chapterName, string chapterId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var command = new SqlCommand("SELECT COUNT(1) FROM chapter WHERE chapter_name = @ChapterName AND chapter_id != @ChapterId", connection);
                command.Parameters.AddWithValue("@ChapterName", chapterName);
                command.Parameters.AddWithValue("@ChapterId", chapterId);

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
        }


        public async Task UpdateChapterAsync(Models.Chapter chapter)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var command = new SqlCommand(
                    "UPDATE chapter SET chapter_name = @ChapterName, chapter_description = @ChapterDescription, " +
                    "chapter_order = @ChapterOrder, chapter_status = @ChapterStatus WHERE chapter_id = @ChapterId",
                    connection);

                command.Parameters.AddWithValue("@ChapterName", chapter.ChapterName);
                command.Parameters.AddWithValue("@ChapterDescription", chapter.ChapterDescription);
                command.Parameters.AddWithValue("@ChapterOrder", chapter.ChapterOrder);
                command.Parameters.AddWithValue("@ChapterStatus", chapter.ChapterStatus ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ChapterId", chapter.ChapterId);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }


        public async Task<List<Models.Chapter>> GetAllChaptersByCourseIdAsync(string courseId)
        {
            var chapters = new List<Models.Chapter>();

            using (var connection = new SqlConnection(_connectionString))
            {
                var command = new SqlCommand("SELECT * FROM chapter WHERE course_id = @CourseId ORDER BY chapter_order", connection);
                command.Parameters.AddWithValue("@CourseId", courseId);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var chapter = new Models.Chapter
                        {
                            ChapterId = reader["chapter_id"].ToString(),
                            CourseId = reader["course_id"].ToString(),
                            ChapterName = reader["chapter_name"].ToString(),
                            ChapterDescription = reader["chapter_description"].ToString(),
                            ChapterOrder = Convert.ToInt32(reader["chapter_order"]),
                            ChapterStatus = reader["chapter_status"] as int?,
                            ChapterCreatedAt = Convert.ToDateTime(reader["chapter_created_at"])
                        };
                        chapters.Add(chapter);
                    }
                }
            }
            return chapters;
        }


        public async Task<Models.Chapter> GetLastChapterInCourseAsync(string courseId)
        {
            Models.Chapter chapter = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                var command = new SqlCommand("SELECT TOP 1 * FROM chapter WHERE course_id = @CourseId ORDER BY chapter_order DESC", connection);
                command.Parameters.AddWithValue("@CourseId", courseId);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        chapter = new Models.Chapter
                        {
                            ChapterId = reader["chapter_id"].ToString(),
                            CourseId = reader["course_id"].ToString(),
                            ChapterName = reader["chapter_name"].ToString(),
                            ChapterDescription = reader["chapter_description"].ToString(),
                            ChapterOrder = Convert.ToInt32(reader["chapter_order"]),
                            ChapterStatus = reader["chapter_status"] as int?,
                            ChapterCreatedAt = Convert.ToDateTime(reader["chapter_created_at"])
                        };
                    }
                }
            }
            return chapter;
        }


        public async Task<string> GenerateNewChapterIdAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var command = new SqlCommand("SELECT TOP 1 chapter_id FROM chapter ORDER BY chapter_id DESC", connection);

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();

                if (result == null)
                {
                    return "CH001";
                }

                var lastId = result.ToString();
                var newIdNumber = int.Parse(lastId.Substring(2)) + 1;
                return "CH" + newIdNumber.ToString("D3");
            }
        }


        public async Task AddChapterAsync(Models.Chapter chapter)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var command = new SqlCommand(
                    "INSERT INTO chapter (chapter_id, course_id, chapter_name, chapter_description, chapter_order, chapter_status, chapter_created_at) " +
                    "VALUES (@ChapterId, @CourseId, @ChapterName, @ChapterDescription, @ChapterOrder, @ChapterStatus, @ChapterCreatedAt)",
                    connection);

                command.Parameters.AddWithValue("@ChapterId", chapter.ChapterId);
                command.Parameters.AddWithValue("@CourseId", chapter.CourseId);
                command.Parameters.AddWithValue("@ChapterName", chapter.ChapterName);
                command.Parameters.AddWithValue("@ChapterDescription", chapter.ChapterDescription ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ChapterOrder", chapter.ChapterOrder);
                command.Parameters.AddWithValue("@ChapterStatus", chapter.ChapterStatus ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ChapterCreatedAt", chapter.ChapterCreatedAt);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }


        public async Task DeleteChaptersAsync(List<string> chapterIds)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var chapterIdsParameter = string.Join(",", chapterIds.Select((id, index) => $"@ChapterId{index}"));
                var commandText = $"DELETE FROM chapter WHERE chapter_id IN ({chapterIdsParameter})";

                using (var command = new SqlCommand(commandText, connection))
                {
                    for (int i = 0; i < chapterIds.Count; i++)
                    {
                        command.Parameters.AddWithValue($"@ChapterId{i}", chapterIds[i]);
                    }

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

    }
}
