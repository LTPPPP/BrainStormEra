using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Services;
using BrainStormEra.Models;
using BrainStormEra.Repo.Chatbot;
using BrainStormEra.ViewModels;
using BrainStormEra.Repo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrainStormEra.Repo.Course;
using BrainStormEra.Repo.Chapter;
using OpenQA.Selenium.DevTools.V127.Database;

namespace BrainStormEra.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly GeminiApiService _geminiApiService;
        private readonly ChatbotRepo _chatbotRepo;
        private readonly CourseRepo _courseRepo;
        private readonly ChapterRepo _chapterRepo;
        private readonly LessonRepo _lessonRepo;

        public ChatbotController(GeminiApiService geminiApiService, ChatbotRepo chatbotRepo, CourseRepo courseRepo, ChapterRepo chapterRepo, LessonRepo lessonRepo)
        {
            _geminiApiService = geminiApiService;
            _chatbotRepo = chatbotRepo;
            _courseRepo = courseRepo;
            _chapterRepo = chapterRepo;
            _lessonRepo = lessonRepo;
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
                if (!int.TryParse(Request.Cookies["user_role"], out int userRole))
                {
                    userRole = 2;
                }

                // Lấy ConversationId lớn nhất và tăng thêm 1
                var maxConversationId = await _chatbotRepo.GetMaxConversationIdAsync();
                chatbotConversation.ConversationId = (maxConversationId + 1).ToString();
                chatbotConversation.ConversationTime = DateTime.Now;

                await _chatbotRepo.AddConversationAsync(chatbotConversation);

                string reply;
                var courseId = HttpContext.Request.Cookies["CourseId"];
                var lessonId = HttpContext.Request.Cookies["LessonId"];
                var chapterID = HttpContext.Request.Cookies["ChapterId"];
                if (userRole == 3)
                {
                    var course = await _courseRepo.GetCourseByIdAsync(courseId);
                    if (course == null)
                    {
                        return BadRequest(new { error = "course not found" });
                    }
                    var chapter = await _chapterRepo.GetChapterByIdAsync(chapterID);
                    if (chapter == null)
                    {
                        return BadRequest(new { error = "chapter not found" });
                    }
                    var lesson = await _lessonRepo.GetLessonByIdAsync(lessonId);
                    reply = await _geminiApiService.GetResponseFromGemini(
                        chatbotConversation.ConversationContent,
                        userRole,
                        course.CourseName,
                        course.CreatedBy,
                        course.CourseDescription,
                        chapter.ChapterName,
                        chapter.ChapterDescription,
                        lesson.LessonName,
                        lesson.LessonDescription,
                        lesson.LessonContent
                    );
                }
                else
                {
                    try
                    {
                        reply = await _geminiApiService.GetResponseFromGemini(chatbotConversation.ConversationContent, userRole, " ", " ", " ", " ", " ", " ", " ", " ");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        reply = "Xin lỗi bạn, đã xảy ra lỗi khi xử lý câu hỏi của bạn. Vui lòng thử lại sau.";
                    }
                }

                var botConversation = new ChatbotConversation
                {
                    UserId = chatbotConversation.UserId,
                    ConversationId = (maxConversationId + 2).ToString(), // Conversation của bot sẽ là max + 2
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

        public async Task<IActionResult> ConversationHistory(int page = 1)
        {
            try
            {
                int pageSize = 1; // Mỗi trang hiển thị hội thoại của 1 ngày
                var conversationDates = await _chatbotRepo.GetDistinctConversationDatesAsync();

                var totalPages = conversationDates.Count;
                if (page > totalPages || page < 1)
                {
                    page = 1; // Reset về trang đầu tiên nếu page vượt quá giới hạn
                }

                var selectedDate = conversationDates[page - 1]; // Lấy ngày tương ứng với trang hiện tại
                var conversations = await _chatbotRepo.GetConversationsByDateAsync(selectedDate);

                var conversationViewModels = conversations.Select(c => new ConversationViewModel
                {
                    ConversationId = c.ConversationId,
                    UserName = c.User?.FullName ?? "Guest",
                    ConversationTime = c.ConversationTime,
                    ConversationContent = c.ConversationContent
                }).ToList();

                var totalConversations = await _chatbotRepo.GetTotalConversationCountAsync();

                var viewModel = new ConversationHistoryViewModel
                {
                    DailyConversations = new List<DailyConversationViewModel>
            {
                new DailyConversationViewModel
                {
                    Date = selectedDate,
                    Conversations = conversationViewModels
                }
            },
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalConversations = totalConversations // Thêm tổng số cuộc hội thoại vào view model
                };

                return View("~/Views/Shared/Chatbot/ConversationHistory.cshtml", viewModel);
            }
            catch (Exception ex)
            {
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

    public class ConversationHistoryViewModel
    {
        public List<DailyConversationViewModel> DailyConversations { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalConversations { get; set; } // Thêm thuộc tính này
    }

    public class DailyConversationViewModel
    {
        public DateTime Date { get; set; }
        public List<ConversationViewModel> Conversations { get; set; }
    }

}
