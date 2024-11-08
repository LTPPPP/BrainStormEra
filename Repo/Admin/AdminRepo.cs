using BrainStormEra.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BrainStormEra.Repo.Admin;

public class AdminRepo
{
    private readonly string _connectionString;

    public AdminRepo(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SwpMainContext");
    }

    // Method to get user details
    public async Task<(string FullName, string UserPicture)> GetUserDetailsAsync(string userId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var query = "SELECT full_name AS FullName, COALESCE(user_picture, '~/lib/img/User-img/default_user.png') AS UserPicture FROM account WHERE user_id = @userId";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return (reader["FullName"].ToString(), reader["UserPicture"].ToString());
                    }
                }
            }
        }
        return ("Guest", "~/lib/img/User-img/default_user.png");
    }

    // Method to get user statistics
    public async Task<List<object>> GetUserStatisticsAsync()
    {
        var userStatistics = new List<object>();
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var query = @"SELECT CONVERT(DATE, account_created_at) AS Date, COUNT(*) AS Count FROM account GROUP BY CONVERT(DATE, account_created_at)";
            using (var command = new SqlCommand(query, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    userStatistics.Add(new
                    {
                        Date = ((DateTime)reader["Date"]).ToString("yyyy-MM-dd"),
                        Count = (int)reader["Count"]
                    });
                }
            }
        }
        return userStatistics;
    }

    // Method to get conversation statistics
    public async Task<List<object>> GetConversationStatisticsAsync()
    {
        var conversationStatistics = new List<object>();
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var query = @"SELECT CONVERT(DATE, conversation_time) AS Date, COUNT(*) AS Count FROM chatbot_conversation GROUP BY CONVERT(DATE, conversation_time)";
            using (var command = new SqlCommand(query, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    conversationStatistics.Add(new
                    {
                        Date = ((DateTime)reader["Date"]).ToString("yyyy-MM-dd"),
                        Count = (int)reader["Count"]
                    });
                }
            }
        }
        return conversationStatistics;
    }

    // Method to get course creation statistics
    public async Task<object> GetCourseCreationStatisticsAsync()
    {
        var courseStatistics = new List<dynamic>();
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var checkQuery = "SELECT COUNT(*) FROM course";
            using (var checkCommand = new SqlCommand(checkQuery, connection))
            {
                int courseCount = (int)await checkCommand.ExecuteScalarAsync();
                if (courseCount == 0)
                {
                    return new { success = false, message = "No courses found", data = new List<object>() };
                }
            }

            var query = @"SELECT CONVERT(DATE, course_created_at) AS Date, COUNT(*) AS Count FROM course WHERE course_created_at <= GETDATE() GROUP BY CONVERT(DATE, course_created_at)";
            using (var command = new SqlCommand(query, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    courseStatistics.Add(new
                    {
                        Date = ((DateTime)reader["Date"]).ToString("yyyy-MM-dd"),
                        Count = (int)reader["Count"]
                    });
                }
            }
        }
        return new
        {
            success = true,
            data = courseStatistics,
            totalCourses = courseStatistics.Count,
            dateRange = new
            {
                start = courseStatistics.Count > 0 ? courseStatistics[0].Date : "N/A",
                end = DateTime.Now.ToString("yyyy-MM-dd")
            }
        };
    }
}
