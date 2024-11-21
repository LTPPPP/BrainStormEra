using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using BrainStormEra.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using BrainStormEra.Views.Course;
namespace BrainStormEra.Repo.Course
{
    public class CourseRepo
    {
        private readonly SwpMainContext _context;
        private readonly string _connectionString;

        public CourseRepo(SwpMainContext context, IConfiguration configuration)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("SwpMainContext");
        }

        public async Task<Models.Course> GetCourseByIdAsync(string courseId)
        {
            Models.Course course = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM course WHERE course_id = @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    course = new Models.Course
                    {
                        CourseId = reader["course_id"].ToString(),
                        CourseName = reader["course_name"].ToString(),
                        CourseDescription = reader["course_description"].ToString(),
                        CoursePicture = reader["course_picture"]?.ToString(),
                        Price = reader["price"] != DBNull.Value ? Convert.ToDecimal(reader["price"]) : 0,
                        CourseStatus = Convert.ToInt32(reader["course_status"]),
                        CourseCreatedAt = Convert.ToDateTime(reader["course_created_at"])
                    };
                }
            }
            return course;
        }

        public async Task<List<CourseCategory>> GetCourseCategoriesAsync()
        {
            var categories = new List<CourseCategory>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM course_category";
                var command = new SqlCommand(query, connection);
                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        categories.Add(new CourseCategory
                        {
                            CourseCategoryId = reader["course_category_id"].ToString(),
                            CourseCategoryName = reader["course_category_name"].ToString()
                        });
                    }
                }
            }
            return categories;
        }

        // Tạo CourseId mới
        public async Task<string> GenerateNewCourseIdAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT TOP 1 course_id FROM course ORDER BY course_id DESC";
                var command = new SqlCommand(query, connection);
                await connection.OpenAsync();
                var lastCourseId = await command.ExecuteScalarAsync() as string;
                return lastCourseId == null ? "CO001" : "CO" + (int.Parse(lastCourseId.Substring(2)) + 1).ToString("D3");
            }
        }

        // Kiểm tra tên course có tồn tại hay không
        public async Task<bool> IsCourseNameExistsAsync(string courseName, string courseId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM course WHERE course_name = @CourseName AND course_id != @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseName", courseName);
                command.Parameters.AddWithValue("@CourseId", courseId);
                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    return reader.HasRows;
                }
            }
        }

        // Lưu course mới vào database
        public async Task AddCourseAsync(Models.Course course)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "INSERT INTO course (course_id, course_name, course_description, course_status, created_by, course_picture, price) " +
                            "VALUES (@CourseId, @CourseName, @CourseDescription, @CourseStatus, @CreatedBy, @CoursePicture, @Price)";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", course.CourseId);
                command.Parameters.AddWithValue("@CourseName", course.CourseName);
                command.Parameters.AddWithValue("@CourseDescription", course.CourseDescription ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CourseStatus", course.CourseStatus);
                command.Parameters.AddWithValue("@CreatedBy", course.CreatedBy);
                command.Parameters.AddWithValue("@CoursePicture", course.CoursePicture ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Price", course.Price);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }

        // Thêm category cho course
        public async Task AddCourseCategoriesAsync(string courseId, List<string> categoryIds)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                foreach (var categoryId in categoryIds)
                {
                    var query = "INSERT INTO course_category_mapping (course_id, course_category_id) VALUES (@CourseId, @CategoryId)";
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@CourseId", courseId);
                    command.Parameters.AddWithValue("@CategoryId", categoryId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task UpdateCourseAsync(EditCourseViewModel viewModel, string coursePicturePath)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "UPDATE course SET course_name = @CourseName, course_description = @CourseDescription, price = @Price, course_picture = @CoursePicture WHERE course_id = @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", viewModel.CourseId);
                command.Parameters.AddWithValue("@CourseName", viewModel.CourseName);
                command.Parameters.AddWithValue("@CourseDescription", viewModel.CourseDescription ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Price", viewModel.Price);
                command.Parameters.AddWithValue("@CoursePicture", coursePicturePath ?? (object)DBNull.Value);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task UpdateCourseCategoriesAsync(string courseId, List<string> categoryIds)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var deleteQuery = "DELETE FROM course_category_mapping WHERE course_id = @CourseId";
                var deleteCommand = new SqlCommand(deleteQuery, connection);
                deleteCommand.Parameters.AddWithValue("@CourseId", courseId);
                await deleteCommand.ExecuteNonQueryAsync();

                foreach (var categoryId in categoryIds)
                {
                    var insertQuery = "INSERT INTO course_category_mapping (course_id, course_category_id) VALUES (@CourseId, @CategoryId)";
                    var insertCommand = new SqlCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@CourseId", courseId);
                    insertCommand.Parameters.AddWithValue("@CategoryId", categoryId);
                    await insertCommand.ExecuteNonQueryAsync();
                }
            }
        }
        public async Task<List<CourseCategory>> GetCourseCategoriesByCourseIdAsync(string courseId)
        {
            var categories = new List<CourseCategory>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT cc.* FROM course_category cc INNER JOIN course_category_mapping ccm ON cc.course_category_id = ccm.course_category_id WHERE ccm.course_id = @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    categories.Add(new CourseCategory
                    {
                        CourseCategoryId = reader["course_category_id"].ToString(),
                        CourseCategoryName = reader["course_category_name"].ToString()
                    });
                }
            }
            return categories;
        }


        public async Task<List<Models.Course>> GetInstructorCoursesAsync(string userId)
        {
            var courses = new List<Models.Course>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM course WHERE created_by = @UserId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    courses.Add(new Models.Course
                    {
                        CourseId = reader["course_id"].ToString(),
                        CourseName = reader["course_name"].ToString(),
                        CourseDescription = reader["course_description"].ToString(),
                        CourseStatus = Convert.ToInt32(reader["course_status"]),
                        CoursePicture = reader["course_picture"]?.ToString(),
                        Price = reader["price"] != DBNull.Value ? Convert.ToDecimal(reader["price"]) : 0,
                        CourseCreatedAt = Convert.ToDateTime(reader["course_created_at"])
                    });
                }
            }
            return courses;
        }

        public async Task<List<Models.Course>> GetAllActiveCoursesAsync()
        {
            var courses = new List<Models.Course>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM course WHERE course_status = 2 ORDER BY course_created_at DESC";
                var command = new SqlCommand(query, connection);
                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    courses.Add(new Models.Course
                    {
                        CourseId = reader["course_id"].ToString(),
                        CourseName = reader["course_name"].ToString(),
                        CourseDescription = reader["course_description"].ToString(),
                        CourseStatus = Convert.ToInt32(reader["course_status"]),
                        CoursePicture = reader["course_picture"]?.ToString(),
                        Price = reader["price"] != DBNull.Value ? Convert.ToDecimal(reader["price"]) : 0,
                        CourseCreatedAt = Convert.ToDateTime(reader["course_created_at"])
                    });
                }
            }
            return courses;
        }

        public async Task<bool> DeleteCourseAsync(string courseId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "DELETE FROM course WHERE course_id = @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                await connection.OpenAsync();
                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }
        public async Task<string> GetCourseCreatorNameAsync(string courseId)
        {
            string createdByName = "Unknown";
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"
            SELECT a.full_name 
            FROM account a 
            INNER JOIN course c ON a.user_id = c.created_by 
            WHERE c.course_id = @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                if (result != null)
                {
                    createdByName = result.ToString();
                }
            }
            return createdByName;
        }

        public async Task<(bool IsEnrolled, bool IsBanned)> CheckEnrollmentStatusAsync(string userId, string courseId)
        {
            bool isEnrolled = false;
            bool isBanned = false;
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT approved FROM enrollment WHERE user_id = @UserId AND course_id = @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@CourseId", courseId);
                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                if (result != null)
                {
                    isEnrolled = true;
                    isBanned = !(bool)result;
                }
            }
            return (isEnrolled, isBanned);
        }

        public async Task<List<string>> GetCourseCategoriesAsync(string courseId)
        {
            var categories = new List<string>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT cc.course_category_name FROM course_category cc " +
                            "JOIN course_category_mapping ccm ON cc.course_category_id = ccm.course_category_id " +
                            "WHERE ccm.course_id = @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    categories.Add(reader["course_category_name"].ToString());
                }
            }
            return categories;
        }

        public async Task<int> GetLearnersCountAsync(string courseId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT COUNT(*) FROM enrollment WHERE course_id = @CourseId AND approved = 1";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                await connection.OpenAsync();
                return (int)await command.ExecuteScalarAsync();
            }
        }

        public async Task<List<Feedback>> GetFeedbacksAsync(string courseId, string userRole, int offset, int pageSize)
        {
            var feedbacks = new List<Feedback>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "";
                if (userRole == "3")
                {
                    query = "SELECT f.*, a.full_name, a.user_picture, f.hidden_status FROM feedback f " +
                            "JOIN account a ON f.user_id = a.user_id " +
                            "WHERE f.course_id = @CourseId AND f.hidden_status = 0 " +
                            "ORDER BY f.feedback_date DESC " +
                            "OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                }
                else
                {
                    query = "SELECT f.*, a.full_name, a.user_picture, f.hidden_status FROM feedback f " +
                            "JOIN account a ON f.user_id = a.user_id " +
                            "WHERE f.course_id = @CourseId " +
                            "ORDER BY f.feedback_date DESC " +
                            "OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                }
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                command.Parameters.AddWithValue("@Offset", offset);
                command.Parameters.AddWithValue("@PageSize", pageSize);
                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    feedbacks.Add(new Feedback
                    {
                        FeedbackId = reader["feedback_id"].ToString(),
                        CourseId = reader["course_id"].ToString(),
                        UserId = reader["user_id"].ToString(),
                        StarRating = reader["star_rating"] != DBNull.Value ? (byte?)Convert.ToByte(reader["star_rating"]) : null,
                        Comment = reader["comment"].ToString(),
                        FeedbackDate = reader["feedback_date"] != DBNull.Value
                            ? DateOnly.FromDateTime(Convert.ToDateTime(reader["feedback_date"]))
                            : DateOnly.MinValue,
                        User = new Account
                        {
                            FullName = reader["full_name"].ToString(),
                            UserPicture = reader["user_picture"]?.ToString()
                        },
                        HiddenStatus = reader["hidden_status"] != DBNull.Value ? Convert.ToBoolean(reader["hidden_status"]) : false
                    });
                }
            }
            return feedbacks;
        }


        public async Task<int> GetTotalFeedbackCountAsync(string courseId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT COUNT(*) FROM feedback WHERE course_id = @CourseId AND hidden_status = 0";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                await connection.OpenAsync();
                return (int)await command.ExecuteScalarAsync();
            }
        }

        public async Task<double> GetAverageRatingAsync(string courseId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT AVG(CAST(star_rating AS FLOAT)) FROM feedback WHERE course_id = @CourseId AND hidden_status = 0";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return result != DBNull.Value ? Convert.ToDouble(result) : 0.0;
            }
        }

        public async Task<Dictionary<int, double>> GetRatingPercentagesAsync(string courseId, int totalComments)
        {
            var ratingPercentages = new Dictionary<int, double>();
            for (int i = 1; i <= 5; i++)
            {
                int count = 0;
                using (var connection = new SqlConnection(_connectionString))
                {
                    var query = "SELECT COUNT(*) FROM feedback WHERE course_id = @CourseId AND star_rating = @Rating AND hidden_status = 0";
                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@CourseId", courseId);
                    command.Parameters.AddWithValue("@Rating", i);
                    await connection.OpenAsync();
                    count = (int)await command.ExecuteScalarAsync();
                }
                ratingPercentages[i] = totalComments > 0 ? (double)count / totalComments : 0;
            }
            return ratingPercentages;
        }
        public async Task<List<Models.Course>> GetPendingCoursesAsync()
        {
            var pendingCourses = new List<Models.Course>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM course WHERE course_status IN (0, 1, 2) ORDER BY " +
                            "CASE WHEN course_status = 1 THEN 0 ELSE 1 END, course_created_at";
                var command = new SqlCommand(query, connection);
                await connection.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var course = new Models.Course
                    {
                        CourseId = reader["course_id"].ToString(),
                        CourseName = reader["course_name"].ToString(),
                        CourseDescription = reader["course_description"].ToString(),
                        CourseStatus = Convert.ToInt32(reader["course_status"]),
                        CoursePicture = reader["course_picture"]?.ToString(),
                        Price = reader["price"] != DBNull.Value ? Convert.ToDecimal(reader["price"]) : 0,
                        CourseCreatedAt = Convert.ToDateTime(reader["course_created_at"])
                    };
                    pendingCourses.Add(course);
                }
            }
            return pendingCourses;
        }
        public async Task<bool> UpdateCourseStatusAsync(string courseId, int status)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "UPDATE course SET course_status = @Status WHERE course_id = @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                command.Parameters.AddWithValue("@Status", status);
                await connection.OpenAsync();
                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }

        public async Task<Models.Account> GetUserByIdAsync(string userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM account WHERE user_id = @UserId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new Models.Account
                        {
                            UserId = reader["user_id"].ToString(),
                            PaymentPoint = Convert.ToDecimal(reader["payment_point"])
                        };
                    }
                }
            }
            return null;
        }
        public async Task<bool> EnrollUserInCourseAsync(string enrollmentId, string userId, string courseId, DateTime createdAt)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "INSERT INTO enrollment (enrollment_id, user_id, course_id, enrollment_status, approved, enrollment_created_at) " +
                            "VALUES (@EnrollmentId, @UserId, @CourseId, 1, 1, @CreatedAt)";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@EnrollmentId", enrollmentId);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@CourseId", courseId);
                command.Parameters.AddWithValue("@CreatedAt", createdAt);
                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync() > 0;
            }
        }
        public async Task<bool> UpdateUserPaymentPointsAsync(string userId, decimal amount)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "UPDATE account SET payment_point = payment_point - @Amount WHERE user_id = @UserId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Amount", amount);
                command.Parameters.AddWithValue("@UserId", userId);
                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync() > 0;
            }
        }
        public async Task<bool> HasChaptersAndLessonsAsync(string courseId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT COUNT(*) FROM chapter ch JOIN lesson l ON ch.chapter_id = l.chapter_id WHERE ch.course_id = @CourseId";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);
                await connection.OpenAsync();
                return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            }
        }
        public async Task<string> GenerateNewEnrollmentIdAsync()
        {
            string maxEnrollmentId = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT TOP 1 enrollment_id FROM enrollment ORDER BY enrollment_id DESC";
                var command = new SqlCommand(query, connection);
                await connection.OpenAsync();
                maxEnrollmentId = (await command.ExecuteScalarAsync())?.ToString();
            }

            int newIdNumber = 1;
            if (!string.IsNullOrEmpty(maxEnrollmentId) && maxEnrollmentId.Length > 2)
            {
                newIdNumber = int.Parse(maxEnrollmentId.Substring(2)) + 1;
            }

            return "EN" + newIdNumber.ToString("D3");
        }

        public List<Models.Course> GetInstructorCoursesByCategory(string userId, string categoryId)
        {
            var courses = new List<Models.Course>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"
SELECT 
    c.course_id, 
    c.course_name, 
    c.course_description, 
    c.course_status, 
    c.course_picture, 
    c.price, 
    c.course_created_at,
    a.full_name AS CreatedBy
FROM 
    course c
JOIN 
    course_category_mapping ccm ON c.course_id = ccm.course_id
LEFT JOIN 
    account a ON c.created_by = a.user_id
WHERE 
    ccm.course_category_id = @CategoryId
ORDER BY 
    c.course_created_at DESC";


                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@CategoryId", categoryId);

                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    courses.Add(new Models.Course
                    {
                        CourseId = reader["course_id"].ToString(),
                        CourseName = reader["course_name"].ToString(),
                        CourseDescription = reader["course_description"].ToString(),
                        CourseStatus = Convert.ToInt32(reader["course_status"]),
                        CoursePicture = reader["course_picture"]?.ToString(),
                        Price = reader["price"] != DBNull.Value ? Convert.ToDecimal(reader["price"]) : 0,
                        CourseCreatedAt = Convert.ToDateTime(reader["course_created_at"]),
                        CreatedBy = reader["CreatedBy"].ToString() // Lấy tên người tạo

                    });
                }
            }
            return courses;
        }

        public List<Models.Course> GetActiveCoursesByCategory(string categoryId)
        {
            var courses = new List<Models.Course>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"
SELECT 
    c.course_id, 
    c.course_name, 
    c.course_description, 
    c.course_status, 
    c.course_picture, 
    c.price, 
    c.course_created_at,
    a.full_name AS CreatedBy
FROM 
    course c
JOIN 
    course_category_mapping ccm ON c.course_id = ccm.course_id
LEFT JOIN 
    account a ON c.created_by = a.user_id
WHERE 
    c.course_status = 2 
    AND ccm.course_category_id = @CategoryId
ORDER BY 
    c.course_created_at DESC";


                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CategoryId", categoryId);

                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    courses.Add(new Models.Course
                    {
                        CourseId = reader["course_id"].ToString(),
                        CourseName = reader["course_name"].ToString(),
                        CourseDescription = reader["course_description"].ToString(),
                        CourseStatus = Convert.ToInt32(reader["course_status"]),
                        CoursePicture = reader["course_picture"]?.ToString(),
                        Price = reader["price"] != DBNull.Value ? Convert.ToDecimal(reader["price"]) : 0,
                        CourseCreatedAt = Convert.ToDateTime(reader["course_created_at"]),
                        CreatedBy = reader["CreatedBy"].ToString() // Lấy tên người tạo

                    });
                }
            }
            return courses;
        }

        public async Task<List<CourseCategory>> GetTopCourseCategoriesAsync()
        {
            var categories = new List<CourseCategory>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"
            SELECT TOP 5
                cc.course_category_id AS CourseCategoryId,
                cc.course_category_name AS CourseCategoryName,
                COUNT(c.course_id) AS CourseCount
            FROM course_category cc
            LEFT JOIN course_category_mapping ccm ON cc.course_category_id = ccm.course_category_id
            LEFT JOIN course c ON c.course_id = ccm.course_id AND c.course_status = 2
            GROUP BY cc.course_category_id, cc.course_category_name
            ORDER BY CourseCount DESC, cc.course_category_name ASC";
                var command = new SqlCommand(query, connection);
                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        categories.Add(new CourseCategory
                        {
                            CourseCategoryId = reader["CourseCategoryId"].ToString(),
                            CourseCategoryName = reader["CourseCategoryName"].ToString()
                        });
                    }
                }
            }
            return categories;
        }

        public async Task<double> GetCourseProgressAsync(string userId, string courseId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                // Đếm số lượng bài học đã hoàn thành
                var completedLessonsQuery = @"
            SELECT COUNT(*)
            FROM lesson_completion lc
            JOIN lesson l ON lc.lesson_id = l.lesson_id
            JOIN chapter ch ON l.chapter_id = ch.chapter_id
            WHERE lc.user_id = @UserId AND ch.course_id = @CourseId";
                var completedLessonsCommand = new SqlCommand(completedLessonsQuery, connection);
                completedLessonsCommand.Parameters.AddWithValue("@UserId", userId);
                completedLessonsCommand.Parameters.AddWithValue("@CourseId", courseId);

                // Đếm tổng số bài học trong khóa học
                var totalLessonsQuery = @"
            SELECT COUNT(*)
            FROM lesson l
            JOIN chapter ch ON l.chapter_id = ch.chapter_id
            WHERE ch.course_id = @CourseId";
                var totalLessonsCommand = new SqlCommand(totalLessonsQuery, connection);
                totalLessonsCommand.Parameters.AddWithValue("@CourseId", courseId);

                await connection.OpenAsync();

                int completedLessons = (int)await completedLessonsCommand.ExecuteScalarAsync();
                int totalLessons = (int)await totalLessonsCommand.ExecuteScalarAsync();

                return totalLessons > 0 ? (double)completedLessons / totalLessons * 100 : 0;
            }
        }
        public async Task<int> GetCourseCountByCategoryAsync(string categoryId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"
            SELECT COUNT(*)
            FROM course c
            INNER JOIN course_category_mapping ccm ON c.course_id = ccm.course_id
            WHERE ccm.course_category_id = @CategoryId AND c.course_status = 2";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CategoryId", categoryId);
                await connection.OpenAsync();
                return (int)await command.ExecuteScalarAsync();
            }
        }

        public async Task<bool> IsUserCourseCreatorAsync(string userId, string courseId)
        {
            try
            {
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand("SELECT COUNT(*) FROM course WHERE course_id = @CourseId AND created_by = @UserId", connection);
                    command.Parameters.AddWithValue("@CourseId", courseId);
                    command.Parameters.AddWithValue("@UserId", userId);

                    var count = (int)await command.ExecuteScalarAsync();
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<int> GetTotalCourseCountAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT COUNT(*) FROM course";
                var command = new SqlCommand(query, connection);
                await connection.OpenAsync();
                return (int)await command.ExecuteScalarAsync();
            }
        }

        public async Task<int> GetPendingCourseCountAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT COUNT(*) FROM course WHERE course_status = 1";
                var command = new SqlCommand(query, connection);
                await connection.OpenAsync();
                return (int)await command.ExecuteScalarAsync();
            }
        }

        public async Task<int> GetAcceptedCourseCountAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT COUNT(*) FROM course WHERE course_status = 2";
                var command = new SqlCommand(query, connection);
                await connection.OpenAsync();
                return (int)await command.ExecuteScalarAsync();
            }
        }

        public async Task<int> GetRejectedCourseCountAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT COUNT(*) FROM course WHERE course_status = 0";
                var command = new SqlCommand(query, connection);
                await connection.OpenAsync();
                return (int)await command.ExecuteScalarAsync();
            }
        }

        public async Task<int> GetNotApprovedCourseCountAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT COUNT(*) FROM course WHERE course_status NOT IN (0, 1, 2)";
                var command = new SqlCommand(query, connection);
                await connection.OpenAsync();
                return (int)await command.ExecuteScalarAsync();
            }
        }


    }
}
