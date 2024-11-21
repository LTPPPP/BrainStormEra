using BrainStormEra.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using BrainStormEra.Views.Profile;
using BrainStormEra.Views.Admin;

namespace BrainStormEra.Repo
{
    public class AccountRepo
    {
        private readonly SwpMainContext _context;
        private readonly ILogger<AccountRepo> _logger;

        public AccountRepo(SwpMainContext context, ILogger<AccountRepo> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Check if a username is already taken
        public async Task<bool> IsUsernameTakenAsync(string username)
        {
            try
            {
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand("SELECT COUNT(*) FROM account WHERE username = @Username", connection);
                    command.Parameters.AddWithValue("@Username", username);

                    var count = (int)await command.ExecuteScalarAsync();
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking username availability.");
                throw;
            }
        }

        // Check if an email is already taken
        public async Task<bool> IsEmailTakenAsync(string email)
        {
            try
            {
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand("SELECT COUNT(*) FROM account WHERE user_email = @UserEmail", connection);
                    command.Parameters.AddWithValue("@UserEmail", email);

                    var count = (int)await command.ExecuteScalarAsync();
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email availability.");
                throw;
            }
        }

        // Generate a unique User ID based on user role
        public async Task<string> GenerateUniqueUserIdAsync(int userRole)
        {
            string prefix = userRole switch
            {
                1 => "AD",
                2 => "IN",
                3 => "LN",
                _ => throw new ArgumentException("Invalid user role", nameof(userRole))
            };

            try
            {
                string lastId = null;
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand("SELECT TOP 1 user_id FROM account WHERE user_role = @UserRole ORDER BY user_id DESC", connection);
                    command.Parameters.AddWithValue("@UserRole", userRole);

                    lastId = await command.ExecuteScalarAsync() as string;
                }

                int nextNumber = 1;
                if (!string.IsNullOrEmpty(lastId) && int.TryParse(lastId[2..], out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }

                string newId = $"{prefix}{nextNumber:D3}";

                // Ensure generated ID is unique
                while (await IsUserIdExistsAsync(newId))
                {
                    nextNumber++;
                    newId = $"{prefix}{nextNumber:D3}";
                }

                return newId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating unique User ID.");
                throw;
            }
        }

        // Check if a User ID already exists
        private async Task<bool> IsUserIdExistsAsync(string userId)
        {
            try
            {
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand("SELECT COUNT(*) FROM account WHERE user_id = @UserId", connection);
                    command.Parameters.AddWithValue("@UserId", userId);

                    int count = (int)await command.ExecuteScalarAsync();
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if User ID exists.");
                throw;
            }
        }

        // Login method
        public async Task<Account?> Login(string username, string hashedPassword)
        {
            try
            {
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand("SELECT * FROM account WHERE username = @Username AND password = @Password", connection);
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", hashedPassword);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Account
                            {
                                UserId = reader["user_id"].ToString(),
                                UserRole = reader["user_role"] as int?,
                                Username = reader["username"].ToString(),
                                Password = reader["password"].ToString(),
                                UserEmail = reader["user_email"].ToString(),
                                FullName = reader["full_name"]?.ToString(),
                                PaymentPoint = reader["payment_point"] as decimal?,
                                AccountCreatedAt = (DateTime)reader["account_created_at"]
                            };
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login.");
                throw;
            }
        }

        // Register a new account
        public async Task RegisterAsync(Account newAccount)
        {
            try
            {
                if (string.IsNullOrEmpty(newAccount.FullName))
                {
                    newAccount.FullName = $"User{newAccount.UserId}";
                }
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand(@"INSERT INTO account (user_id, user_role, username, password, user_email, full_name, account_created_at, user_picture)
                                          VALUES (@UserId, @UserRole, @Username, @Password, @UserEmail, @FullName, @AccountCreatedAt, @UserPicture)", connection);

                    command.Parameters.AddWithValue("@UserId", newAccount.UserId);
                    command.Parameters.AddWithValue("@UserRole", newAccount.UserRole ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Username", newAccount.Username);
                    command.Parameters.AddWithValue("@Password", newAccount.Password);
                    command.Parameters.AddWithValue("@UserEmail", newAccount.UserEmail);
                    command.Parameters.AddWithValue("@FullName", newAccount.FullName);
                    command.Parameters.AddWithValue("@AccountCreatedAt", newAccount.AccountCreatedAt);
                    command.Parameters.AddWithValue("@UserPicture", "/uploads/User-img/default_user.png");
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering new account.");
                throw;
            }
        }
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
                               a.account_created_at,
                               CASE WHEN EXISTS (
                                   SELECT 1 FROM enrollment e WHERE e.user_id = a.user_id AND e.approved = 1
                               ) THEN 1 ELSE 0 END AS Approved
                        FROM account a
                        WHERE a.user_role IN (2, 3)"; // Chỉ lấy Learners và Instructors

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
                                Approved = Convert.ToInt32(reader["Approved"])
                            };

                            users.Add(userDetails);
                        }
                    }
                }
            }

            return users;
        }

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
                               a.account_created_at,
                               CASE WHEN EXISTS (
                                   SELECT 1 FROM enrollment e WHERE e.user_id = a.user_id AND e.approved = 1
                               ) THEN 1 ELSE 0 END AS Approved
                        FROM account a
                        LEFT JOIN enrollment e ON a.user_id = e.user_id
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
                                AccountCreatedAt = (DateTime)reader["account_created_at"],
                                Approved = Convert.ToInt32(reader["Approved"])
                            };
                        }
                    }
                }
            }
            return null;
        }

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
        public async Task<List<UserDetailsViewModel>> GetLearnersAsync()
        {
            var learners = new List<UserDetailsViewModel>();

            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                SELECT a.user_id, a.user_role, a.username, a.user_email, a.full_name, 
                       a.date_of_birth, a.gender, a.phone_number, a.user_address, 
                       a.account_created_at,
                       CASE WHEN EXISTS (
                           SELECT 1 FROM enrollment e WHERE e.user_id = a.user_id AND e.approved = 1
                       ) THEN 1 ELSE 0 END AS Approved
                FROM account a
                WHERE a.user_role = 3"; // Only get learners

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var learner = new UserDetailsViewModel
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
                                Approved = Convert.ToInt32(reader["Approved"])
                            };

                            learners.Add(learner);
                        }
                    }
                }
            }

            return learners;
        }
        public async Task<Account?> GetAccountByUserIdAsync(string userId)
        {
            try
            {
                return await _context.Accounts
                    .FromSqlRaw("SELECT * FROM account WHERE user_id = @p0", userId)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving account by User ID.");
                throw;
            }
        }

        public async Task<int?> GetUserRoleByUserIdAsync(string userId)
        {
            try
            {
                return await _context.Accounts
                    .Where(a => a.UserId == userId)
                    .Select(a => a.UserRole)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user role by User ID.");
                throw;
            }
        }

        public async Task UpdateAccountAsync(string userId, string? fullName, string? userEmail, string? phoneNumber, string? gender, string? userAddress, DateOnly? dateOfBirth)
        {
            var updateSql = @"
                UPDATE account
                SET full_name = @p1, user_email = @p2, phone_number = @p3, gender = @p4,
                    user_address = @p5, date_of_birth = @p6
                WHERE user_id = @p0";

            await _context.Database.ExecuteSqlRawAsync(
                updateSql,
                userId, fullName, userEmail, phoneNumber, gender, userAddress, dateOfBirth
            );
        }

        public async Task<string> SaveAvatarAsync(string userId, IFormFile avatar)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "User-img");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = Path.GetFileName(avatar.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }

            return $"/uploads/User-img/{fileName}";
        }

        public async Task UpdateUserPictureAsync(string userId, string filePath)
        {
            var updatePictureSql = "UPDATE account SET user_picture = @p1 WHERE user_id = @p0";
            await _context.Database.ExecuteSqlRawAsync(updatePictureSql, userId, filePath);
        }
        // Phương thức để lấy thông tin người dùng dựa trên email
        public async Task<Account?> GetUserByEmailAsync(string email)
        {
            try
            {
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand("SELECT * FROM account WHERE user_email = @Email", connection);
                    command.Parameters.AddWithValue("@Email", email);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Account
                            {
                                UserId = reader["user_id"].ToString(),
                                Username = reader["username"].ToString(),
                                UserEmail = reader["user_email"].ToString(),
                                UserRole = reader["user_role"] as int?,
                                FullName = reader["full_name"]?.ToString()
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by email.");
                throw;
            }
            return null;
        }

        // Phương thức để cập nhật mật khẩu của người dùng
        public async Task UpdatePasswordAsync(string userId, string newPassword)
        {
            try
            {
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand("UPDATE account SET password = @Password WHERE user_id = @UserId", connection);
                    command.Parameters.AddWithValue("@Password", newPassword);
                    command.Parameters.AddWithValue("@UserId", userId);

                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password.");
                throw;
            }
        }

        public async Task<List<UserRankingViewModel>> GetUserRankingAsync()
        {
            var rankings = new List<UserRankingViewModel>();

            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                SELECT a.user_id, a.username, a.full_name, COUNT(lc.lesson_id) AS CompletedCourses, a.user_picture
                FROM account a
                LEFT JOIN lesson_completion lc ON a.user_id = lc.user_id
                LEFT JOIN lesson l ON lc.lesson_id = l.lesson_id
                LEFT JOIN chapter ch ON l.chapter_id = ch.chapter_id
                LEFT JOIN course c ON ch.course_id = c.course_id
                WHERE a.user_role = 3 -- Chỉ lấy Learners
                GROUP BY a.user_id, a.username, a.full_name, a.user_picture
                ORDER BY COUNT(lc.lesson_id) DESC"; // Sắp xếp giảm dần

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            rankings.Add(new UserRankingViewModel
                            {
                                UserId = reader["user_id"].ToString(),
                                Username = reader["username"].ToString(),
                                FullName = reader["full_name"]?.ToString(),
                                CompletedCourses = (int)reader["CompletedCourses"],
                                UserPicture = reader["user_picture"]?.ToString() ?? "/images/default-user.png" // Đặt ảnh mặc định nếu không có
                            });
                        }
                    }
                }
            }

            return rankings;
        }

        public async Task<string> GetUserRankAsync(string userId)
        {
            try
            {
                using (var connection = _context.Database.GetDbConnection())
                {
                    await connection.OpenAsync();

                    // Kiểm tra số lượng khóa học đã hoàn thành
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                SELECT COUNT(lc.lesson_id)
                FROM lesson_completion lc
                WHERE lc.user_id = @UserId";
                    command.Parameters.Add(new SqlParameter("@UserId", userId));

                    var completedCoursesCount = (int)await command.ExecuteScalarAsync();

                    // Nếu không có khóa học nào đã hoàn thành, trả về "NO Ranking"
                    if (completedCoursesCount == 0)
                    {
                        return "NO Ranking";
                    }

                    // Sử dụng ROW_NUMBER() để xếp hạng người dùng dựa trên số lượng khóa học đã hoàn thành
                    command = connection.CreateCommand();
                    command.CommandText = @"
                SELECT ranked_users.ranking
                FROM (
                    SELECT a.user_id,
                           ROW_NUMBER() OVER(ORDER BY COUNT(lc.lesson_id) DESC) AS ranking
                    FROM account a
                    LEFT JOIN lesson_completion lc ON a.user_id = lc.user_id
                    GROUP BY a.user_id
                ) AS ranked_users
                WHERE ranked_users.user_id = @UserId";
                    command.Parameters.Add(new SqlParameter("@UserId", userId));

                    var result = await command.ExecuteScalarAsync();

                    // Nếu tìm thấy thứ hạng, trả về giá trị
                    if (result != null && int.TryParse(result.ToString(), out int rank))
                    {
                        return rank.ToString();
                    }
                    else
                    {
                        return "NO Ranking";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user rank.");
                throw;
            }
        }


        public async Task<List<string>> GetAdminEmailsAsync()
        {
            var adminEmails = new List<string>();

            try
            {
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand("SELECT user_email FROM account WHERE user_role = 1", connection);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var email = reader["user_email"].ToString();
                            if (email != null)
                            {
                                adminEmails.Add(email);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin emails.");
                throw;
            }

            return adminEmails;
        }

        public async Task<UserRoleCountsViewModel> GetUserRoleCountsAsync()
        {
            var userRoleCounts = new UserRoleCountsViewModel();

            try
            {
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand(@"
                SELECT
                    COUNT(*) AS TotalUsers,
                    SUM(CASE WHEN user_role = 1 THEN 1 ELSE 0 END) AS AdminCount,
                    SUM(CASE WHEN user_role = 2 THEN 1 ELSE 0 END) AS InstructorCount,
                    SUM(CASE WHEN user_role = 3 THEN 1 ELSE 0 END) AS LearnerCount
                FROM account", connection);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            userRoleCounts.TotalUsers = (int)reader["TotalUsers"];
                            userRoleCounts.AdminCount = (int)reader["AdminCount"];
                            userRoleCounts.InstructorCount = (int)reader["InstructorCount"];
                            userRoleCounts.LearnerCount = (int)reader["LearnerCount"];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user role counts.");
                throw;
            }

            return userRoleCounts;
        }


        public string GetMd5Hash(string input)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
                sb.Append(b.ToString("X2"));
            return sb.ToString();
        }

        public class UserRoleCountsViewModel
        {
            public int TotalUsers { get; set; }
            public int AdminCount { get; set; }
            public int InstructorCount { get; set; }
            public int LearnerCount { get; set; }
        }

    }
}