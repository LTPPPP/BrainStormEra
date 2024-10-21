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
            var userId = HttpContext.Session.GetString("user_id");

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


        [HttpGet]
        public IActionResult Edit()
        {

            var userId = HttpContext.Session.GetString("user_id");

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
