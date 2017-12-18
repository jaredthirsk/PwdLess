using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using PwdLess.Models;

namespace PwdLess.Data
{
    public class ApplicationUser : IdentityUser, IAdditionalUserInfo
    {
        public DateTimeOffset DateCreated { get; set; }

        // from IAdditionalUserInfo
        public string FavColor { get; set; }
    }
}
