using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}