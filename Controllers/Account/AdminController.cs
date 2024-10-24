using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace BrainStormEra.Controllers.Account
{
    public class AdminController : Controller
    {
        private readonly SwpMainFpContext _context;

        public AdminController(SwpMainFpContext context)
        {
            _context = context;
        }

    }
}
