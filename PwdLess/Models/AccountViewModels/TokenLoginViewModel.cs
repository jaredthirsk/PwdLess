using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PwdLess.Models.AccountViewModels
{
    public class TokenLoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }

        public string Token { get; set; }
    }
}
