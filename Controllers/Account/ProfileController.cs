using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace BrainStormEra.Controllers.Account
{
    public class ProfileController : Controller
    {
        private readonly SwpDb7Context _context;

        public ProfileController(SwpDb7Context context)
        {
            _context = context;
        }

        // Action to display the profile page
        public IActionResult Index()
        {
            var userId = Request.Cookies["user_id"];

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LoginPage", "Login");
            }

            var account = _context.Accounts.FirstOrDefault(a => a.UserId == userId);

            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        // Action to edit profile
        [HttpGet]
        public IActionResult Edit()
        {
            var userId = Request.Cookies["user_id"];

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LoginPage", "Login");
            }

            var account = _context.Accounts.FirstOrDefault(a => a.UserId == userId);

            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        // Action to post profile edits
        // Action to post profile edits
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(BrainStormEra.Models.Account account, IFormFile avatar)
        {
            if (ModelState.IsValid)
            {
                // Lấy thông tin tài khoản người dùng từ database
                var accountInDb = _context.Accounts.FirstOrDefault(a => a.UserId == account.UserId);
                if (accountInDb == null) return NotFound();

                // Cập nhật thông tin người dùng
                accountInDb.FullName = account.FullName;
                accountInDb.UserEmail = account.UserEmail;
                accountInDb.PhoneNumber = account.PhoneNumber;
                accountInDb.Gender = account.Gender;
                accountInDb.UserAddress = account.UserAddress;
                accountInDb.DateOfBirth = account.DateOfBirth;

                // Xử lý avatar mới nếu có
                if (avatar != null && avatar.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                    // Tạo thư mục nếu chưa tồn tại
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Path.GetFileName(avatar.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    // Lưu file avatar vào thư mục uploads
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        avatar.CopyTo(stream);
                    }

                    // Cập nhật đường dẫn ảnh trong database
                    accountInDb.UserPicture = $"/uploads/{fileName}";
                }

                // Lưu các thay đổi vào database
                _context.SaveChanges();

                // Sau khi cập nhật, chuyển hướng về trang profile
                return RedirectToAction("Index");
            }

            // Nếu có lỗi, trả về view hiện tại với các thông tin lỗi
            return View(account);
        }

    }
}
