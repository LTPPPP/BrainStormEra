using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrainStormEra.Views.Profile
{
    public class UserDetailsViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public int? UserRole { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? PhoneNumber { get; set; }
        public string? UserAddress { get; set; }
        public DateTime AccountCreatedAt { get; set; }
        public string UserPicture { get; set; }
        public int Approved { get; set; } // This property will hold the 'approved' status
    }
}
