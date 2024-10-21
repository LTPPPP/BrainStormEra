using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;

namespace BrainStormEra.Controllers.Account
{
    public class AdminController : Controller
    {
        private readonly SwpDb7Context _context;
        public AdminController(SwpDb7Context context)
        {
            _context = context;
        }
        public IActionResult ManageCourses()
        {

            return View();
        }

        public IActionResult ManageUsers()
        {
            var users = _context.Accounts.ToList();
            return View(users);
        }

    }
}
