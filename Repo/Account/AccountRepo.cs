using BrainStormEra.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace BrainStormEra.Repo
{
    public class AccountRepo
    {
        private readonly SwpMainContext _context;

        public AccountRepo(SwpMainContext context)
        {
            _context = context;
        }

        // Đăng nhập người dùng
        public async Task<Account?> Login(string username, string hashedPassword)
        {
            Account? user = null;
            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM account WHERE username = @username AND password = @password";
                    command.Parameters.Add(new SqlParameter("@username", SqlDbType.NVarChar) { Value = username });
                    command.Parameters.Add(new SqlParameter("@password", SqlDbType.NVarChar) { Value = hashedPassword });

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            user = new Account
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
            }
            return user;
        }

        // Đăng ký tài khoản mới
        public async Task Register(Account newAccount)
        {
            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"INSERT INTO account (user_id, user_role, username, password, user_email, full_name, account_created_at)
                                            VALUES (@UserId, @UserRole, @Username, @Password, @UserEmail, @FullName, @AccountCreatedAt)";

                    command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.NVarChar) { Value = newAccount.UserId });
                    command.Parameters.Add(new SqlParameter("@UserRole", SqlDbType.Int) { Value = newAccount.UserRole ?? (object)DBNull.Value });
                    command.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar) { Value = newAccount.Username });
                    command.Parameters.Add(new SqlParameter("@Password", SqlDbType.NVarChar) { Value = newAccount.Password });
                    command.Parameters.Add(new SqlParameter("@UserEmail", SqlDbType.NVarChar) { Value = newAccount.UserEmail });
                    command.Parameters.Add(new SqlParameter("@FullName", SqlDbType.NVarChar) { Value = newAccount.FullName ?? (object)DBNull.Value });
                    command.Parameters.Add(new SqlParameter("@AccountCreatedAt", SqlDbType.DateTime) { Value = newAccount.AccountCreatedAt });

                    await command.ExecuteNonQueryAsync();
                }
            }
        }


    }
}
