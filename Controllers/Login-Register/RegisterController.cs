using BrainStormEra.Models;
using BrainStormEra.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra.Controllers
{
    public class RegisterController : Controller
    {
        private readonly SwpMainContext _context;
        private readonly ILogger<RegisterController> _logger;

        public RegisterController(SwpMainContext context, ILogger<RegisterController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
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
            if (await _context.Accounts.AnyAsync(a => a.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Username is already taken. Please choose another one.");
                GenerateCaptcha();
                return View(model);
            }

            // Check if Email already exists
            if (await _context.Accounts.AnyAsync(a => a.UserEmail == model.Email))
            {
                ModelState.AddModelError("Email", "Email is already in use. Please use a different email.");
                GenerateCaptcha();
                return View(model);
            }

            // Generate unique user ID based on user role
            int userRole = 3; // Default role (e.g., learner)
            string userId = await GenerateUniqueUserIdAsync(userRole);

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
            _context.Accounts.Add(newAccount);
            await _context.SaveChangesAsync();

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

        private async Task<string> GenerateUniqueUserIdAsync(int userRole)
        {
            string prefix = userRole switch
            {
                1 => "AD",
                2 => "IN",
                3 => "LN",
                _ => throw new ArgumentException("Invalid user role", nameof(userRole))
            };

            string lastId = await _context.Accounts
                .Where(a => a.UserRole == userRole)
                .OrderByDescending(a => a.UserId)
                .Select(a => a.UserId)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastId) && int.TryParse(lastId.Substring(2), out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }

            string newId = $"{prefix}{nextNumber:D3}";

            // Ensure generated ID is unique
            while (await _context.Accounts.AnyAsync(a => a.UserId == newId))
            {
                nextNumber++;
                newId = $"{prefix}{nextNumber:D3}";
            }

            return newId;
        }
    }
}
