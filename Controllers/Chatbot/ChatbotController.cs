using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Services;
using BrainStormEra.Models;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace BrainStormEra.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly GeminiApiService _geminiApiService;
        private readonly SwpDb7Context _dbContext;

        public ChatbotController(GeminiApiService geminiApiService, SwpDb7Context dbContext)
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
                .GroupBy(c => c.ConversationTime.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            return Json(conversationData);
        }
    }
}
