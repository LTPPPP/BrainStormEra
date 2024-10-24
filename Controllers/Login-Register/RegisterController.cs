using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using BrainStormEra.Views.Register;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public class RegisterController : Controller
{
    private readonly SwpMainFpContext _context;

    public RegisterController(SwpMainFpContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(RegisterModel model)
    {
        if (ModelState.IsValid)
        {
            // Kiểm tra xem Password và ConfirmPassword đã được sử dụng chưa
            if (model.Password != model.ConfirmPassword)
            {
                ViewBag.ErrorMessage = "Password và Confirm Password không khớp.";
                return View(model);
            }

            // Kiểm tra xem email và username đã tồn tại chưa
            bool isUsernameExist = _context.Accounts.Any(a => a.Username == model.Username);
            bool isEmailExist = _context.Accounts.Any(a => a.UserEmail == model.Email);

            if (isUsernameExist)
            {
                ViewBag.ErrorMessage = "Username đã tồn tại.";
                return View(model);
            }

            if (isEmailExist)
            {
                ViewBag.ErrorMessage = "Email đã tồn tại.";
                return View(model);
            }

            // Nếu hợp lệ, thêm dữ liệu vào database
            Account newAccount = new Account
            {
                UserId = Guid.NewGuid().ToString(),
                Username = model.Username,
                Password = HashPasswordMD5(model.Password), 
                UserRole = 3, // 3: Vai trò của User bình thường
                UserEmail = model.Email,
                AccountCreatedAt = DateTime.Now
            };

            _context.Accounts.Add(newAccount);
            _context.SaveChanges();

            // Quay lại trang login
            return RedirectToAction("LoginPage", "Login");
        }

        // Nếu model không hợp lệ, trả lại form
        return View(model);
    }

    private string HashPasswordMD5(string password)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(password);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
