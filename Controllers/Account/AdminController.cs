using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace BrainStormEra.Controllers.Account
{
    public class AdminController : Controller
    {
        private readonly SwpMainContext _context;

        public AdminController(SwpMainContext context)
        {
            _context = context;
        }

    }
}
