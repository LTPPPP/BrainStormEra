using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using BrainStormEra.Views.Login;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
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
                string hashedPassword = HashPasswordMD5(model.Password); // Hash the password
                var user = _context.Accounts.FirstOrDefault(u => u.Username == model.Username && u.Password == hashedPassword);

                if (user != null)
                {
                    // Save user_id to session
                    HttpContext.Session.SetString("user_id", user.UserId);
                    HttpContext.Session.SetString("username", user.Username);
                    HttpContext.Session.SetString("user_role", user.UserRole.ToString());

                    // Redirect based on user role
                    switch (user.UserRole)
                    {
                        case 1:
                            return RedirectToAction("HomepageAdmin", "Home");
                        case 2:
                            return RedirectToAction("HomePageInstructor", "Home");
                        case 3:
                            return RedirectToAction("HomePageLearner", "Home");
                        default:
                            return RedirectToAction("LoginPage", "Login");
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
            HttpContext.Session.Clear(); // Clear all session
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
