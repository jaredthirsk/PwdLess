using System.ComponentModel.DataAnnotations;

namespace PwdLess.Models.AccountViewModels
{
    public class TokenInputViewModel
    {   
        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }

        [Display(Name = "Code")]
        public string Token { get; set; }
        
        [EmailAddress]
        public string Email { get; set; }
        
        public string Purpose { get; set; }
    }
}
