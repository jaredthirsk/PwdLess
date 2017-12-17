using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace PwdLess.Models
{
    public class ApplicationUser : IdentityUser, IAdditionalUserInfo
    {
        [EmailAddress]
        public string EmailFromExternalProvider { get; set; }

        public DateTimeOffset DateCreated { get; set; }

        // from IAdditionalUserInfo
        public string FavColor { get; set; }
    }
}
