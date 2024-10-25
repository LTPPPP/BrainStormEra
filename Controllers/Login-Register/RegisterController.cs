using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BrainStormEra.Views.Register;

public class RegisterController : Controller
{
    private readonly SwpMainFpContext _context;
    private readonly EmailService _emailService;

    public RegisterController(SwpMainFpContext context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
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
            // Kiểm tra Password và ConfirmPassword
            if (model.Password != model.ConfirmPassword)
            {
                ViewBag.ErrorMessage = "Password và Confirm Password không khớp.";
                return View(model);
            }

            // Kiểm tra tồn tại của username và email
            bool isUsernameExist = _context.Accounts.Any(a => a.Username == model.Username);
            bool isEmailExist = _context.Accounts.Any(a => a.UserEmail == model.Email);

            if (isUsernameExist || isEmailExist)
            {
                ViewBag.ErrorMessage = isUsernameExist ? "Username đã tồn tại." : "Email đã tồn tại.";
                return View(model);
            }

            // Nếu hợp lệ, thêm vào database
            Account newAccount = new Account
            {
                UserId = Guid.NewGuid().ToString(),
                Username = model.Username,
                Password = HashPasswordMD5(model.Password),
                UserRole = 3,
                UserEmail = model.Email,
                AccountCreatedAt = DateTime.Now
            };

            _context.Accounts.Add(newAccount);
            _context.SaveChanges();

            return RedirectToAction("LoginPage", "Login");
        }

        return View(model);
    }

    [HttpPost]
    public IActionResult SendOTP(RegisterModel model)
    {
        // Kiểm tra các trường bắt buộc
        if (string.IsNullOrWhiteSpace(model.Username) ||
            string.IsNullOrWhiteSpace(model.Email) ||
            string.IsNullOrWhiteSpace(model.Password) ||
            string.IsNullOrWhiteSpace(model.ConfirmPassword))
        {
            ViewBag.ErrorMessage = "Vui lòng điền đầy đủ các thông tin trước khi gửi OTP.";
            return View("Register", model);
        }

        try
        {
            var otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("OTP", otp);
            HttpContext.Session.SetString("OTPCreationTime", DateTime.Now.ToString());

            // Gửi OTP qua email
            _emailService.SendOTPEmail(model.Email, otp);
            ViewBag.SuccessMessage = "OTP đã được gửi đến email của bạn.";
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = "Đã xảy ra lỗi khi gửi OTP. Vui lòng thử lại.";
            // Log lỗi nếu cần thiết
        }

        return View("Register", model);
    }

    [HttpPost]
    public IActionResult VerifyOTP(string otp)
    {
        try
        {
            var sessionOtp = HttpContext.Session.GetString("OTP");
            var creationTimeString = HttpContext.Session.GetString("OTPCreationTime");

            if (sessionOtp == null || creationTimeString == null)
            {
                ViewBag.ErrorMessage = "OTP không hợp lệ hoặc đã hết hạn.";
                return View("Register");
            }

            var creationTime = DateTime.Parse(creationTimeString);
            if (sessionOtp == otp && DateTime.Now < creationTime.AddSeconds(90))
            {
                HttpContext.Session.Remove("OTP");
                HttpContext.Session.Remove("OTPCreationTime");
                return RedirectToAction("LoginPage", "Login");
            }

            ViewBag.ErrorMessage = "Mã OTP không đúng hoặc đã hết hạn.";
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = "Đã xảy ra lỗi khi xác thực OTP. Vui lòng thử lại.";
            // Log lỗi nếu cần thiết
        }

        return View("Register");
    }


    private string HashPasswordMD5(string password)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(password);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            return string.Concat(hashBytes.Select(b => b.ToString("X2")));
        }
    }
}
