using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace BrainStormEra.Controllers.Account
{
    public class ProfileController : Controller
    {
        private readonly SwpMainFpContext _context;

        public ProfileController(SwpMainFpContext context)
        {
            _context = context;
        }

        // Định nghĩa ViewModel ngay bên trong Controller
        public class ProfileEditViewModel
        {
            public string UserId { get; set; }
            public string? FullName { get; set; }
            public string? UserEmail { get; set; }
            public string? PhoneNumber { get; set; }
            public string? Gender { get; set; }
            public string? UserAddress { get; set; }
            public DateOnly? DateOfBirth { get; set; }
            public string? UserPicture { get; set; }
        }

        // Action để hiển thị trang profile
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

        // Action để chỉnh sửa hồ sơ
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

            // Khởi tạo ViewModel và map dữ liệu từ Account
            var model = new ProfileEditViewModel
            {
                UserId = account.UserId,
                FullName = account.FullName,
                UserEmail = account.UserEmail,
                PhoneNumber = account.PhoneNumber,
                Gender = account.Gender,
                UserAddress = account.UserAddress,
                DateOfBirth = account.DateOfBirth,
                UserPicture = account.UserPicture
            };

            return View(model);  // Trả về View cùng với model
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProfileEditViewModel model, IFormFile avatar)
        {
            var userId = Request.Cookies["user_id"];

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LoginPage", "Login");
            }

            if (ModelState.IsValid || avatar == null)
            {
                var accountInDb = _context.Accounts.FirstOrDefault(a => a.UserId == userId);
                if (accountInDb == null)
                {
                    return NotFound();
                }

                // Cập nhật thông tin từ ViewModel vào Account
                accountInDb.FullName = model.FullName;
                accountInDb.UserEmail = model.UserEmail;
                accountInDb.PhoneNumber = model.PhoneNumber;
                accountInDb.Gender = model.Gender;
                accountInDb.UserAddress = model.UserAddress;
                accountInDb.DateOfBirth = model.DateOfBirth;

                // Xử lý việc upload avatar nếu có
                if (avatar != null)
                {
                    // Kiểm tra loại file (chỉ cho phép .png, .jpeg)
                    var validTypes = new[] { "image/png", "image/jpeg" };
                    if (!validTypes.Contains(avatar.ContentType))
                    {
                        ModelState.AddModelError("avatar", "Chỉ chấp nhận tệp PNG và JPEG.");
                        return View(model); // Không lưu file và trả về view với lỗi
                    }

                    // Kiểm tra kích thước file (giới hạn 2MB)
                    if (avatar.Length > 2 * 1024 * 1024) // 2MB
                    {
                        ModelState.AddModelError("avatar", "Kích thước tệp không được vượt quá 2MB.");
                        return View(model); // Không lưu file và trả về view với lỗi
                    }

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads","User-img");

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

                    // Cập nhật đường dẫn ảnh vào database
                    accountInDb.UserPicture = $"/uploads/User-img/{fileName}";

                }

                // Lưu thay đổi vào database
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(model);  // Trả về view cùng với model nếu có lỗi
        }

        // Action để điều hướng về trang chủ dựa trên vai trò của user
        public IActionResult RedirectToHome()
        {
            var userIdCookie = Request.Cookies["user_id"];
            var userRoleCookie = Request.Cookies["user_role"];

            if (userIdCookie != null && userRoleCookie != null)
            {
                int userRole = int.Parse(userRoleCookie);

                switch (userRole)
                {
                    case 1:
                        return RedirectToAction("HomepageAdmin", "HomePageAdmin");
                    case 2:
                        return RedirectToAction("HomePageInstructor", "HomePageInstructor");
                    case 3:
                        return RedirectToAction("HomePageLearner", "HomePageLearner");
                    default:
                        return RedirectToAction("LoginPage", "Login");
                }
            }

            return RedirectToAction("LoginPage", "Login");
        }
    }
}
