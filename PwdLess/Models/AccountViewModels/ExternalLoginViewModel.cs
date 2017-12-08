using System.ComponentModel.DataAnnotations;

namespace PwdLess.Models.AccountViewModels
{
    public class ExternalLoginViewModel
    {
        [Required]
        public string UserName { get; set; }
    }
}