using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrainStormEra.Views.Home
{
    public class HomePageAdminModel : PageModel
    {
        public string? FullName { get; set; }
        public string? UserPicture { get; set; }
    }
}
