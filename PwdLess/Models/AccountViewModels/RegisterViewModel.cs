using System.ComponentModel.DataAnnotations;

namespace PwdLess.Models.AccountViewModels
{
    public class RegisterViewModel : AdditionalUserInfo
    {
        public bool RememberMe { get; set; }
        public string Token { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public string ExternalLoginProviderDisplayName { get; set; }

        public string ReturnUrl { get; set; }

    }
}
