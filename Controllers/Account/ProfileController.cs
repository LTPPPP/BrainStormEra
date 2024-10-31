using BrainStormEra.Models;
using BrainStormEra.Repo;
using Microsoft.AspNetCore.Mvc;
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

        public ProfileController(AccountRepo accountRepo, IConfiguration configuration)
        {
            _accountRepo = accountRepo;
            _configuration = configuration;
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

        private async Task SendPaymentConfirmationEmail(string filePath, string userId, string userEmail)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var fromAddress = new MailAddress(smtpSettings["UserName"], "BrainStormEra User");
            var toAddress = new MailAddress("llttpp.dev@gmail.com", "Admin");
            const string subject = "[BRAINSTORMERA] PAYMENT CONFIRMATION";

            string body = $@"
                <html>
                    <body style='font-family: Arial, sans-serif; color: #333;'>
                        <h2 style='color: #2c3e50;'>Payment Confirmation from User</h2>
                        <p>Hello Admin,</p>
                        <p>The user with the following details has submitted a payment confirmation:</p>
                        <table style='border-collapse: collapse; width: 100%; max-width: 500px;'>
                            <tr>
                                <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>User ID</td>
                                <td style='padding: 8px; border: 1px solid #ddd;'>{userId}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Email</td>
                                <td style='padding: 8px; border: 1px solid #ddd;'>{userEmail}</td>
                            </tr>
                        </table>
                        <p style='margin-top: 20px;'>Please check the attached image for payment verification.</p>
                        <p>Best Regards,<br>BrainStormEra Team</p>
                        <hr style='border-top: 1px solid #ddd; margin-top: 20px;' />
                        <p style='font-size: 12px; color: #888;'>This is an automated email from the system. Please do not reply.</p>
                    </body>
                </html>";

            using (var smtp = new SmtpClient
            {
                Host = smtpSettings["Host"],
                Port = int.Parse(smtpSettings["Port"]),
                EnableSsl = bool.Parse(smtpSettings["EnableSsl"]),
                Credentials = new System.Net.NetworkCredential(smtpSettings["UserName"], smtpSettings["Password"])
            })
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
            {
                message.Attachments.Add(new Attachment(filePath));
                await smtp.SendMailAsync(message);
            }
        }
    }
}
