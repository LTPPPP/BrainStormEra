using BrainStormEra.Models;
using BrainStormEra.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace BrainStormEra.Controllers.Account
{
    public class ProfileController : Controller
    {
        private readonly SwpMainContext _context;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(SwpMainContext context, IConfiguration configuration, EmailService emailService, IMemoryCache cache, ILogger<ProfileController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration;
            _emailService = emailService;
            _cache = cache;
            _logger = logger;
        }

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

        // Display profile page
        public async Task<IActionResult> Index()
        {
            var userId = Request.Cookies["user_id"];
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LoginPage", "Login");
            }

            var account = await _context.Accounts
                .Where(a => a.UserId == userId)
                .FirstOrDefaultAsync();

            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        // Edit profile (GET)
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = Request.Cookies["user_id"];
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LoginPage", "Login");
            }

            var account = await _context.Accounts
                .Where(a => a.UserId == userId)
                .FirstOrDefaultAsync();

            if (account == null)
            {
                return NotFound();
            }

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

            return View(model);
        }

        // Edit profile (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileEditViewModel model, IFormFile avatar)
        {
            var userId = Request.Cookies["user_id"];
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LoginPage", "Login");
            }

            if (ModelState.IsValid || avatar == null)
            {
                var account = await _context.Accounts
                    .Where(a => a.UserId == userId)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    return NotFound();
                }

                account.FullName = model.FullName;
                account.UserEmail = model.UserEmail;
                account.PhoneNumber = model.PhoneNumber;
                account.Gender = model.Gender;
                account.UserAddress = model.UserAddress;
                account.DateOfBirth = model.DateOfBirth;

                if (avatar != null)
                {
                    var validTypes = new[] { "image/png", "image/jpeg" };
                    if (!validTypes.Contains(avatar.ContentType))
                    {
                        ModelState.AddModelError("avatar", "Only PNG and JPEG files are accepted.");
                        return View(model);
                    }

                    if (avatar.Length > 2 * 1024 * 1024)
                    {
                        ModelState.AddModelError("avatar", "File size must not exceed 2MB.");
                        return View(model);
                    }

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "User-img");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Path.GetFileName(avatar.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await avatar.CopyToAsync(stream);
                    }

                    account.UserPicture = $"/uploads/User-img/{fileName}";
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(model);
        }

        // Redirect to home based on role
        public async Task<IActionResult> RedirectToHome()
        {
            var userIdCookie = Request.Cookies["user_id"];
            var userRoleCookie = Request.Cookies["user_role"];

            if (!string.IsNullOrEmpty(userIdCookie) && int.TryParse(userRoleCookie, out var userRole))
            {
                var userRoleFromDb = await _context.Accounts
                    .Where(a => a.UserId == userIdCookie)
                    .Select(a => a.UserRole)
                    .FirstOrDefaultAsync();

                switch (userRoleFromDb)
                {
                    case 1:
                        return RedirectToAction("HomepageAdmin", "HomePageAdmin");
                    case 2:
                        return RedirectToAction("HomePageInstructor", "HomePageInstructor");
                    case 3:
                        return RedirectToAction("HomePageLearner", "HomePageLearner");
                }
            }

            return RedirectToAction("LoginPage", "Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(IFormFile paymentImage, int paymentAmount)
        {
            var userId = Request.Cookies["user_id"];
            var account = await _context.Accounts
                .Where(a => a.UserId == userId)
                .FirstOrDefaultAsync();

            if (account == null || paymentImage == null || paymentImage.Length == 0)
            {
                return BadRequest("User information or image is invalid.");
            }

            // Limit the number of payment confirmation attempts within a time frame
            var cacheKey = $"PaymentAttempts_{userId}";
            var paymentAttempts = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return 0;
            });

            if (paymentAttempts >= 3)
            {
                TempData["ErrorMessage"] = "You have exceeded the number of payment confirmation attempts in 10 minutes. Please try again later.";
                return RedirectToAction("Index");
            }

            _cache.Set(cacheKey, paymentAttempts + 1);

            // Check image size and format
            var validTypes = new[] { "image/png", "image/jpeg" };
            if (!validTypes.Contains(paymentImage.ContentType) || paymentImage.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError("paymentImage", "Only PNG or JPEG files up to 2MB are accepted.");
                return RedirectToAction("Index");
            }

            // Retrieve admin emails and special emails
            var adminEmails = await _context.Accounts
                .Where(a => a.UserRole == 1)
                .Select(a => a.UserEmail)
                .ToListAsync();

            var specialEmails = _configuration.GetSection("SpecialEmails").Get<List<string>>() ?? new List<string>();

            // Combine admin emails and special emails
            var allEmails = adminEmails.Concat(specialEmails).Distinct();

            // Send confirmation email to each email in the combined list
            using (var stream = paymentImage.OpenReadStream())
            {
                foreach (var email in allEmails)
                {
                    await _emailService.SendPaymentConfirmationEmailAsync(email, account.UserId, account.UserEmail, paymentAmount, stream, paymentImage.FileName);
                }
            }

            TempData["SuccessMessage"] = "Payment has been confirmed. Please wait for admin approval.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var userId = Request.Cookies["user_id"];
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "User is not authenticated.";
                return RedirectToAction("LoginPage", "Login");
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirm password do not match.";
                return RedirectToAction("Index");
            }

            try
            {
                var account = await _context.Accounts
                    .Where(a => a.UserId == userId)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index");
                }

                var oldPasswordHash = GetMd5Hash(oldPassword);

                if (account.Password != oldPasswordHash)
                {
                    TempData["ErrorMessage"] = "Old password is incorrect.";
                    return RedirectToAction("Index");
                }

                account.Password = GetMd5Hash(newPassword);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Password has been reset successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while resetting the password.";
                _logger.LogError(ex, "Error resetting password.");
            }

            return RedirectToAction("Index");
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
    }
}
