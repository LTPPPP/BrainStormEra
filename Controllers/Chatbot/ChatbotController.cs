using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Services;
using BrainStormEra.Models;
using BrainStormEra.Repo.Chatbot;
using BrainStormEra.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace BrainStormEra.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly GeminiApiService _geminiApiService;
        private readonly ChatbotRepo _chatbotRepo;

        public ChatbotController(GeminiApiService geminiApiService, ChatbotRepo chatbotRepo)
        {
            _geminiApiService = geminiApiService;
            _chatbotRepo = chatbotRepo;
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
                var userConversationCount = await _chatbotRepo.GetUserConversationCountAsync(chatbotConversation.UserId);

                chatbotConversation.ConversationId = $"{chatbotConversation.UserId}-{userConversationCount + 1}";
                chatbotConversation.ConversationTime = DateTime.Now;
                await _chatbotRepo.AddConversationAsync(chatbotConversation);

                var reply = await _geminiApiService.GetResponseFromGemini(chatbotConversation.ConversationContent);

                var botConversation = new ChatbotConversation
                {
                    UserId = chatbotConversation.UserId,
                    ConversationId = $"{chatbotConversation.UserId}-{userConversationCount + 2}",
                    ConversationTime = DateTime.Now,
                    ConversationContent = reply
                };
                await _chatbotRepo.AddConversationAsync(botConversation);

                return Json(new { reply });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to get response from Gemini API", details = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetConversationStatistics()
        {
            try
            {
                var conversationData = await _chatbotRepo.GetConversationStatisticsAsync();
                var formattedData = conversationData.Select(d => new
                {
                    Date = d.Date.ToString("yyyy-MM-dd"),
                    d.Count
                });

                return Json(formattedData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve conversation statistics", details = ex.Message });
            }
        }

        public async Task<IActionResult> ConversationHistory(int page = 1, int pageSize = 10)
        {
            try
            {
                var totalConversations = await _chatbotRepo.GetTotalConversationCountAsync();
                var totalPages = (int)Math.Ceiling((double)totalConversations / pageSize);
                var offset = (page - 1) * pageSize;

                var conversations = await _chatbotRepo.GetPaginatedConversationsAsync(offset, pageSize);
                var conversationViewModels = conversations.Select(c => new ConversationViewModel
                {
                    ConversationId = c.ConversationId,
                    UserName = c.User?.FullName ?? "Guest",
                    ConversationTime = c.ConversationTime,
                    ConversationContent = c.ConversationContent
                }).ToList();

                var viewModel = new ConversationHistoryViewModel
                {
                    Conversations = conversationViewModels,
                    CurrentPage = page,
                    TotalPages = totalPages
                };

                return View("~/Views/Shared/Chatbot/ConversationHistory.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                // Log the error here if you have logging configured
                return View("Error", new ErrorViewModel { Message = "Failed to load conversation history." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConversations([FromBody] List<string> conversationIds)
        {
            if (conversationIds == null || !conversationIds.Any())
            {
                return BadRequest(new { error = "No conversations selected" });
            }

            try
            {
                await _chatbotRepo.DeleteConversationsByIdsAsync(conversationIds);
                return Ok(new { success = "Selected conversations deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete conversations", details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAllConversations()
        {
            try
            {
                await _chatbotRepo.DeleteAllConversationsAsync();
                return Ok(new { success = "All conversations deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete conversations", details = ex.Message });
            }
        }
    }

    // Add this class to your ViewModels folder
    public class ConversationHistoryViewModel
    {
        public List<ConversationViewModel> Conversations { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}