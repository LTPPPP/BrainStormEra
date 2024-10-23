using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace BrainStormEra.Controllers.Account
{
    public class AdminController : Controller
    {
        private readonly SwpDb7Context _context;

        public AdminController(SwpDb7Context context)
        {
            _context = context;
        }

    }
}
