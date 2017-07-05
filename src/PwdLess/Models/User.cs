using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;

namespace PwdLess.Models
{
    public class User // Only model used for model binding
    {
        [BindNever]
        public string UserId { get; set; }

        [BindNever]
        public string RefreshToken { get; set; }
        [BindNever]
        public long RefreshTokenExpiry { get; set; }

        [BindNever]
        public ICollection<UserContact> UserContacts { get; set; }
    }
}