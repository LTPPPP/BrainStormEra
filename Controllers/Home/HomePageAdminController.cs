using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using System.Linq;

namespace BrainStormEra.Controllers
{
    public class HomePageAdminController : Controller
    {
        private readonly SwpDb7Context _context;

        public HomePageAdminController(SwpDb7Context context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Lấy user_id từ session
            string userId = HttpContext.Session.GetString("user_id");

            // Kiểm tra nếu user_id tồn tại
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LoginPage", "Login");
            }

            // Lấy thông tin user từ database
            var user = _context.Accounts.FirstOrDefault(u => u.UserId == userId);

            if (user != null)
            {
                // Truyền tên và hình ảnh của người dùng vào View
                ViewBag.FullName = user.FullName;
                ViewBag.UserPicture = user.UserPicture;
            }

            return View();
        }
    }
}
