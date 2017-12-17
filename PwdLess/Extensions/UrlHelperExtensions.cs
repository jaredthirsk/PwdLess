using PwdLess.Controllers;
using PwdLess.Models.AccountViewModels;

namespace Microsoft.AspNetCore.Mvc
{
    public static class UrlHelperExtensions
    {
        public static string TokenInputLink(this IUrlHelper urlHelper, string scheme, TokenInputViewModel tokenModel)
        {
            return urlHelper.Action(
                action: nameof(AccountController.TokenInput),
                controller: "Account",
                values: tokenModel,
                protocol: scheme);
        }
    }
}
