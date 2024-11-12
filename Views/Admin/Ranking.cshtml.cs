using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrainStormEra.Views.Admin
{
    public class UserRankingViewModel
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public int CompletedCourses { get; set; }
        public string UserPicture { get; set; } // Thêm thuộc tính này
    }


}
