using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using BrainStormEra.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace BrainStormEra.Controllers
{
    public class LoginController : Controller
    {
        private readonly SwpDb7Context _context;

        public LoginController(SwpDb7Context context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult LoginPage()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                //string hashedPassword = HashPasswordMD5(model.Password);
                string hashedPassword = model.Password;
                var user = _context.Accounts.FirstOrDefault(u => u.Username == model.Username && u.Password == hashedPassword);

                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Role, user.UserRole.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    switch (user.UserRole)
                    {
                        case 1:
                            return RedirectToAction("HomePageAdmin", "Home");
                        case 2:
                            return RedirectToAction("HomePageInstructor", "Home");
                        case 3:
                            return RedirectToAction("HomePageLearner", "Home");
                    }

                }
                else
                {
                    // If login fails, set error message
                    ViewBag.ErrorMessage = "Invalid username or password.";
                }
            }

            // Return the view with error message if login fails
            return View("LoginPage", model);
        }

        [HttpPost]
        public async Task<IActionResult> Register(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if the email is already registered
                var existingUser = _context.Accounts.FirstOrDefault(u => u.UserEmail == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View("LoginPage", model);
                }

                string hashedPassword = HashPasswordMD5(model.Password);

                var newAccount = new Account
                {
                    Username = model.Username,
                    UserEmail = model.Email,
                    Password = hashedPassword,
                    UserRole = 3, 
                    AccountCreatedAt = DateTime.Now
                };

                _context.Accounts.Add(newAccount);
                await _context.SaveChangesAsync();

                // Return success modal
                TempData["RegisterSuccess"] = true;
                return Json(new { success = true });
            }

            return View("LoginPage", model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("LoginPage", "Login");
        }

        private string HashPasswordMD5(string password)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(password);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}
