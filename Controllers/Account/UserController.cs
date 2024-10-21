using BrainStormEra.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra.Controllers.Account
{
    [Authorize(Roles = "Admin,Lecturer")]

    public class UserController : Controller
    {
        private readonly SwpDb7Context _context;

        public UserController(SwpDb7Context context)
        {
            _context = context;
        }




    }
}
