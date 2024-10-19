using Microsoft.AspNetCore.Mvc;

namespace BrainStormEra.Controllers.Chatbot
{
    public class GeminiApiService : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
