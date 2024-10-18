using System.ComponentModel.DataAnnotations;

namespace BrainStormEra.Views.Login
{
    public class LoginPageViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

    }
}