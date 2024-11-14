using BrainStormEra.Models;
using BrainStormEra.Models.ViewModels;
using BrainStormEra.Repo.Certificate;
using BrainStormEra.Views.Profile;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace BrainStormEra.Repo.Admin
{
    public class ProfileRepo
    {
        private readonly SwpMainContext _context;

        public ProfileRepo(SwpMainContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Method to get learners and instructors
        public async Task<List<UserDetailsViewModel>> GetLearnersAndInstructorsAsync()
        {
            var users = new List<UserDetailsViewModel>();

            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT a.user_id, a.user_role, a.username, a.user_email, a.full_name, 
                               a.date_of_birth, a.gender, a.phone_number, a.user_address, 
                               a.account_created_at, a.user_picture,  
                               CASE WHEN EXISTS (
                                   SELECT 1 FROM enrollment e WHERE e.user_id = a.user_id AND e.approved = 1
                               ) THEN 1 ELSE 0 END AS Approved
                        FROM account a
                        WHERE a.user_role IN (2, 3)";  // Only get Learners and Instructors

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var userDetails = new UserDetailsViewModel
                            {
                                UserId = reader["user_id"].ToString(),
                                UserRole = reader["user_role"] as int?,
                                Username = reader["username"].ToString(),
                                UserEmail = reader["user_email"].ToString(),
                                FullName = reader["full_name"]?.ToString(),
                                DateOfBirth = reader["date_of_birth"] == DBNull.Value ? (DateOnly?)null : DateOnly.FromDateTime(Convert.ToDateTime(reader["date_of_birth"])),
                                Gender = reader["gender"]?.ToString(),
                                PhoneNumber = reader["phone_number"]?.ToString(),
                                UserAddress = reader["user_address"]?.ToString(),
                                AccountCreatedAt = (DateTime)reader["account_created_at"],
                                UserPicture = reader["user_picture"]?.ToString(),
                                Approved = Convert.ToInt32(reader["Approved"])
                            };

                            users.Add(userDetails);
                        }
                    }
                }
            }

            return users;
        }

        // Method to get user details by userId
        public async Task<UserDetailsViewModel?> GetUserDetailsAsync(string userId)
        {
            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT a.user_id, a.user_role, a.username, a.user_email, a.full_name, 
                               a.date_of_birth, a.gender, a.phone_number, a.user_address, 
                               a.account_created_at, a.user_picture,
                               CASE WHEN EXISTS (
                                   SELECT 1 FROM enrollment e WHERE e.user_id = a.user_id AND e.approved = 1
                               ) THEN 1 ELSE 0 END AS Approved
                        FROM account a
                        WHERE a.user_id = @userId";

                    command.Parameters.Add(new SqlParameter("@userId", userId));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new UserDetailsViewModel
                            {
                                UserId = reader["user_id"].ToString(),
                                UserRole = reader["user_role"] as int?,
                                Username = reader["username"].ToString(),
                                UserEmail = reader["user_email"].ToString(),
                                FullName = reader["full_name"]?.ToString(),
                                DateOfBirth = reader["date_of_birth"] == DBNull.Value ? (DateOnly?)null : DateOnly.FromDateTime(Convert.ToDateTime(reader["date_of_birth"])),
                                Gender = reader["gender"]?.ToString(),
                                PhoneNumber = reader["phone_number"]?.ToString(),
                                UserAddress = reader["user_address"]?.ToString(),
                                UserPicture = reader["user_picture"]?.ToString(),
                                AccountCreatedAt = (DateTime)reader["account_created_at"],
                                Approved = Convert.ToInt32(reader["Approved"])
                            };
                        }
                    }
                }
            }
            return null;
        }

        // Method to ban a learner
        public async Task BanLearnerAsync(string userId)
        {
            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE enrollment
                        SET approved = 0
                        WHERE user_id = @userId";
                    command.Parameters.Add(new SqlParameter("@userId", userId));

                    var affectedRows = await command.ExecuteNonQueryAsync();

                    if (affectedRows == 0)
                    {
                        throw new Exception("Failed to ban learner: No matching enrollment found.");
                    }
                }
            }
        }

        // Method to unban a learner
        public async Task UnbanLearnerAsync(string userId)
        {
            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE enrollment
                        SET approved = 1
                        WHERE user_id = @userId";
                    command.Parameters.Add(new SqlParameter("@userId", userId));

                    var affectedRows = await command.ExecuteNonQueryAsync();

                    if (affectedRows == 0)
                    {
                        throw new Exception("Failed to unban learner: No matching enrollment found.");
                    }
                }
            }
        }

        // Method to promote a learner to instructor
        public async Task<string?> PromoteLearnerToInstructorAsync(string userId)
        {
            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();

                // Check payment and enrollment conditions
                using (var checkCommand = connection.CreateCommand())
                {
                    checkCommand.CommandText = @"
                        SELECT payment_point 
                        FROM account 
                        WHERE user_id = @userId AND user_role = 3";
                    checkCommand.Parameters.Add(new SqlParameter("@userId", userId));

                    var paymentPoint = (decimal?)await checkCommand.ExecuteScalarAsync();

                    // Return null if payment_point is not zero or user doesn't exist as a learner
                    if (paymentPoint == null || paymentPoint > 0)
                        return null;
                }

                using (var enrollmentCheckCommand = connection.CreateCommand())
                {
                    enrollmentCheckCommand.CommandText = @"
                        SELECT COUNT(*) 
                        FROM enrollment 
                        WHERE user_id = @userId";
                    enrollmentCheckCommand.Parameters.Add(new SqlParameter("@userId", userId));

                    var enrollmentCount = (int)await enrollmentCheckCommand.ExecuteScalarAsync();

                    // Return null if learner has enrollments
                    if (enrollmentCount > 0)
                        return null;
                }

                // Get the next instructor ID
                string newInstructorId;
                using (var maxIdCommand = connection.CreateCommand())
                {
                    maxIdCommand.CommandText = @"
                        SELECT MAX(CAST(SUBSTRING(user_id, 3, LEN(user_id)) AS INT)) 
                        FROM account 
                        WHERE user_role = 2";
                    var maxInstructorId = await maxIdCommand.ExecuteScalarAsync();
                    int nextId = (maxInstructorId == DBNull.Value ? 1 : (int)maxInstructorId + 1);
                    newInstructorId = $"IN{nextId:D3}"; // Format as IN001, IN002, etc.
                }

                // Promote learner to instructor
                using (var promoteCommand = connection.CreateCommand())
                {
                    promoteCommand.CommandText = @"
                        UPDATE account 
                        SET user_id = @newId, user_role = 2 
                        WHERE user_id = @userId AND user_role = 3";
                    promoteCommand.Parameters.Add(new SqlParameter("@newId", newInstructorId));
                    promoteCommand.Parameters.Add(new SqlParameter("@userId", userId));

                    var rowsAffected = await promoteCommand.ExecuteNonQueryAsync();

                    if (rowsAffected == 0)
                        throw new Exception("Failed to promote learner to instructor.");
                }

                return newInstructorId;
            }
        }

        // Method to get learners grouped by instructor's courses
        public async Task<Dictionary<string, (string CourseName, List<UserDetailsViewModel> Learners)>> GetLearnersByInstructorCoursesAsync(string instructorId)
        {
            var courseLearners = new Dictionary<string, (string CourseName, List<UserDetailsViewModel> Learners)>();

            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT c.course_id, c.course_name, a.user_id, a.full_name, a.username, a.user_email, a.phone_number,
                               a.date_of_birth, a.gender, a.user_address, a.user_picture
                        FROM account a
                        JOIN enrollment e ON e.user_id = a.user_id
                        JOIN course c ON c.course_id = e.course_id
                        WHERE c.created_by = @instructorId AND e.approved = 1 AND a.user_role = 3
                        ORDER BY c.course_name";

                    command.Parameters.Add(new SqlParameter("@instructorId", instructorId));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var courseId = reader["course_id"].ToString();
                            var courseName = reader["course_name"].ToString();

                            var learner = new UserDetailsViewModel
                            {
                                UserId = reader["user_id"].ToString(),
                                FullName = reader["full_name"]?.ToString(),
                                Username = reader["username"].ToString(),
                                UserEmail = reader["user_email"].ToString(),
                                PhoneNumber = reader["phone_number"]?.ToString(),
                                DateOfBirth = reader["date_of_birth"] == DBNull.Value ? (DateOnly?)null : DateOnly.FromDateTime((DateTime)reader["date_of_birth"]),
                                Gender = reader["gender"]?.ToString(),
                                UserAddress = reader["user_address"]?.ToString(),
                                UserPicture = reader["user_picture"]?.ToString()
                            };

                            if (!courseLearners.ContainsKey(courseId))
                            {
                                courseLearners[courseId] = (courseName, new List<UserDetailsViewModel>());
                            }

                            courseLearners[courseId].Learners.Add(learner);
                        }
                    }
                }
            }

            return courseLearners;
        }
        // Method to get completed courses for a specific learner using raw SQL
        public async Task<List<CertificateSummaryViewModel>> GetCompletedCoursesForLearnerAsync(string userId)
        {
            var completedCourses = new List<CertificateSummaryViewModel>();

            var query = @"
                SELECT 
                    c.course_id AS CourseId,
                    c.course_name AS CourseName,
                    e.certificate_issued_date AS CompletedDate
                FROM 
                    enrollment e
                JOIN 
                    course c ON e.course_id = c.course_id
                WHERE 
                    e.user_id = @UserId 
                    AND e.enrollment_status = 5
                    AND e.certificate_issued_date IS NOT NULL";

            using (var connection = _context.Database.GetDbConnection())
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.CommandType = CommandType.Text;
                    command.Parameters.Add(new SqlParameter("@UserId", userId));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            completedCourses.Add(new CertificateSummaryViewModel
                            {
                                CourseId = reader["CourseId"] as string,
                                CourseName = reader["CourseName"] as string,
                                CompletedDate = reader["CompletedDate"] != DBNull.Value ? (DateTime)reader["CompletedDate"] : default
                            });
                        }
                    }
                }
            }

            return completedCourses;
        }
        public async Task<CertificateSummaryViewModel?> GetCertificateDetailsForCourseAsync(string userId, string courseId)
        {
            var query = @"
        SELECT 
            c.course_id AS CourseId,
            c.course_name AS CourseName,
            e.certificate_issued_date AS CompletedDate
        FROM 
            enrollment e
        JOIN 
            course c ON e.course_id = c.course_id
        WHERE 
            e.user_id = @UserId 
            AND e.course_id = @CourseId
            AND e.enrollment_status = 5
            AND e.certificate_issued_date IS NOT NULL";

            using (var connection = _context.Database.GetDbConnection())
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.CommandType = CommandType.Text;
                    command.Parameters.Add(new SqlParameter("@UserId", userId));
                    command.Parameters.Add(new SqlParameter("@CourseId", courseId));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new CertificateSummaryViewModel
                            {
                                CourseId = reader["CourseId"].ToString(),
                                CourseName = reader["CourseName"].ToString(),
                                CompletedDate = reader["CompletedDate"] != DBNull.Value
                                                ? (DateTime)reader["CompletedDate"]
                                                : default
                            };
                        }
                    }
                }
            }

            return null; // Return null if no completed course certificate is found
        }

    }
}