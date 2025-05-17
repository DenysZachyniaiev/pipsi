using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels
{
    public class ResetPasswordViewModel
    {
        public ResetPasswordViewModel(string Email)
        {
            this.Email = Email;
        }

        public ResetPasswordViewModel()
        {
        }

        [Required]
        public string VerificationCode { get; set; }

        [Required]
        public string NewPassword { get; set; }
        [Required]
        public string Email { get; set; }
    }
}