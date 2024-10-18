using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using BrainStormEra.Views.Login;
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
        public async Task<IActionResult> Index(LoginPageViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Hash password using MD5
                //string hashedPassword = HashPasswordMD5(model.Password);
                string hashedPassword = model.Password;
                // Check if the user exists in the database
                var user = _context.Accounts.FirstOrDefault(u => u.Username == model.Username && u.Password == hashedPassword);

                if (user != null)
                {
                    // Create the claims for the user
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
                    ViewBag.ErrorMessage = "Username or password is incorrect!";
                }
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
