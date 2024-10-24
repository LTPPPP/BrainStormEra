using BrainStormEra.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra.Controllers.Account
{
    [Authorize(Roles = "Admin,Lecturer")]

    public class UserController : Controller
    {
        private readonly SwpMainFpContext _context;

        public UserController(SwpMainFpContext context)
        {
            _context = context;
        }

    }
}
