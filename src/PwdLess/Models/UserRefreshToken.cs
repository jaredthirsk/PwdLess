using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace PwdLess.Models
{
    public class UserRefreshToken
    {
        [BindNever, Key]
        public string Content { get; set; }
        [BindNever]
        public long Expiry { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }
    }
}
