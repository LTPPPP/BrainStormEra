using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using BrainStormEra.Repo;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text;
using System.Threading.Tasks;
using BrainStormEra.Views.Login;
using BrainStormEra.Services;

namespace BrainStormEra.Controllers
{
    public class LoginController : Controller
    {
        private readonly AccountRepo _accountRepository;
        private readonly EmailService _emailService;
        private readonly OtpService _otpService;

        public LoginController(AccountRepo accountRepository, EmailService emailService, OtpService otpService)
        {
            _accountRepository = accountRepository;
            _emailService = emailService;
            _otpService = otpService;
        }

        [HttpGet]
        public IActionResult LoginPage()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(LoginPageViewModel model)
        {
            if (ModelState.IsValid)
            {
                string hashedPassword = GetMd5Hash(model.Password);

                var user = await _accountRepository.Login(model.Username, hashedPassword);

                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Role, user.UserRole.ToString())
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    Response.Cookies.Append("user_id", user.UserId.ToString(), new CookieOptions { Expires = DateTime.Now.AddHours(1) });
                    Response.Cookies.Append("username", user.Username, new CookieOptions { Expires = DateTime.Now.AddHours(1) });
                    Response.Cookies.Append("user_role", user.UserRole.ToString(), new CookieOptions { Expires = DateTime.Now.AddHours(1) });

                    return RedirectToRoleSpecificPage(user.UserRole);
                }
                else
                {
                    ViewBag.ErrorMessage = "Username or password is incorrect!";
                }
            }
            return View("LoginPage", model);
        }

        private IActionResult RedirectToRoleSpecificPage(int? userRole)
        {
            return userRole switch
            {
                1 => RedirectToAction("HomepageAdmin", "HomePageAdmin"),
                2 => RedirectToAction("HomePageInstructor", "HomePageInstructor"),
                3 => RedirectToAction("HomePageLearner", "HomePageLearner"),
                _ => RedirectToAction("LoginPage", "Login")
            };
        }

        private string GetMd5Hash(string input)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
                sb.Append(b.ToString("X2"));
            return sb.ToString();
        }

        public async Task<IActionResult> Logout()
        {
            if (Request.Cookies["user_id"] != null)
            {
                Response.Cookies.Delete("user_id");
                Response.Cookies.Delete("username");
                Response.Cookies.Delete("user_role");
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        public IActionResult RedirectToHome()
        {
            var userIdCookie = Request.Cookies["user_id"];
            var userRoleCookie = Request.Cookies["user_role"];

            if (userIdCookie != null && userRoleCookie != null)
            {
                int userRole = int.Parse(userRoleCookie);
                return RedirectToRoleSpecificPage(userRole);
            }

            return RedirectToAction("LoginPage", "Login");
        }

        // Quên mật khẩu - Gửi OTP đến email
        [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _accountRepository.GetUserByEmailAsync(request.Email);
            if (user != null)
            {
                var otpCode = _otpService.GenerateOtp(request.Email);
                await _emailService.SendOtpEmailAsync(request.Email, otpCode);
                return Ok("OTP đã được gửi đến email của bạn.");
            }
            return BadRequest("Email không tồn tại trong hệ thống.");
        }

        // Xác thực OTP
        [HttpPost]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            if (_otpService.VerifyOtp(request.Email, request.Otp))
            {
                return Ok("OTP hợp lệ. Hãy nhập mật khẩu mới.");
            }
            return BadRequest("OTP không hợp lệ hoặc đã hết hạn.");
        }

        // Đặt lại mật khẩu
        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var user = await _accountRepository.GetUserByEmailAsync(request.Email);
            if (user != null)
            {
                await _accountRepository.UpdatePasswordAsync(user.UserId, GetMd5Hash(request.NewPassword));
                return Ok("Mật khẩu đã được đặt lại thành công.");
            }
            return BadRequest("Không tìm thấy tài khoản với email này.");
        }
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; }
    }

    public class VerifyOtpRequest
    {
        public string Email { get; set; }
        public string Otp { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; }
        public string NewPassword { get; set; }
    }
}
