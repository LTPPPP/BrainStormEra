using BrainStormEra.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BrainStormEra.Repo
{
    public class FeedbackRepo
    {
        private readonly SwpMainContext _context;
        private readonly ILogger<FeedbackRepo> _logger;

        public FeedbackRepo(SwpMainContext context, ILogger<FeedbackRepo> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CreateFeedbackAsync(string courseId, string userId, int starRating, string comment)
        {
            string feedbackId = await GenerateFeedbackIdAsync();

            try
            {
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand(
                        @"INSERT INTO feedback (feedback_id, course_id, user_id, star_rating, comment, feedback_date, feedback_created_at)
                  VALUES (@FeedbackId, @CourseId, @UserId, @StarRating, @Comment, @FeedbackDate, @CreatedAt)", connection);

                    command.Parameters.AddWithValue("@FeedbackId", feedbackId);
                    command.Parameters.AddWithValue("@CourseId", courseId); // Đảm bảo @CourseId được thiết lập
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@StarRating", starRating);
                    command.Parameters.AddWithValue("@Comment", comment);
                    command.Parameters.AddWithValue("@FeedbackDate", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating feedback.");
                throw;
            }
        }

        public async Task<bool> HasUserFeedbackAsync(string courseId, string userId)
        {
            using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(
                    "SELECT COUNT(*) FROM feedback WHERE course_id = @CourseId AND user_id = @UserId AND hidden_status = 0", // hoặc hidden_status = false tùy theo cơ sở dữ liệu
                    connection);

                command.Parameters.AddWithValue("@CourseId", courseId);
                command.Parameters.AddWithValue("@UserId", userId);

                var count = (int)await command.ExecuteScalarAsync();
                return count > 0;
            }
        }


        public async Task<string> GenerateFeedbackIdAsync()
        {
            string newId = "FE001"; // ID mặc định nếu không có feedback nào trong DB
            try
            {
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand("SELECT TOP 1 feedback_id FROM feedback ORDER BY feedback_id DESC", connection);

                    var result = await command.ExecuteScalarAsync() as string;

                    if (result != null)
                    {
                        // Lấy phần số từ ID và tăng thêm 1
                        int currentIdNumber = int.Parse(result.Substring(2));
                        newId = "FE" + (currentIdNumber + 1).ToString("D3"); // Đảm bảo ID mới có 3 chữ số
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating new feedback ID.");
                throw;
            }
            return newId;
        }

        public async Task<bool> CanDeleteFeedbackAsync(string feedbackId, string userId)
        {
            using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(
                    "SELECT COUNT(*) FROM feedback WHERE feedback_id = @FeedbackId AND user_id = @UserId",
                    connection);

                command.Parameters.AddWithValue("@FeedbackId", feedbackId);
                command.Parameters.AddWithValue("@UserId", userId);

                int count = (int)await command.ExecuteScalarAsync();
                return count > 0; // User can delete if they own the feedback
            }
        }

        public async Task<bool> CanEditFeedbackAsync(string feedbackId, string userId)
        {
            return await CanDeleteFeedbackAsync(feedbackId, userId); // Tương tự như kiểm tra quyền xóa
        }


        public async Task DeleteFeedbackAsync(string feedbackId, string user_role)
        {
            using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
            {
                await connection.OpenAsync();
                SqlCommand command;
                if (user_role == "3")
                {
                    command = new SqlCommand("UPDATE feedback SET hidden_status = 1 WHERE feedback_id = @FeedbackId", connection);
                }
                else
                {
                    command = new SqlCommand("DELETE FROM feedback WHERE feedback_id = @FeedbackId", connection);
                }
                command.Parameters.AddWithValue("@FeedbackId", feedbackId);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task UpdateFeedbackAsync(string feedbackId, string newComment, int newStarRating)
        {
            using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
            {

                if (newStarRating < 1 || newStarRating > 5)
                {
                    throw new ArgumentException("Star rating must be between 1 and 5.");
                }
                await connection.OpenAsync();
                var command = new SqlCommand(
                    "UPDATE feedback SET comment = @NewComment, star_rating = @NewStarRating WHERE feedback_id = @FeedbackId",
                    connection);

                command.Parameters.AddWithValue("@FeedbackId", feedbackId);
                command.Parameters.AddWithValue("@NewComment", newComment);
                command.Parameters.AddWithValue("@NewStarRating", newStarRating);

                await command.ExecuteNonQueryAsync();
            }
        }

        public List<Feedback> GetFeedbacksForCourse(string courseId, int page, int pageSize, string userRole)
        {
            string sqlQuery;

            // Kiểm tra nếu là admin (userRole = "1"), bao gồm cả các feedback bị ẩn
            if (userRole == "1")
            {
                sqlQuery = @"
            SELECT feedback_id, course_id, user_id, star_rating, comment, feedback_date, hidden_status, feedback_created_at
            FROM feedback
            WHERE course_id = {0}
            ORDER BY feedback_created_at DESC
            OFFSET {1} ROWS FETCH NEXT {2} ROWS ONLY";
            }
            else
            {
                sqlQuery = @"
            SELECT feedback_id, course_id, user_id, star_rating, comment, feedback_date, hidden_status, feedback_created_at
            FROM feedback
            WHERE course_id = {0} AND hidden_status = 0
            ORDER BY feedback_created_at DESC
            OFFSET {1} ROWS FETCH NEXT {2} ROWS ONLY";
            }

            return _context.Feedbacks
                           .FromSqlRaw(sqlQuery, courseId, (page - 1) * pageSize, pageSize)
                           .ToList();
        }

        public int GetTotalFeedbackCount(string courseId, string userRole)
        {
            string sqlQuery;

            if (userRole == "1") // Nếu là admin, tính tổng tất cả feedbacks
            {
                sqlQuery = @"
            SELECT COUNT(*)
            FROM feedback
            WHERE course_id = {0}";
            }
            else // Nếu không phải admin, chỉ tính feedbacks có hidden_status = 0
            {
                sqlQuery = @"
            SELECT COUNT(*)
            FROM feedback
            WHERE course_id = {0} AND hidden_status = 0";
            }

            return _context.Feedbacks
                           .FromSqlRaw(sqlQuery, courseId)
                           .AsEnumerable()
                           .Count();
        }


    }
}
