using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using PwdLess.Services;

namespace PwdLess.Services
{
    public static class EmailSenderExtensions
    {
        public static Task SendEmailConfirmationAsync(this IEmailSender emailSender, string email, AuthOperation messageKind, string link, string token)
        {
            var subject = "";
            var message = "";

            switch (messageKind)
            {
                case AuthOperation.AddingOtherUserEmail:
                    subject = "Adding email failed.";
                    message = $"The email '{email}' is already in use by another account! " +
                        $"Try logging out then logging in with that email instead. " +
                        $"If you still need to add it to this account then delete the other one.";
                    break;
                case AuthOperation.AddingNovelEmail:
                    subject = "Add this email to your account";
                    message = $"To add this email to your account, please click this link: <a href='{link}'>link</a>" +
                        $"Alternatively, use this code: {token}";
                    break;
                case AuthOperation.Registering:
                    subject = "Create your account";
                    message = $"To continue creating your account, please click this link: <a href='{link}'>link</a>" +
                        $"Alternatively, use this code: {token}";
                    break;
                case AuthOperation.LoggingIn:
                    subject = "Login to your account";
                    message = $"To login to your account, please click this link: <a href='{link}'>link</a>" +
                        $"Alternatively, use this code: {token}";
                    break;
                default:
                    break;
            }

            return emailSender.SendEmailAsync(email, subject, message);
        }
    }
}

