using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PwdLess.Models;
using PwdLess.Models.AccountViewModels;
using PwdLess.Models.HomeViewModels;
using PwdLess.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PwdLess.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            AuthOperation attemptedOperation;

            ApplicationUser userToSignTokenWith;

            var email = _userManager.NormalizeKey(model.Email);

            var userWithConfirmedEmail = await _userManager.FindByLoginAsync("Email", email);
            var userCurrentlySignedIn = await _userManager.GetUserAsync(User);          

            if (userCurrentlySignedIn == null) // No locally signed-in user (trying to register or login)
            {
                // Clear the existing external cookie to ensure a clean login process
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                if (userWithConfirmedEmail == null) // Email not associated with any other accounts (trying to register)
                {
                    userToSignTokenWith = new ApplicationUser()
                    {
                        Id = email,
                        Email = email,
                        SecurityStamp = ""
                    };

                    attemptedOperation = AuthOperation.Registering;
                }
                else // Email associated with an account (trying to login)
                {
                    userToSignTokenWith = userWithConfirmedEmail;
                    attemptedOperation = AuthOperation.LoggingIn;
                }
            }
            else // A user is currently locally signed-in (trying to add email)
            {
                userToSignTokenWith = userCurrentlySignedIn;

                if (userWithConfirmedEmail == null) // Email not associated with any other accounts (trying to add a novel email)
                {
                    attemptedOperation = AuthOperation.AddingNovelEmail;
                }
                else // Email associated with another user's account
                {
                    if (userWithConfirmedEmail.Id == userCurrentlySignedIn.Id) // Email already added to user's account
                    {
                        return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                        {
                            NoticeType = NoticeType.Warning,
                            Title = "This email is already added to your account.",
                            Description = " "
                        });
                    }
                    else // Email associated with another account that's not the user's
                    {
                        attemptedOperation = AuthOperation.AddingOtherUserEmail;
                    }
                }
            }

            var token = "";
            var purpose = "";

            switch (attemptedOperation)
            {
                case AuthOperation.AddingOtherUserEmail:
                    purpose = "AddEmail";
                    break;
                case AuthOperation.AddingNovelEmail:
                    purpose = "AddEmail";
                    token = await _userManager.GenerateUserTokenAsync(userToSignTokenWith, "Email", purpose);
                    break;
                case AuthOperation.Registering:
                case AuthOperation.LoggingIn:
                    purpose = "RegisterOrLogin";
                    token = await _userManager.GenerateUserTokenAsync(userToSignTokenWith, "Email", purpose);
                    break;
            }

            var callbackUrl = Url.TokenLoginLink(Request.Scheme,
                new TokenLoginViewModel
                {
                    Token = token,
                    RememberMe = model.RememberMe,
                    ReturnUrl = returnUrl,
                    Email = email, 
                    Purpose = purpose
                });

            await _emailSender.SendTokenAsync(email, attemptedOperation, callbackUrl, token);

            return RedirectToAction(nameof(TokenInputManual), 
                new TokenLoginViewModel
                {
                    RememberMe = model.RememberMe,
                    ReturnUrl = returnUrl,
                    Email = email,
                    Purpose = purpose
                });
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult TokenInputManual(TokenLoginViewModel model)
        {
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TokenLogin(TokenLoginViewModel model)
        {

            if (String.IsNullOrWhiteSpace(model.Email) ||
                String.IsNullOrWhiteSpace(model.Purpose) ||
                String.IsNullOrWhiteSpace(model.Token))
            {
                return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                {
                    NoticeType = NoticeType.Warning,
                    Title = "Invalid parameters supplied.",
                    Description = "Did you type in the code correctly?"
                });
            }

            var email = _userManager.NormalizeKey(model.Email);
            
            var userWithConfirmedEmail = await _userManager.FindByLoginAsync("Email", email);
            var userCurrentlySignedIn = await _userManager.GetUserAsync(User);    
            var userEmpty = new ApplicationUser()
            {
                Id = email,
                Email = email,
                SecurityStamp = ""
            };

            var isTokenValid = false;

            if (model.Purpose == "RegisterOrLogin") // Trying to register or login
            {
                isTokenValid = await _userManager.VerifyUserTokenAsync(
                    userWithConfirmedEmail  // Case: logging-in
                    ?? userEmpty,           // Case: registering,
                    "Email", model.Purpose, model.Token);
            }
            else // Trying to add email
            {
                if (userCurrentlySignedIn == null) // If the user is not signed in, prompt them to, then let them back here
                    return RedirectToAction(nameof(Login), new
                    {
                        returnUrl = Request.Path + Request.QueryString
                    });

                isTokenValid = await _userManager.VerifyUserTokenAsync(
                    userCurrentlySignedIn,
                    "Email", model.Purpose, model.Token);
            }

            if (!isTokenValid)
                return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                {
                    NoticeType = NoticeType.Error,
                    Title = "Problem validating code.",
                    Description = "Your code might have expired. Please try again!"
                });

            // Valid {token + email (user) + purpose} supplied
            
            if (model.Purpose == "RegisterOrLogin") // Trying to register or login
            {
                if (userWithConfirmedEmail == null) // Success trying to register
                {
                    var token = await _userManager.GenerateUserTokenAsync(userEmpty, "Default", "Register");

                    ViewData["ReturnUrl"] = model.ReturnUrl;

                    return View("Register", new RegisterViewModel
                    {
                        RememberMe = model.RememberMe,
                        Email = email,
                        Token = token
                    });
                }
                else // Success trying to login
                {
                    await _signInManager.SignInAsync(userWithConfirmedEmail, isPersistent: model.RememberMe);
                }
            }
            else // Trying to add email
            {
                var userWithConfirmedEmailToAdd = await _userManager.FindByLoginAsync("Email", email);
                
                if (userWithConfirmedEmailToAdd == null) // Email to be added never seen before, add email to userCurrentlySignedIn
                {
                    var addLoginResult = await _userManager.AddLoginAsync(userCurrentlySignedIn, 
                        new UserLoginInfo("Email", email, "Email"));

                    if (!addLoginResult.Succeeded)
                        return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                        {
                            NoticeType = NoticeType.Error
                        });

                    userCurrentlySignedIn.Email = email;
                    userCurrentlySignedIn.EmailConfirmed = true;
                    var updateUserResult = await _userManager.UpdateAsync(userCurrentlySignedIn);

                    if (!updateUserResult.Succeeded)
                        return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                        {
                            NoticeType = NoticeType.Error
                        });

                }
                else // Email to be added is in use
                {
                    if (userWithConfirmedEmailToAdd.Id == userCurrentlySignedIn.Id) // Email is already in user's account
                    {
                        return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                        {
                            NoticeType = NoticeType.Warning,
                            Title = "This email is already in your account.",
                            Description = " "
                        });
                    }
                    else // Email associated with another account (same user since both verified!)
                    {
                        return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                        {
                            NoticeType = NoticeType.Warning,
                            Title = "This email is in another user's account.",
                            Description = $"To add it to this account instead, login to {email} then delete the account or add an alternative login method."
                        });
                    }
                }
            }

            // Success
            return RedirectToLocal(model.ReturnUrl);
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginAsync(string provider, string returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme); // Clear the existing external cookie to ensure a clean login process

            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
            return new ChallengeResult(provider, properties);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
                return RedirectToAction(nameof(Login));

            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
                return RedirectToAction(nameof(Login));

            var userWithExternalLogin = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            var userCurrentlySignedIn = await _userManager.GetUserAsync(User);


            var emailFromExternalLoginProvider = _userManager.NormalizeKey(info.Principal.FindFirstValue(ClaimTypes.Email));

            if (userCurrentlySignedIn == null) // No locally signed-in user (trying to register or login)
            {
                if (userWithExternalLogin != null) // User exists and attempting to login
                {
                    var externalLoginResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

                    if (externalLoginResult.Succeeded)
                        return RedirectToLocal(returnUrl); // Success logging in

                    if (externalLoginResult.IsLockedOut || externalLoginResult.IsNotAllowed)
                        return RedirectToAction(nameof(Lockout));
                }
                
                // The user does not have an account, is attempting to register
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["LoginProvider"] = info.LoginProvider;
                return View("Register", new RegisterViewModel()
                {
                    EmailFromExternalProvider = emailFromExternalLoginProvider
                });

            }
            else // A user is currently locally signed-in (trying to add external login)
            {

                if (userWithExternalLogin != null) // External login already in use
                {
                    if (userWithExternalLogin.Id == userCurrentlySignedIn.Id) // External login is already in user's account
                    {
                        return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                        {
                            NoticeType = NoticeType.Warning,
                            Title = "This external login is already in your account.",
                            Description = " ",
                            ShowBackButton = false
                        });
                    }
                    else
                    {
                        return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                        {
                            NoticeType = NoticeType.Warning,
                            Title = "This external login is in another user's account.",
                            Description = $"To add it to this account instead, login to the other account then delete it or add an alternative login method.",
                            ShowBackButton = false
                        });
                    }
                }

                userCurrentlySignedIn.EmailFromExternalProvider = emailFromExternalLoginProvider;

                var addLoginResult = await _userManager.AddLoginAsync(userCurrentlySignedIn, info);
                if (addLoginResult.Succeeded)
                {
                    var updateResult = await _userManager.UpdateAsync(userCurrentlySignedIn);
                    if (updateResult.Succeeded)
                    {
                        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme); // Clear the existing external cookie to ensure a clean login process

                        return RedirectToLocal(returnUrl);
                    }
                }

                return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                {
                    NoticeType = NoticeType.Error
                });
            }

        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterConfirmation(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid || (String.IsNullOrWhiteSpace(model.Email) ^ String.IsNullOrWhiteSpace(model.Email)))
                return View("Register", model);

            var email = _userManager.NormalizeKey(model.Email ?? model.EmailFromExternalProvider);
            
            UserLoginInfo loginInfo = await _signInManager.GetExternalLoginInfoAsync();

            var userEmpty = new ApplicationUser()
            {
                UserName = model.UserName,
                SecurityStamp = "",

                FavColor = model.FavColor,
            };

            if (loginInfo == null) // User trying to register locally
            {
                userEmpty.Id = email; // Is set to null a few lines later
                userEmpty.Email = email;
                userEmpty.EmailConfirmed = true;

                var userWithConfirmedEmail = await _userManager.FindByLoginAsync("Email", email);

                var isTokenValid = await _userManager.VerifyUserTokenAsync(userEmpty, "Default", "Register", model.Token);

                if (isTokenValid && userWithConfirmedEmail == null) // Supplied email is verified & user does not exist
                {
                    userEmpty.Id = null;
                    loginInfo = new UserLoginInfo("Email", userEmpty.Email, "Email");
                }
                else
                {
                    return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                    {
                        NoticeType = NoticeType.Error
                    });
                }

            }
            else // User trying to register after external login
            {
                userEmpty.EmailFromExternalProvider = email;
            }
            var createResult = await _userManager.CreateAsync(userEmpty);            

            if (createResult.Succeeded)
            {
                var addLoginResult = await _userManager.AddLoginAsync(userEmpty, loginInfo);

                if (addLoginResult.Succeeded)
                {
                    // Success
                    await _signInManager.SignInAsync(userEmpty, isPersistent: model.RememberMe);
                    return RedirectToLocal(returnUrl);
                }
            }
            else
            {
                AddErrors(createResult);
                return View("Register", model);
            }


            await _userManager.DeleteAsync(userEmpty);

            return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
            {
                NoticeType = NoticeType.Error
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }
  
        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                {
                    NoticeType = NoticeType.Success,
                    Title = " ",
                    Description = " ",
                    ShowBackButton = false
                });
            }
        }

        #endregion
    }
}
