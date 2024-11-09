using BrainStormEra.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace BrainStormEra.Repo
{
    public class PointsRepo
    {
        private readonly string _connectionString;

        public PointsRepo(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SwpMainContext");
        }


        public async Task<List<Account>> GetLearners(string search)
        {
            var learners = new List<Account>();
            var query = "SELECT * FROM account WHERE user_role = 3 AND user_id LIKE 'LN%'";

            if (!string.IsNullOrEmpty(search))
            {
                query += " AND (user_id LIKE @search OR full_name LIKE @search)";
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(query, connection))
                {
                    if (!string.IsNullOrEmpty(search))
                    {
                        command.Parameters.Add(new SqlParameter("@search", $"%{search}%"));
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var learner = new Account
                            {
                                UserId = reader["user_id"].ToString(),
                                FullName = reader["full_name"].ToString(),
                                PaymentPoint = reader["payment_point"] as decimal?,
                                // Populate other Account properties as needed
                            };
                            learners.Add(learner);
                        }
                    }
                }
            }

            return learners;
        }

        public async Task<string> UpdatePaymentPoints(string userId, decimal newPoints)
        {
            if (newPoints < 1000 || newPoints > 20000000)
            {
                return "The points must be between 1,000 and 20,000,000.";
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Kiểm tra tài khoản người dùng
                var userExistsQuery = "SELECT COUNT(*) FROM account WHERE user_id = @userId";
                using (var userExistsCmd = new SqlCommand(userExistsQuery, connection))
                {
                    userExistsCmd.Parameters.Add(new SqlParameter("@userId", userId));
                    var userExists = (int)await userExistsCmd.ExecuteScalarAsync();
                    if (userExists == 0)
                    {
                        return "User not found!";
                    }
                }

                // Cập nhật điểm thanh toán
                var updatePointsQuery = "UPDATE account SET payment_point = payment_point + @newPoints WHERE user_id = @userId";
                using (var updateCmd = new SqlCommand(updatePointsQuery, connection))
                {
                    updateCmd.Parameters.Add(new SqlParameter("@newPoints", newPoints));
                    updateCmd.Parameters.Add(new SqlParameter("@userId", userId));
                    await updateCmd.ExecuteNonQueryAsync();
                }

                // Tạo PaymentId mới
                var maxPaymentIdQuery = "SELECT TOP 1 payment_id FROM payment ORDER BY payment_id DESC";
                string newPaymentId = "PA001";
                using (var maxPaymentIdCmd = new SqlCommand(maxPaymentIdQuery, connection))
                {
                    var maxPaymentId = await maxPaymentIdCmd.ExecuteScalarAsync() as string;
                    if (!string.IsNullOrEmpty(maxPaymentId) && int.TryParse(maxPaymentId.Substring(2), out int idNumber))
                    {
                        newPaymentId = $"PA{idNumber + 1:D3}";
                    }
                }

                // Ghi log thanh toán
                var insertPaymentQuery = "INSERT INTO payment (payment_id, user_id, payment_description, amount, points_earned, payment_status, payment_date) " +
                                         "VALUES (@paymentId, @userId, @description, @amount, @pointsEarned, 'Completed', @paymentDate)";
                using (var insertCmd = new SqlCommand(insertPaymentQuery, connection))
                {
                    insertCmd.Parameters.Add(new SqlParameter("@paymentId", newPaymentId));
                    insertCmd.Parameters.Add(new SqlParameter("@userId", userId));
                    insertCmd.Parameters.Add(new SqlParameter("@description", $"{userId} - {newPoints:N0} points update"));
                    insertCmd.Parameters.Add(new SqlParameter("@amount", newPoints));
                    insertCmd.Parameters.Add(new SqlParameter("@pointsEarned", (int)newPoints));
                    insertCmd.Parameters.Add(new SqlParameter("@paymentDate", DateTime.Now));
                    await insertCmd.ExecuteNonQueryAsync();
                }
            }

            return "Payment points updated and transaction logged successfully!";
        }
    }
}
