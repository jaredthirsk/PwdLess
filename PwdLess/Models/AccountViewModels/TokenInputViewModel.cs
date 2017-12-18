using System.ComponentModel.DataAnnotations;

namespace PwdLess.Models.AccountViewModels
{
    public class TokenInputViewModel
    {   
        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }

        [Required]
        [Display(Name = "Code")]
        public string Token { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Purpose { get; set; }
    }
}
