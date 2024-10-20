using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Services;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace BrainStormEra.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly GeminiApiService _geminiApiService;

        public ChatbotController(GeminiApiService geminiApiService)
        {
            _geminiApiService = geminiApiService;
        }

        public IActionResult Chat()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessage chatMessage)
        {
            if (chatMessage == null || string.IsNullOrEmpty(chatMessage.Message))
            {
                return BadRequest(new { error = "Invalid message" });
            }

            try
            {
                var reply = await _geminiApiService.GetResponseFromGemini(chatMessage.Message);
                return Json(new { reply });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to get response from Gemini API", details = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult LogChat([FromBody] ChatLogEntry logEntry)
        {
            if (logEntry == null)
            {
                return BadRequest(new { error = "Invalid log entry" });
            }

            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "Json", "Chat.json");
                var chatLog = new List<ChatLogEntry>();

                if (System.IO.File.Exists(filePath))
                {
                    var jsonContent = System.IO.File.ReadAllText(filePath);
                    chatLog = JsonConvert.DeserializeObject<List<ChatLogEntry>>(jsonContent) ?? new List<ChatLogEntry>();
                }

                chatLog.Add(logEntry);
                var updatedJsonContent = JsonConvert.SerializeObject(chatLog, Formatting.Indented);
                System.IO.File.WriteAllText(filePath, updatedJsonContent);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to log chat", details = ex.Message });
            }
        }
    }

    public class ChatLogEntry
    {
        public string Timestamp { get; set; }
        public string Sender { get; set; }
        public string Message { get; set; }
    }

    public class ChatMessage
    {
        public string Message { get; set; }
    }
}