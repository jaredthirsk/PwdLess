using System.ComponentModel.DataAnnotations;

namespace PwdLess.Models
{
    public class UserContact
    { 
        [Key]
        public string Contact { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }

    }
}
