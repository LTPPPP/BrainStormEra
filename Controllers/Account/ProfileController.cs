using BrainStormEra.Models;
using BrainStormEra.Repo;
using BrainStormEra.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace BrainStormEra.Controllers.Account
{
    public class ProfileController : Controller
    {
        private readonly AccountRepo _accountRepo;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ProfileController> _logger;


        public ProfileController(AccountRepo accountRepo, IConfiguration configuration, EmailService emailService, IMemoryCache cache, ILogger<ProfileController> logger)
        {
            _accountRepo = accountRepo;
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

            var account = await _accountRepo.GetAccountByUserIdAsync(userId);
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

            var account = await _accountRepo.GetAccountByUserIdAsync(userId);
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
                var account = await _accountRepo.GetAccountByUserIdAsync(userId);
                if (account == null)
                {
                    return NotFound();
                }

                await _accountRepo.UpdateAccountAsync(userId, model.FullName, model.UserEmail, model.PhoneNumber, model.Gender, model.UserAddress, model.DateOfBirth);

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

                    var fileName = await _accountRepo.SaveAvatarAsync(userId, avatar);
                    await _accountRepo.UpdateUserPictureAsync(userId, fileName);
                }

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
                var userRoleFromDb = await _accountRepo.GetUserRoleByUserIdAsync(userIdCookie);

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
            var account = await _accountRepo.GetAccountByUserIdAsync(userId);

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
            var adminEmails = await _accountRepo.GetAdminEmailsAsync();
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
                var account = await _accountRepo.GetAccountByUserIdAsync(userId);
                if (account == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index");
                }

                var oldPasswordHash = _accountRepo.GetMd5Hash(oldPassword);

                if (account.Password != oldPasswordHash)
                {
                    TempData["ErrorMessage"] = "Old password is incorrect.";
                    return RedirectToAction("Index");
                }

                await _accountRepo.UpdatePasswordAsync(userId, _accountRepo.GetMd5Hash(newPassword));
                TempData["SuccessMessage"] = "Password has been reset successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while resetting the password.";
                _logger.LogError(ex, "Error resetting password.");
            }

            return RedirectToAction("Index");
        }

    }
}