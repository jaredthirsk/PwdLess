using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PwdLess.Models.ManageViewModels
{
    public class EditUserInfoViewModel : AdditionalUserInfo
    {
        [Required]
        [JsonIgnore]
        public IList<UserLoginInfo> Logins { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Main email")]
        [JsonIgnore]
        public string Email { get; set; }

        [JsonIgnore]
        public bool EmailConfirmed { get; set; }
    }
}
