using BrainStormEra.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BrainStormEra.Repo.Chatbot
{
    public class ChatbotRepo
    {
        private readonly SwpMainContext _dbContext;

        public ChatbotRepo(SwpMainContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> GetUserConversationCountAsync(string userId)
        {
            return await _dbContext.ChatbotConversations
                .FromSqlRaw("SELECT COUNT(*) FROM ChatbotConversations WHERE UserId = {0}", userId)
                .CountAsync();
        }

        public async Task AddConversationAsync(ChatbotConversation conversation)
        {
            await _dbContext.ChatbotConversations.AddAsync(conversation);
            await _dbContext.SaveChangesAsync();
        }

        public IEnumerable<dynamic> GetConversationStatistics()
        {
            return _dbContext.ChatbotConversations
                .FromSqlRaw(@"
                    SELECT 
                        CAST(ConversationTime AS DATE) AS Date, 
                        COUNT(*) AS Count 
                    FROM 
                        chatbot_conversation 
                    GROUP BY 
                        CAST(ConversationTime AS DATE)
                    ORDER BY 
                        Date")
                .AsEnumerable()
                .Select(d => new
                {
                    Date = d.ConversationTime.ToString("yyyy-MM-dd"),
                });
        }

        public async Task<int> GetTotalConversationCountAsync()
        {
            return await _dbContext.ChatbotConversations
                .FromSqlRaw("SELECT COUNT(*) FROM ChatbotConversations")
                .CountAsync();
        }

        public IEnumerable<ChatbotConversation> GetPaginatedConversations(int offset, int pageSize)
        {
            return _dbContext.ChatbotConversations
                .FromSqlRaw($"SELECT * FROM ChatbotConversations ORDER BY ConversationTime DESC OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY")
                .ToList();
        }

        public async Task DeleteConversationsByIdsAsync(List<string> conversationIds)
        {
            var conversationIdsString = string.Join(",", conversationIds.Select(id => $"'{id}'"));
            await _dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM ChatbotConversations WHERE ConversationId IN ({conversationIdsString})");
        }

        public async Task DeleteAllConversationsAsync()
        {
            await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM ChatbotConversations");
        }
    }
}
