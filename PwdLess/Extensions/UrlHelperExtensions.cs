using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PwdLess.Controllers;
using PwdLess.Models.AccountViewModels;

namespace Microsoft.AspNetCore.Mvc
{
    public static class UrlHelperExtensions
    {
        public static string TokenLoginLink(this IUrlHelper urlHelper, string scheme, TokenLoginViewModel tokenModel)
        {
            return urlHelper.Action(
                action: nameof(AccountController.TokenLogin),
                controller: "Account",
                values: tokenModel,
                protocol: scheme);
        }
    }
}
