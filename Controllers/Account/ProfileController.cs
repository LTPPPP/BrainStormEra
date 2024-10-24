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

        // Action to display the profile page
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

        // Action to edit profile
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

            return View(account);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(BrainStormEra.Models.Account account, IFormFile avatar)
        {
            var userId = Request.Cookies["user_id"];

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LoginPage", "Login");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the user's account from the database
                    var accountInDb = _context.Accounts.FirstOrDefault(a => a.UserId == userId);
                    if (accountInDb == null) return NotFound();

                    // Update the account details
                    accountInDb.FullName = account.FullName;
                    accountInDb.UserEmail = account.UserEmail;
                    accountInDb.PhoneNumber = account.PhoneNumber;
                    accountInDb.Gender = account.Gender;
                    accountInDb.UserAddress = account.UserAddress;
                    accountInDb.DateOfBirth = account.DateOfBirth;

                    // Handle new avatar upload if available
                    if (avatar != null && avatar.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

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

                        // Update the user's picture path in the database
                        accountInDb.UserPicture = $"/uploads/{fileName}";
                    }

                    // Save the changes back to the database
                    var result = _context.SaveChanges();
                    Console.WriteLine($"{result} record(s) updated.");

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating profile: {ex.Message}");
                    ModelState.AddModelError("", "An error occurred while updating your profile.");
                }
            }
            else
            {
                Console.WriteLine("ModelState is not valid");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }
            }

            return View(account);
        }
        public IActionResult RedirectToHome()
        {
            var userIdCookie = Request.Cookies["user_id"];
            var userRoleCookie = Request.Cookies["user_role"];

            if (userIdCookie != null && userRoleCookie != null)
            {
                int userRole = int.Parse(userRoleCookie);

                // Điều hướng dựa vào userRole
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

            // Nếu không có cookie, chuyển về trang login
            return RedirectToAction("LoginPage", "Login");
        }

    }
}
