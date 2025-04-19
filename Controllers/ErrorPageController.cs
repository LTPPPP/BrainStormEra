using Microsoft.AspNetCore.Mvc;

namespace BrainStormEra.Controllers
{
    public class ErrorPageController : Controller
    {
        [HttpGet]
        public IActionResult Error(int statusCode)
        {
            ViewBag.StatusCode = statusCode;
            return View();
        }
    }
}
