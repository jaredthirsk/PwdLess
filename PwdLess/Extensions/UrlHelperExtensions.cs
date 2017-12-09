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
        public static string EmailConfirmationLink(this IUrlHelper urlHelper, string scheme, TokenLoginViewModel tokenModel)
        {
            return urlHelper.Action(
                action: nameof(AccountController.TokenLogin),
                controller: "Account",
                values: tokenModel,
                protocol: scheme);
        }

        //public static string ResetPasswordCallbackLink(this IUrlHelper urlHelper, string userId, string code, string scheme)
        //{
        //    return urlHelper.Action(
        //        action: nameof(AccountController.ResetPassword),
        //        controller: "Account",
        //        values: new { userId, code },
        //        protocol: scheme);
        //}
    }
}
