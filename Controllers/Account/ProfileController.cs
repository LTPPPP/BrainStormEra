using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;

namespace BrainStormEra.Controllers.Account
{
    public class ProfileController : Controller
    {
        private readonly SwpDb7Context _context;

        public ProfileController(SwpDb7Context context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var userId = Request.Cookies["user_id"];

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LoginPage", "Login"); // Chuyển hướng về trang đăng nhập nếu không có session
            }
            var account = _context.Accounts.FirstOrDefault(a => a.UserId == userId);

            if (account == null)
            {
                return NotFound(); // Nếu không tìm thấy tài khoản thì trả về lỗi NotFound
            }

            return View(account); // Truyền thông tin tài khoản đến view để hiển thị
        }

        public IActionResult RedirectToHome()
        {
            // Retrieve the user_id and user_role from cookies
            var userId = Request.Cookies["user_id"];
            var userRole = Request.Cookies["user_role"];

            // Check if user_id or user_role is missing, redirect to login page if necessary
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
            {
                return RedirectToAction("LoginPage", "Login");
            }

            // Redirect based on the user's role
            switch (userRole)
            {
                case "1":  // Admin role
                    return RedirectToAction("HomepageAdmin", "HomePageAdmin");
                case "2":  // Instructor role
                    return RedirectToAction("HomePageInstructor", "HomePageInstructor");
                case "3":  // Learner role
                    return RedirectToAction("HomePageLearner", "HomePageLearner");
                default:   // Unknown role, redirect to login page
                    return RedirectToAction("LoginPage", "Login");
            }
        }


        [HttpGet]
        public IActionResult Edit()
        {

            var userId = Request.Cookies["user_id"];

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LoginPage", "Login"); // Chuyển hướng về trang đăng nhập nếu không có session
            }
            var account = _context.Accounts.FirstOrDefault(a => a.UserId == userId);

            if (account == null)
            {
                return NotFound(); // Nếu không tìm thấy tài khoản thì trả về lỗi NotFound
            }

            return View(account); // Truyền thông tin tài khoản đến view để hiển thị
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(BrainStormEra.Models.Account account, IFormFile avatar)
        {
            if (ModelState.IsValid)
            {
                var accountInDb = _context.Accounts.FirstOrDefault(a => a.UserId == account.UserId);
                if (accountInDb == null) return NotFound();

                // Cập nhật các trường thông tin khác của tài khoản
                accountInDb.FullName = account.FullName;
                accountInDb.UserEmail = account.UserEmail;
                accountInDb.PhoneNumber = account.PhoneNumber;
                accountInDb.Gender = account.Gender;
                accountInDb.UserAddress = account.UserAddress;
                accountInDb.DateOfBirth = account.DateOfBirth;

                // Xử lý logic giữ nguyên avatar cũ nếu không có ảnh mới
                if (avatar != null && avatar.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                    // Tạo thư mục uploads nếu chưa có
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Path.GetFileName(avatar.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        avatar.CopyTo(stream);
                    }

                    // Cập nhật đường dẫn ảnh mới vào cơ sở dữ liệu
                    accountInDb.UserPicture = $"/uploads/{fileName}";
                }
                // Không cập nhật avatar nếu không có ảnh mới
                else
                {
                    accountInDb.UserPicture = accountInDb.UserPicture; // Giữ nguyên avatar hiện tại
                }

                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(account);
        }

    }
}
