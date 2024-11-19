using BrainStormEra.Models;
using BrainStormEra.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BrainStormEra.Repo.Chatbot
{
    public class ChatbotRepo
    {
        private readonly string _connectionString;

        public ChatbotRepo(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SwpMainContext");
        }

        public async Task<int> GetUserConversationCountAsync(string userId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string sql = "SELECT COUNT(*) FROM chatbot_conversation WHERE (@UserId IS NULL AND user_id IS NULL) OR (user_id = @UserId)";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);

                    int count = (int)await cmd.ExecuteScalarAsync();

                    return count;
                }
            }
        }

        public async Task AddConversationAsync(ChatbotConversation conversation)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string sql = "INSERT INTO chatbot_conversation (conversation_id, user_id, conversation_time, conversation_content) VALUES (@ConversationId, @UserId, @ConversationTime, @ConversationContent)";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ConversationId", conversation.ConversationId);
                    cmd.Parameters.AddWithValue("@UserId", (object)conversation.UserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ConversationTime", conversation.ConversationTime);
                    cmd.Parameters.AddWithValue("@ConversationContent", conversation.ConversationContent);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<ConversationStatistics>> GetConversationStatisticsAsync()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string sql = @"
                    SELECT CAST(conversation_time AS DATE) AS Date, COUNT(*) AS Count
                    FROM chatbot_conversation
                    GROUP BY CAST(conversation_time AS DATE)
                    ORDER BY Date";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<ConversationStatistics>();
                        while (await reader.ReadAsync())
                        {
                            var stat = new ConversationStatistics
                            {
                                Date = reader.GetDateTime(0),
                                Count = reader.GetInt32(1)
                            };
                            list.Add(stat);
                        }
                        return list;
                    }
                }
            }
        }

        public async Task<int> GetTotalConversationCountAsync()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string sql = "SELECT COUNT(*) FROM chatbot_conversation";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    int count = (int)await cmd.ExecuteScalarAsync();
                    return count;
                }
            }
        }

        public async Task<List<ChatbotConversation>> GetPaginatedConversationsAsync(int offset, int pageSize)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string sql = @"
                    SELECT c.*, a.*
                    FROM chatbot_conversation c
                    LEFT JOIN account a ON c.user_id = a.user_id
                    ORDER BY c.conversation_time DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<ChatbotConversation>();
                        while (await reader.ReadAsync())
                        {
                            var conversation = new ChatbotConversation
                            {
                                ConversationId = reader["conversation_id"].ToString(),
                                UserId = reader["user_id"] == DBNull.Value ? null : reader["user_id"].ToString(),
                                ConversationTime = Convert.ToDateTime(reader["conversation_time"]),
                                ConversationContent = reader["conversation_content"].ToString(),
                                User = reader["user_id"] == DBNull.Value ? null : new Account
                                {
                                    UserId = reader["user_id"].ToString(),
                                    FullName = reader["full_name"]?.ToString(),
                                    // Thêm các thuộc tính khác nếu cần
                                }
                            };
                            list.Add(conversation);
                        }
                        return list;
                    }
                }
            }
        }

        public async Task DeleteConversationsByIdsAsync(List<string> conversationIds)
        {
            if (conversationIds == null || conversationIds.Count == 0)
            {
                throw new InvalidOperationException("No conversations specified");
            }

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                // Kiểm tra xem các hội thoại có tồn tại không
                List<string> parameters = new List<string>();
                for (int i = 0; i < conversationIds.Count; i++)
                {
                    parameters.Add($"@Id{i}");
                }

                string sqlCheck = $"SELECT COUNT(*) FROM chatbot_conversation WHERE conversation_id IN ({string.Join(",", parameters)})";

                using (SqlCommand cmdCheck = new SqlCommand(sqlCheck, conn))
                {
                    for (int i = 0; i < conversationIds.Count; i++)
                    {
                        cmdCheck.Parameters.AddWithValue($"@Id{i}", conversationIds[i]);
                    }

                    int count = (int)await cmdCheck.ExecuteScalarAsync();

                    if (count == 0)
                    {
                        throw new InvalidOperationException("No conversations found with the specified IDs");
                    }
                }

                // Xóa các hội thoại
                string sqlDelete = $"DELETE FROM chatbot_conversation WHERE conversation_id IN ({string.Join(",", parameters)})";

                using (SqlCommand cmdDelete = new SqlCommand(sqlDelete, conn))
                {
                    for (int i = 0; i < conversationIds.Count; i++)
                    {
                        cmdDelete.Parameters.AddWithValue($"@Id{i}", conversationIds[i]);
                    }

                    await cmdDelete.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task DeleteAllConversationsAsync()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string sql = "DELETE FROM chatbot_conversation";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<ChatbotConversation>> GetAllConversationsAsync()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string sql = @"
                    SELECT c.*, a.*
                    FROM chatbot_conversation c
                    LEFT JOIN account a ON c.user_id = a.user_id
                    ORDER BY c.conversation_time DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<ChatbotConversation>();
                        while (await reader.ReadAsync())
                        {
                            var conversation = new ChatbotConversation
                            {
                                ConversationId = reader["conversation_id"].ToString(),
                                UserId = reader["user_id"] == DBNull.Value ? null : reader["user_id"].ToString(),
                                ConversationTime = Convert.ToDateTime(reader["conversation_time"]),
                                ConversationContent = reader["conversation_content"].ToString(),
                                User = reader["user_id"] == DBNull.Value ? null : new Account
                                {
                                    UserId = reader["user_id"].ToString(),
                                    FullName = reader["full_name"]?.ToString(),
                                    // Thêm các thuộc tính khác nếu cần
                                }
                            };
                            list.Add(conversation);
                        }
                        return list;
                    }
                }
            }
        }

        public async Task<List<DateTime>> GetDistinctConversationDatesAsync()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string sql = @"
                    SELECT DISTINCT CAST(conversation_time AS DATE) AS Date
                    FROM chatbot_conversation
                    ORDER BY Date DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<DateTime>();
                        while (await reader.ReadAsync())
                        {
                            list.Add(reader.GetDateTime(0));
                        }
                        return list;
                    }
                }
            }
        }

        public async Task<List<ChatbotConversation>> GetConversationsByDateAsync(DateTime date)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string sql = @"
                    SELECT c.*, a.*
                    FROM chatbot_conversation c
                    LEFT JOIN account a ON c.user_id = a.user_id
                    WHERE CAST(c.conversation_time AS DATE) = @Date
                    ORDER BY c.conversation_time DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Date", date.Date);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<ChatbotConversation>();
                        while (await reader.ReadAsync())
                        {
                            var conversation = new ChatbotConversation
                            {
                                ConversationId = reader["conversation_id"].ToString(),
                                UserId = reader["user_id"] == DBNull.Value ? null : reader["user_id"].ToString(),
                                ConversationTime = Convert.ToDateTime(reader["conversation_time"]),
                                ConversationContent = reader["conversation_content"].ToString(),
                                User = reader["user_id"] == DBNull.Value ? null : new Account
                                {
                                    UserId = reader["user_id"].ToString(),
                                    FullName = reader["full_name"]?.ToString(),
                                    // Thêm các thuộc tính khác nếu cần
                                }
                            };
                            list.Add(conversation);
                        }
                        return list;
                    }
                }
            }
        }

        public async Task<int> GetMaxConversationIdAsync()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                // Lấy phần số cuối cùng của ConversationId mà có thể chuyển đổi sang kiểu INT
                string sql = @"
            SELECT MAX(CAST(SUBSTRING(conversation_id, PATINDEX('%[0-9]%', conversation_id), LEN(conversation_id)) AS INT))
            FROM chatbot_conversation
            WHERE ISNUMERIC(SUBSTRING(conversation_id, PATINDEX('%[0-9]%', conversation_id), LEN(conversation_id))) = 1";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    var result = await cmd.ExecuteScalarAsync();
                    return result != DBNull.Value ? Convert.ToInt32(result) : 0;
                }
            }
        }
        public async Task<List<ChatbotConversation>> GetAggregatedConversationsAsync(DateTime? startDate = null, DateTime? endDate = null, string userId = null)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string sql = @"
            SELECT c.*, a.*
            FROM chatbot_conversation c
            LEFT JOIN account a ON c.user_id = a.user_id
            WHERE (@StartDate IS NULL OR c.conversation_time >= @StartDate)
            AND (@EndDate IS NULL OR c.conversation_time <= @EndDate)
            AND (@UserId IS NULL OR c.user_id = @UserId)
            ORDER BY c.conversation_time DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@StartDate", (object)startDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EndDate", (object)endDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<ChatbotConversation>();
                        while (await reader.ReadAsync())
                        {
                            var conversation = new ChatbotConversation
                            {
                                ConversationId = reader["conversation_id"].ToString(),
                                UserId = reader["user_id"] == DBNull.Value ? null : reader["user_id"].ToString(),
                                ConversationTime = Convert.ToDateTime(reader["conversation_time"]),
                                ConversationContent = reader["conversation_content"].ToString(),
                                User = reader["user_id"] == DBNull.Value ? null : new Account
                                {
                                    UserId = reader["user_id"].ToString(),
                                    FullName = reader["full_name"]?.ToString(),
                                    // Thêm các thuộc tính khác nếu cần
                                }
                            };
                            list.Add(conversation);
                        }
                        return list;
                    }
                }
            }
        }
    }

    // Class để lưu trữ thống kê hội thoại
    public class ConversationStatistics
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}
