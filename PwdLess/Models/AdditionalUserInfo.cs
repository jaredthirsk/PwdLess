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
        [Display(Name = "Username", Description = "Should be unique.")]
        public string UserName { get; set; }

        [Display(Name = "Favourite Color")]
        [Required]
        [MaxLength(5)]
        public string FavColor { get; set; }
    }
}
