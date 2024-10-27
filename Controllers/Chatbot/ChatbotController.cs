using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Services;
using BrainStormEra.Models;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;
using BrainStormEra.ViewModels;

namespace BrainStormEra.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly GeminiApiService _geminiApiService;
        private readonly SwpMainContext _dbContext;

        public ChatbotController(GeminiApiService geminiApiService, SwpMainContext dbContext)
        {
            _geminiApiService = geminiApiService;
            _dbContext = dbContext;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatbotConversation chatbotConversation)
        {
            if (chatbotConversation == null || string.IsNullOrEmpty(chatbotConversation.ConversationContent))
            {
                return BadRequest(new { error = "Invalid message" });
            }

            try
            {
                var userConversationCount = _dbContext.ChatbotConversations
                    .Where(c => c.UserId == chatbotConversation.UserId)
                    .Count();

                chatbotConversation.ConversationId = chatbotConversation.UserId + "-" + (userConversationCount + 1);
                chatbotConversation.ConversationTime = DateTime.Now;
                _dbContext.ChatbotConversations.Add(chatbotConversation);
                await _dbContext.SaveChangesAsync();

                var reply = await _geminiApiService.GetResponseFromGemini(chatbotConversation.ConversationContent);

                var botConversation = new ChatbotConversation
                {
                    UserId = chatbotConversation.UserId,
                    ConversationId = chatbotConversation.UserId + "-" + (userConversationCount + 2),
                    ConversationTime = DateTime.Now,
                    ConversationContent = reply
                };
                _dbContext.ChatbotConversations.Add(botConversation);
                await _dbContext.SaveChangesAsync();

                return Json(new { reply });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to get response from Gemini API", details = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetConversationStatistics()
        {
            var conversationData = _dbContext.ChatbotConversations
                .GroupBy(c => c.ConversationTime.Date) // Group by Date, ignoring the time
                .Select(g => new
                {
                    Date = g.Key,  // Group by the raw DateTime first
                    Count = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList()
                .Select(d => new  // After retrieving the data, format the date on the client side
                {
                    Date = d.Date.ToString("yyyy-MM-dd"),  // Format the date as "yyyy-MM-dd"
                    Count = d.Count
                })
                .ToList();

            return Json(conversationData);
        }
        public IActionResult ConversationHistory(int page = 1, int pageSize = 10)
        {
            var totalConversations = _dbContext.ChatbotConversations.Count();
            var totalPages = (int)Math.Ceiling((double)totalConversations / pageSize);

            var conversations = _dbContext.ChatbotConversations
                .OrderByDescending(c => c.ConversationTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ConversationViewModel
                {
                    ConversationId = c.ConversationId,
                    UserName = c.User != null ? c.User.FullName : "Guest",
                    ConversationTime = c.ConversationTime,
                    ConversationContent = c.ConversationContent
                })
                .ToList();

            var viewModel = new ConversationViewModel
            {
                Conversations = conversations,
                CurrentPage = page,
                TotalPages = totalPages
            };

            return View("~/Views/Shared/Chatbot/ConversationHistory.cshtml", viewModel);
        }

    }
}
