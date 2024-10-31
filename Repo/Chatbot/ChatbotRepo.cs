using BrainStormEra.Models;
using BrainStormEra.ViewModels;
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
                .Where(c => c.UserId == userId)
                .CountAsync();
        }

        public async Task AddConversationAsync(ChatbotConversation conversation)
        {
            await _dbContext.ChatbotConversations.AddAsync(conversation);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<ConversationStatistics>> GetConversationStatisticsAsync()
        {
            return await _dbContext.ChatbotConversations
                .GroupBy(c => c.ConversationTime.Date)
                .Select(g => new ConversationStatistics
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();
        }

        public async Task<int> GetTotalConversationCountAsync()
        {
            return await _dbContext.ChatbotConversations.CountAsync();
        }

        public async Task<List<ChatbotConversation>> GetPaginatedConversationsAsync(int offset, int pageSize)
        {
            return await _dbContext.ChatbotConversations
                .OrderByDescending(c => c.ConversationTime)
                .Skip(offset)
                .Take(pageSize)
                .Include(c => c.User) // Include user data if needed
                .ToListAsync();
        }

        public async Task DeleteConversationsByIdsAsync(List<string> conversationIds)
        {
            var conversationsToDelete = await _dbContext.ChatbotConversations
                .Where(c => conversationIds.Contains(c.ConversationId))
                .ToListAsync();

            if (!conversationsToDelete.Any())
            {
                throw new InvalidOperationException("No conversations found with the specified IDs");
            }

            _dbContext.ChatbotConversations.RemoveRange(conversationsToDelete);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAllConversationsAsync()
        {
            // Using a more EF Core friendly approach
            await _dbContext.ChatbotConversations
                .ExecuteDeleteAsync();
        }
    }

    // New class to handle statistics
    public class ConversationStatistics
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}