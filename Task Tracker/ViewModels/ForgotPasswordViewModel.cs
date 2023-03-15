using System.ComponentModel.DataAnnotations;

namespace Task_Tracker.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
