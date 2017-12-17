using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace PwdLess.Models
{
    public interface IAdditionalUserInfo
    {
        string UserName { get; set; }
        string FavColor { get; set; }
    }

    public class AdditionalUserInfo : IAdditionalUserInfo
    {
        [Required]
        [StringLength(15, MinimumLength = 4, ErrorMessage = "Your username should be between 4 and 15 characters in length.")]
        [Display(Name = "Username", Description = "Should be unique.")]
        public string UserName { get; set; }

        [Display(Name = "Favourite Color")]
        [MinLength(2)]
        public string FavColor { get; set; }
    }
}
