using System.ComponentModel.DataAnnotations;

namespace PwdLess.Models.AccountViewModels
{
    public class RegisterViewModel : AdditionalUserInfo
    {
        public bool RememberMe { get; set; }
        public string Token { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [EmailAddress]
        public string EmailFromExternalProvider { get; set; }

    }
}
