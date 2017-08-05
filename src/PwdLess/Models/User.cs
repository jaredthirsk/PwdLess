using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PwdLess.Models
{
    // This is the only model used for model binding. Contains custom user properties.
    public class User : BaseUser
    {
        [Required, MinLength(3), MaxLength(15)]
        public string DisplayName { get; set; }

        [Required]
        public string FavouriteColour { get; set; }
    }
}
