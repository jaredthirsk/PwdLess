using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PwdLess.Models.ManageViewModels
{
    public class LoginsViewModel
    {
        public IList<UserLoginInfo> Logins { get; set; }
        
        [EmailAddress]
        public string EmailFromExternalProvider { get; set; }
    }
}
