using BrainStormEra.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace BrainStormEra.Repo.Certificate
{
    public interface ICertificateRepository
    {
        Task<List<CertificateSummaryViewModel>> GetCompletedCoursesAsync(string userId);
        Task<CertificateViewModel> GetCertificateDetailsAsync(string userId, string courseId);
    }

    public class CertificateRepository : ICertificateRepository
    {
        private readonly SwpMainContext _context;

        public CertificateRepository(SwpMainContext context)
        {
            _context = context;
        }

        public async Task<List<CertificateSummaryViewModel>> GetCompletedCoursesAsync(string userId)
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
                        AND e.enrollment_status = 5
                 	    AND e.certificate_issued_date IS NOT NULL";

            var completedCourses = new List<CertificateSummaryViewModel>();

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

        public async Task<CertificateViewModel> GetCertificateDetailsAsync(string userId, string courseId)
        {
            var query = @"
                 SELECT 
                        a.full_name AS UserName,
                        c.course_name AS CourseName,
                        c.course_description AS CourseDescription,
                        e.certificate_issued_date AS CompletedDate,
                        e.enrollment_created_at AS StartedDate
                    FROM 
                        enrollment e
                    JOIN 
                        course c ON e.course_id = c.course_id
                    JOIN 
                        account a ON e.user_id = a.user_id
                    WHERE 
                        e.user_id = @UserId 
                        AND e.course_id = @CourseId
                        AND e.enrollment_status = 5";

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
                            return new CertificateViewModel
                            {
                                UserName = reader["UserName"] as string,
                                CourseName = reader["CourseName"] as string,
                                CourseDescription = reader["CourseDescription"] as string,
                                CompletedDate = reader["CompletedDate"] != DBNull.Value ? (DateTime)reader["CompletedDate"] : default,
                                StartedDate = reader["StartedDate"] != DBNull.Value ? (DateTime)reader["CompletedDate"] : default


                            };
                        }
                    }
                }
            }

            return null;
        }
    }

    public class CertificateSummaryViewModel
    {
        public string CourseId { get; set; }
        public string CourseName { get; set; }
        public DateTime CompletedDate { get; set; }


    }

    // ViewModel cho chi tiết chứng chỉ
    public class CertificateViewModel
    {
        public string UserName { get; set; }
        public string CourseName { get; set; }
        public DateTime CompletedDate { get; set; }
        public DateTime StartedDate { get; set; }
        public string CourseDescription { get; set; }
    }
}
