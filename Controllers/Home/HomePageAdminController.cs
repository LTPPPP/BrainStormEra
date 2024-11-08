using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BrainStormEra.Controllers
{
    public class HomePageAdminController : Controller
    {
        private readonly string _connectionString;

        public HomePageAdminController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SwpMainContext");
        }

        [HttpGet]
        public IActionResult HomepageAdmin()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != null)
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        var query = "SELECT full_name AS FullName, COALESCE(user_picture, '~/lib/img/User-img/default_user.png') AS UserPicture FROM account WHERE user_id = @userId";
                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@userId", userId);
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    ViewBag.FullName = reader["FullName"]?.ToString() ?? "Guest";
                                    ViewBag.UserPicture = reader["UserPicture"]?.ToString();
                                }
                                else
                                {
                                    ViewBag.FullName = "Guest";
                                    ViewBag.UserPicture = "~/lib/img/User-img/default_user.png";
                                }
                            }
                        }
                    }
                    return View("~/Views/Home/HomePageAdmin.cshtml");
                }
            }
            return RedirectToAction("LoginPage", "Login");
        }


        [HttpGet]
        public IActionResult GetUserStatistics()
        {
            try
            {
                var userStatistics = new List<object>();
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"SELECT CONVERT(DATE, account_created_at) AS Date, COUNT(*) AS Count 
                                  FROM account GROUP BY CONVERT(DATE, account_created_at)";
                    using (var command = new SqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            userStatistics.Add(new
                            {
                                Date = ((DateTime)reader["Date"]).ToString("yyyy-MM-dd"),
                                Count = (int)reader["Count"]
                            });
                        }
                    }
                }
                return Json(userStatistics);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user statistics: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving user statistics." });
            }
        }

        [HttpGet]
        public JsonResult GetConversationStatistics()
        {
            try
            {
                var conversationStatistics = new List<object>();
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"SELECT CONVERT(DATE, conversation_time) AS Date, COUNT(*) AS Count 
                                  FROM chatbot_conversation GROUP BY CONVERT(DATE, conversation_time)";
                    using (var command = new SqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            conversationStatistics.Add(new
                            {
                                Date = ((DateTime)reader["Date"]).ToString("yyyy-MM-dd"),
                                Count = (int)reader["Count"]
                            });
                        }
                    }
                }
                return Json(conversationStatistics);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching conversation statistics: {ex.Message}");
                return Json(new { message = "An error occurred while retrieving conversation statistics." });
            }
        }

        [HttpGet]
        public JsonResult GetCourseCreationStatistics()
        {
            try
            {
                var courseStatistics = new List<dynamic>();

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var checkQuery = "SELECT COUNT(*) FROM course";
                    using (var checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        int courseCount = (int)checkCommand.ExecuteScalar();
                        if (courseCount == 0)
                        {
                            return Json(new
                            {
                                success = false,
                                message = "No courses found",
                                data = new List<object>()
                            });
                        }
                    }

                    var query = @"SELECT CONVERT(DATE, course_created_at) AS Date, COUNT(*) AS Count 
                          FROM course WHERE course_created_at <= GETDATE() 
                          GROUP BY CONVERT(DATE, course_created_at)";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            courseStatistics.Add(new
                            {
                                Date = ((DateTime)reader["Date"]).ToString("yyyy-MM-dd"),
                                Count = (int)reader["Count"]
                            });
                        }
                    }

                    return Json(new
                    {
                        success = true,
                        data = courseStatistics,
                        totalCourses = courseStatistics.Count,
                        dateRange = new
                        {
                            start = courseStatistics.Count > 0 ? courseStatistics[0].Date : "N/A",
                            end = DateTime.Now.ToString("yyyy-MM-dd")
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCourseCreationStatistics: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "An error occurred while retrieving course statistics.",
                    error = ex.Message,
                    data = new List<object>()
                });
            }
        }

    }
}
