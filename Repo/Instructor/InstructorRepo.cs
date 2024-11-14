using BrainStormEra.Models;
using Microsoft.EntityFrameworkCore;
using BrainStormEra.Views.Course;
using Microsoft.Data.SqlClient;
namespace BrainStormEra.Repositories
{
    public class InstructorRepo
    {
        private readonly string _connectionString;
        public InstructorRepo( IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SwpMainContext");
        }
        public List<CourseCategory> GetTopCategories()
        {
            string categoryQuery = @"
                SELECT TOP 5
                    course_category_id AS CourseCategoryId,
                    course_category_name AS CourseCategoryName
                FROM
                    course_category
                ORDER BY
                    course_category_name;
            ";

            var categories = new List<CourseCategory>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = categoryQuery;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(new CourseCategory
                            {
                                CourseCategoryId = reader["CourseCategoryId"].ToString(),
                                CourseCategoryName = reader["CourseCategoryName"].ToString()
                            });
                        }
                    }
                }
            }

            return categories;
        }

        public List<ManagementCourseViewModel> GetRecommendedCourses()
        {
            string sqlQuery = @"
                SELECT TOP 4
                    c.course_id AS CourseId,
                    c.course_name AS CourseName,
                    c.course_description AS CourseDescription,
                    c.course_status AS CourseStatus,
                    c.course_picture AS CoursePicture,
                    c.price AS Price,
                    c.course_created_at AS CourseCreatedAt,
                    a.full_name AS CreatedBy,
                    COALESCE(ROUND(AVG(f.star_rating), 0), 0) AS StarRating,
                    STUFF((SELECT DISTINCT ', ' + cc.course_category_name
                           FROM course_category_mapping AS ccm
                           JOIN course_category AS cc ON ccm.course_category_id = cc.course_category_id
                           WHERE ccm.course_id = c.course_id
                           FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS CourseCategories
                FROM 
                    course c
                    LEFT JOIN account a ON c.created_by = a.user_id
                    LEFT JOIN enrollment e ON c.course_id = e.course_id
                    LEFT JOIN feedback f ON c.course_id = f.course_id
                WHERE 
                    c.course_status = 2
                GROUP BY 
                    c.course_id, c.course_name, c.course_description, c.course_status, 
                    c.course_picture, c.price, c.course_created_at, a.full_name
                ORDER BY 
                    COUNT(e.enrollment_id) DESC;
            ";

            var recommendedCourses = new List<ManagementCourseViewModel>();
            var courseDictionary = new Dictionary<string, ManagementCourseViewModel>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var courseId = reader["CourseId"].ToString();

                            if (!courseDictionary.TryGetValue(courseId, out var course))
                            {
                                course = new ManagementCourseViewModel
                                {
                                    CourseId = courseId,
                                    CourseName = reader["CourseName"].ToString(),
                                    CourseDescription = reader["CourseDescription"].ToString(),
                                    CourseStatus = reader["CourseStatus"] as int?,
                                    CoursePicture = reader["CoursePicture"].ToString(),
                                    Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                                    CourseCreatedAt = reader.GetDateTime(reader.GetOrdinal("CourseCreatedAt")),
                                    CreatedBy = reader["CreatedBy"].ToString(),
                                    StarRating = reader["StarRating"] != DBNull.Value ? (byte?)Convert.ToByte(reader["StarRating"]) : (byte?)0,
                                    CourseCategories = new List<CourseCategory>()
                                };
                                courseDictionary[courseId] = course;
                            }

                            var categoriesString = reader["CourseCategories"].ToString();
                            if (!string.IsNullOrEmpty(categoriesString))
                            {
                                foreach (var categoryName in categoriesString.Split(','))
                                {
                                    var trimmedCategoryName = categoryName.Trim();
                                    if (!string.IsNullOrEmpty(trimmedCategoryName) &&
                                        course.CourseCategories.All(c => c.CourseCategoryName != trimmedCategoryName))
                                    {
                                        course.CourseCategories.Add(new CourseCategory
                                        {
                                            CourseCategoryName = trimmedCategoryName
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return courseDictionary.Values.ToList();
        }

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
    }
}
