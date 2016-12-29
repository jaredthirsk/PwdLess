using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PwdLess.Auth.Models
{
    public class User
    {
        [Key]
        [EmailAddress]
        public string Email { get; set; }
    }
}
