using System.ComponentModel.DataAnnotations;

namespace PwdLess.Models.AccountViewModels
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; } = true;
    }
}
