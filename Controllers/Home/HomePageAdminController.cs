using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BrainStormEra.Repo;
using BrainStormEra.Repo.Admin;
using System.Security.Claims;

namespace BrainStormEra.Controllers
{
    public class HomePageAdminController : Controller
    {
        private readonly AdminRepo _adminRepo;

        public HomePageAdminController(AdminRepo adminRepo)
        {
            _adminRepo = adminRepo;
        }

        [HttpGet]
        public async Task<IActionResult> HomepageAdmin()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != null)
                {
                    var (fullName, userPicture) = await _adminRepo.GetUserDetailsAsync(userId);
                    ViewBag.FullName = fullName;
                    ViewBag.UserPicture = userPicture;

                    return View("~/Views/Home/HomePageAdmin.cshtml");
                }
            }
            return RedirectToAction("LoginPage", "Login");
        }

        [HttpGet]
        public async Task<IActionResult> GetUserStatistics()
        {
            try
            {
                var userStatistics = await _adminRepo.GetUserStatisticsAsync();
                return Json(userStatistics);
            }
            catch
            {
                return StatusCode(500, new { message = "An error occurred while retrieving user statistics." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetConversationStatistics()
        {
            try
            {
                var conversationStatistics = await _adminRepo.GetConversationStatisticsAsync();
                return Json(conversationStatistics);
            }
            catch
            {
                return Json(new { message = "An error occurred while retrieving conversation statistics." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCourseCreationStatistics()
        {
            try
            {
                var courseStatistics = await _adminRepo.GetCourseCreationStatisticsAsync();
                return Json(courseStatistics);
            }
            catch
            {
                return Json(new { message = "An error occurred while retrieving course statistics." });
            }
        }
    }
}
