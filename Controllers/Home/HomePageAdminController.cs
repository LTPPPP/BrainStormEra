using Microsoft.AspNetCore.Mvc;

namespace BrainStormEra.Controllers.Home.HomePageAdminController
{
    public class HomePageAdminController : Controller
    {
        public IActionResult HomePageAdmin()
        {
            return View();
        }
    }
}
