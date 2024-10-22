using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace BrainStormEra.Controllers.Account
{
    public class ProfileController : Controller
    {
        private readonly SwpDb7Context _context;

        public ProfileController(SwpDb7Context context)
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
                _context.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(account);
        }
    }
}
