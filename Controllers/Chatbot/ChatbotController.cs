using Microsoft.AspNetCore.Mvc;

namespace BrainStormEra.Controllers.Chatbot
{
    public class ChatbotController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
