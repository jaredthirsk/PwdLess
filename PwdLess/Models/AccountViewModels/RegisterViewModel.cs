using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PwdLess.Models.AccountViewModels
{
    public class RegisterViewModel : IAdditionalUserInfo
    {
        public bool RememberMe { get; set; }
        public string Token { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Display(Name = "User name", Description = "Should be unique.")]
        public string UserName { get; set; }

        // From IAdditionalUserInfo        
        [Display(Name = "Favourite Color")]
        [Required]
        [MaxLength(5)]
        public string FavColor { get; set; }

    }
}
