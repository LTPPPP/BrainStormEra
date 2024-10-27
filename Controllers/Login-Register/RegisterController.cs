using BrainStormEra.Models;
using BrainStormEra.ViewModels;
using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BrainStormEra.Controllers
{
    public class RegisterController : Controller
    {
        private readonly SwpMainContext _context;
        private readonly ILogger<RegisterController> _logger;

        public RegisterController(SwpMainContext context, ILogger<RegisterController> logger)
        {
            _context = context;
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
                GenerateCaptcha(); // Generate new CAPTCHA question for retry
                return View(model);
            }

            // Check CAPTCHA
            if (TempData["CaptchaAnswer"] == null ||
                TempData["CaptchaAnswer"].ToString() != model.CAPTCHA)
            {
                ViewBag.ErrorMessage = "CAPTCHA is incorrect, please try again.";
                GenerateCaptcha(); // Generate new CAPTCHA question for retry
                return View(model);
            }

            // Check if Username already exists
            bool usernameExists = await _context.Accounts.AnyAsync(a => a.Username == model.Username);
            if (usernameExists)
            {
                ModelState.AddModelError("Username", "Username is already taken. Please choose another one.");
                GenerateCaptcha();
                return View(model);
            }

            // Check if Email already exists
            bool emailExists = await _context.Accounts.AnyAsync(a => a.UserEmail == model.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email is already in use. Please use a different email.");
                GenerateCaptcha();
                return View(model);
            }

            // Generate unique user ID based on user role
            int userRole = 3; // Assume default role (e.g., learner)
            string userId = await GenerateUniqueUserId(userRole);

            // Hash password before saving
            string hashedPassword = HashPasswordMD5(model.Password);

            // Create new account and save to the database
            var newAccount = new Models.Account
            {
                UserId = userId,
                UserRole = userRole,
                Username = model.Username,
                Password = hashedPassword,
                UserEmail = model.Email,
                AccountCreatedAt = DateTime.UtcNow
            };

            _context.Accounts.Add(newAccount);
            await _context.SaveChangesAsync();

            // Redirect to login page upon successful registration
            return RedirectToAction("LoginPage", "Login");
        }

        private async Task<string> GenerateUniqueUserId(int userRole)
        {
            string prefix = userRole switch
            {
                1 => "AD",
                2 => "IN",
                3 => "LN",
                _ => throw new ArgumentException("Invalid user role", nameof(userRole))
            };

            var lastId = await _context.Accounts
                .Where(a => a.UserRole == userRole)
                .Select(a => a.UserId)
                .OrderByDescending(id => id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastId != null && int.TryParse(lastId[2..], out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }

            string newId = $"{prefix}{nextNumber:D3}";

            while (await _context.Accounts.AnyAsync(u => u.UserId == newId))
            {
                nextNumber++;
                newId = $"{prefix}{nextNumber:D3}";
            }

            return newId;
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

            // Store CAPTCHA question and answer in ViewBag and TempData for access on the view and on postback
            ViewBag.CaptchaQuestion = captchaQuestion;
            TempData["CaptchaAnswer"] = captchaAnswer.ToString();
        }
    }
}
