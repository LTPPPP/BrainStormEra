using Microsoft.AspNetCore.Mvc;
using BrainStormEra.Models;
using BrainStormEra.Repo;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text;
using System.Threading.Tasks;
using BrainStormEra.Views.Login;

namespace BrainStormEra.Controllers 
{
    public class LoginController : Controller
    {
        private readonly AccountRepo _accountRepository;

        public LoginController(AccountRepo accountRepository)
        {
            _accountRepository = accountRepository;
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
                // Hash the password using MD5
                string hashedPassword = GetMd5Hash(model.Password);

                // Check if the user exists in the database using AccountRepository
                var user = await _accountRepository.Login(model.Username, hashedPassword);

                if (user != null)
                {
                    // Create the user claims (data about the user)
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Role, user.UserRole.ToString())
                    };

                    // Create the identity and principal
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    // Sign in the user and issue the cookie
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    // Set cookies for user_id and other details
                    Response.Cookies.Append("user_id", user.UserId.ToString(), new CookieOptions { Expires = DateTime.Now.AddHours(1) });
                    Response.Cookies.Append("username", user.Username, new CookieOptions { Expires = DateTime.Now.AddHours(1) });
                    Response.Cookies.Append("user_role", user.UserRole.ToString(), new CookieOptions { Expires = DateTime.Now.AddHours(1) });

                    // Redirect based on user role
                    return RedirectToRoleSpecificPage(user.UserRole);
                }
                else
                {
                    ViewBag.ErrorMessage = "Username or password is incorrect!";
                }
            }
            return View("LoginPage", model);
        }

        private IActionResult RedirectToRoleSpecificPage(int? userRole)
        {
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

        // Helper method to hash password with MD5
        private string GetMd5Hash(string input)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public async Task<IActionResult> Logout()
        {
            // Clear cookies and authentication
            if (Request.Cookies["user_id"] != null)
            {
                Response.Cookies.Delete("user_id");
                Response.Cookies.Delete("username");
                Response.Cookies.Delete("user_role");
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("LoginPage", "Login");
        }

        public IActionResult RedirectToHome()
        {
            var userIdCookie = Request.Cookies["user_id"];
            var userRoleCookie = Request.Cookies["user_role"];

            if (userIdCookie != null && userRoleCookie != null)
            {
                int userRole = int.Parse(userRoleCookie);
                return RedirectToRoleSpecificPage(userRole);
            }

            return RedirectToAction("LoginPage", "Login");
        }
    }
}
