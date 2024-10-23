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
        private readonly SwpDb7Context _dbContext; // Inject your DbContext here

        public ChatbotController(GeminiApiService geminiApiService, SwpDb7Context dbContext)
        {
            _geminiApiService = geminiApiService;
            _dbContext = dbContext; // Assign the context
        }

        public IActionResult Chat()
        {
            return View();
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

                // Generate ConversationId based on UserId and conversation count
                chatbotConversation.ConversationId = chatbotConversation.UserId + "-" + (userConversationCount + 1);
                chatbotConversation.ConversationTime = DateTime.Now; // Set the conversation time

                // Save the user's message to the database
                _dbContext.ChatbotConversations.Add(chatbotConversation);
                await _dbContext.SaveChangesAsync();

                // Get the response from the Gemini API
                var reply = await _geminiApiService.GetResponseFromGemini(chatbotConversation.ConversationContent);

                // Save the bot's reply to the database
                var botConversation = new ChatbotConversation
                {
                    UserId = chatbotConversation.UserId,
                    ConversationId = chatbotConversation.UserId + "-" + (userConversationCount + 2), // For bot reply
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
    }
}
