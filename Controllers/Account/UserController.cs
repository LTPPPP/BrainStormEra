using BrainStormEra.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra.Controllers.Account
{
    [Authorize(Roles = "Admin,Lecturer")]

    public class UserController : Controller
    {
        private readonly SwpMainContext _context;

        public UserController(SwpMainContext context)
        {
            _context = context;
        }

    }
}
