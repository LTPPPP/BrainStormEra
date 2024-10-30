using BrainStormEra.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading.Tasks;

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
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand(@"INSERT INTO account (user_id, user_role, username, password, user_email, full_name, account_created_at)
                                                  VALUES (@UserId, @UserRole, @Username, @Password, @UserEmail, @FullName, @AccountCreatedAt)", connection);

                    command.Parameters.AddWithValue("@UserId", newAccount.UserId);
                    command.Parameters.AddWithValue("@UserRole", newAccount.UserRole ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Username", newAccount.Username);
                    command.Parameters.AddWithValue("@Password", newAccount.Password);
                    command.Parameters.AddWithValue("@UserEmail", newAccount.UserEmail);
                    command.Parameters.AddWithValue("@FullName", newAccount.FullName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@AccountCreatedAt", newAccount.AccountCreatedAt);

                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering new account.");
                throw;
            }
        }
    }
}
