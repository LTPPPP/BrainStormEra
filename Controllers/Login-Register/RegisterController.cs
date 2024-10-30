using BrainStormEra.Models;
using BrainStormEra.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BrainStormEra.Repo;

namespace BrainStormEra.Controllers
{
    public class RegisterController : Controller
    {
        private readonly AccountRepo _accountRepo;
        private readonly ILogger<RegisterController> _logger;

        public RegisterController(AccountRepo accountRepo, ILogger<RegisterController> logger)
        {
            _accountRepo = accountRepo;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register()
        {
            GenerateCaptcha();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                ViewBag.ErrorMessage = string.Join(". ", errors);
                GenerateCaptcha();
                return View(model);
            }

            // Check CAPTCHA
            if (TempData["CaptchaAnswer"] == null ||
                TempData["CaptchaAnswer"].ToString() != model.CAPTCHA)
            {
                ViewBag.ErrorMessage = "CAPTCHA is incorrect, please try again.";
                GenerateCaptcha();
                return View(model);
            }

            // Check if Username already exists
            if (await _accountRepo.IsUsernameTakenAsync(model.Username))
            {
                ModelState.AddModelError("Username", "Username is already taken. Please choose another one.");
                GenerateCaptcha();
                return View(model);
            }

            // Check if Email already exists
            if (await _accountRepo.IsEmailTakenAsync(model.Email))
            {
                ModelState.AddModelError("Email", "Email is already in use. Please use a different email.");
                GenerateCaptcha();
                return View(model);
            }

            // Generate unique user ID based on user role
            int userRole = 3; // Default role (e.g., learner)
            string userId = await _accountRepo.GenerateUniqueUserIdAsync(userRole);

            // Hash password before saving
            string hashedPassword = HashPasswordMD5(model.Password);

            // Create new account
            var newAccount = new Models.Account
            {
                UserId = userId,
                UserRole = userRole,
                Username = model.Username,
                Password = hashedPassword,
                UserEmail = model.Email,
                AccountCreatedAt = DateTime.UtcNow
            };

            // Register new account
            await _accountRepo.RegisterAsync(newAccount);

            // Redirect to login page upon successful registration
            return RedirectToAction("LoginPage", "Login");
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

        private void GenerateCaptcha()
        {
            var random = new Random();
            int num1 = random.Next(1, 10);
            int num2 = random.Next(1, 10);
            string captchaQuestion = $"{num1} + {num2} = ?";
            int captchaAnswer = num1 + num2;

            // Store CAPTCHA question and answer in ViewBag and TempData
            ViewBag.CaptchaQuestion = captchaQuestion;
            TempData["CaptchaAnswer"] = captchaAnswer.ToString();
        }
    }
}
