using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PwdLess.Models.ManageViewModels
{
    public class EditUserInfoViewModel : AdditionalUserInfo
    {
        [Required]
        public IList<UserLoginInfo> Logins { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Main email")]
        public string CommunicationEmail { get; set; }
    }
}
