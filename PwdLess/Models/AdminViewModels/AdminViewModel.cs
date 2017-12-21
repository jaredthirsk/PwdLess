using PwdLess.Data;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PwdLess.Models.AdminViewModels
{
    public class AdminViewModel
    {
        public int UserCount { get; set; }

        [Display(Name ="Search users...")]
        public string SearchTerm { get; set; }

        public IList<ApplicationUser> Users { get; set; }
    }
}