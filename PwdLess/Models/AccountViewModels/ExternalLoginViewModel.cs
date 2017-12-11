using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PwdLess.Models.AccountViewModels
{
    public class ExternalLoginViewModel
    {
        [Required]
        [MaxLength(10)]
        [Display(Name = "Username", Description = "Username unique to you.")]
        public string UserName { get; set; }
    }
}
