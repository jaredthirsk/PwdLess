using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PwdLess.Models
{
    public class BaseUser
    {
        [BindNever, JsonIgnore]
        public string UserId { get; set; }

        [BindNever, JsonIgnore]
        public long DateCreated { get; set; }

        [BindNever, JsonIgnore]
        public ICollection<UserRefreshToken> UserRefreshTokens { get; set; }

        [BindNever, JsonIgnore]
        public ICollection<UserContact> UserContacts { get; set; }
    }
}
 