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
                            Title = "This email is already added to your account."
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

            var callbackUrl = Url.TokenLoginLink(Request.Scheme, // TODO: make URL generation optional? It already is not supported with AddEmail
                new TokenLoginViewModel
                {
                    Token = token,
                    RememberMe = model.RememberMe,
                    ReturnUrl = returnUrl,
                    Email = email, 
                    Purpose = purpose
                });

            await _emailSender.SendTokenAsync(email, attemptedOperation, callbackUrl, token);

            return RedirectToAction(nameof(TokenLoginManual), 
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
        public IActionResult TokenLoginManual(TokenLoginViewModel model)
        {
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous] // TODO: maybe make this into a HttpPost with ValidateAntiforgeryToken and disable url (only manual)
        public async Task<IActionResult> TokenLogin(TokenLoginViewModel model)
        {

            if (!ModelState.IsValid)
                return View(nameof(TokenLoginManual));
                //return RedirectToAction(nameof(HomeController.Notice), "Home", new
                //{
                //    NoticeType = NoticeType.Warning,
                //    Title = "Invalid parameters supplied."
                //});

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
                isTokenValid = await _userManager.VerifyUserTokenAsync(
                    userCurrentlySignedIn,
                    "Email", model.Purpose, model.Token);
            }

            if (!isTokenValid)
                return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                {
                    NoticeType = NoticeType.Error,
                    Title = "Problem validating code.",
                    Description = "Your code might have expired. Simply try again!"
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
                var emailToAdd = _userManager.NormalizeKey(model.Purpose);

                var userWithConfirmedEmailToAdd = await _userManager.FindByLoginAsync("Email", emailToAdd);
                
                if (userWithConfirmedEmailToAdd == null) // Email to be added never seen before, add email to userCurrentlySignedIn
                {
                    var addLoginResult = await _userManager.AddLoginAsync(userCurrentlySignedIn, 
                        new UserLoginInfo("Email", emailToAdd, "Email"));

                    if (!addLoginResult.Succeeded)
                        return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                        {
                            NoticeType = NoticeType.Error
                        });

                    userCurrentlySignedIn.Email = emailToAdd;
                    userCurrentlySignedIn.EmailConfirmed = true;
                    var updateUserResult = await _userManager.UpdateAsync(userCurrentlySignedIn); // TODO: return success page instead of returning login page

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
                            Title = "This email is already in your account."
                        });
                    }
                    else // Email associated with another account (same user since both verified!)
                    {
                        return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                        {
                            NoticeType = NoticeType.Error,
                            Title = "This email is in another user's account.",
                            Description = $"To add it to this account instead, login with {emailToAdd} and delete the account or add an alternative email."
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
            {
                //ErrorMessage = $"Error from external provider: {remoteError}"; // TODO: research ErrorMessage
                return RedirectToAction(nameof(Login));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user != null) // The user is signed-in
            {
                var result = await _userManager.AddLoginAsync(user, info);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme); // Clear the existing external cookie to ensure a clean login process

                    return View("Message", new NoticeViewModel()
                    {
                        NoticeType = "Success",
                        Title = $"External login {info.ProviderDisplayName} successfully added to your account."
                    });
                }
                else
                {
                    return View("Message", new NoticeViewModel()
                    {
                        NoticeType = "Error",
                        Title = "An unexpected error occured. Please try again later."
                    });
                }
            }
            else // The user is not signed-in
            {
                var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
                if (result.Succeeded)
                {
                    return RedirectToLocal(returnUrl); // Success logging in
                }
                else if (result.IsLockedOut || result.IsNotAllowed)
                {
                    return RedirectToAction(nameof(Lockout));
                }
                else // The user does not have an account, is attempting to register
                {
                    ViewData["ReturnUrl"] = returnUrl;
                    ViewData["LoginProvider"] = info.LoginProvider;
                    var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                    return View("Register", new RegisterViewModel() { Email = email });
                }
            }

        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterConfirmation(RegisterViewModel model, string returnUrl = null)
        {
            var email = _userManager.NormalizeKey(model.Email);
            UserLoginInfo info = await _signInManager.GetExternalLoginInfoAsync();
            //var existingUser = await _userManager.FindByEmailAsync(email)
                ?? new ApplicationUser()
                {
                    Id = email,
                    Email = email,
                    SecurityStamp = ""
                };

            existingUser.UserName = model.UserName;
            existingUser.FavColor = model.FavColor;

            if (info != null) // The user is coming here after an external login to complete registration
            {
            }
            else // The user is trying to register locally
            {
                var existingVerifiedUser = await _userManager.FindByLoginAsync("Email", email);

                var result = await _userManager.VerifyUserTokenAsync(existingUser, "Default", "Register", model.Token);

                if (result && existingVerifiedUser == null) // Verified email supplied & user not registered
                {
                    info = new UserLoginInfo("Email", existingUser.Email, "Email");
                    
                    existingUser.EmailConfirmed = true;
                }
                else
                {
                    return View("Message", new NoticeViewModel()
                    {
                        NoticeType = "Error",
                        Title = "An unexpected error occured. Please try again later." // TODO: better errors
                    });
                }
            }

            existingUser.Id = null;
            var createResult = await _userManager.CreateAsync(existingUser);

            // TODO: edge case where email trying to register with is already used as unconfirmed primary email (of an external login account),
            //       in that case it is allowed to *delete* the external login account alltogether (maybe after prompt? - maybe in TokenLogin?)
            //       since the true owner of that email just came in!

               

            if (!createResult.Succeeded) // TODO: refactor?
            {
                AddErrors(createResult);
                ViewData["ReturnUrl"] = returnUrl;
                return View("Register", model);
            }


            var addResult = await _userManager.AddLoginAsync(existingUser, info);
            if (addResult.Succeeded)
            {
                await _signInManager.SignInAsync(existingUser, isPersistent: model.RememberMe);
                // Success
                return RedirectToLocal(returnUrl);
            } else
            {
                return View("Message", new NoticeViewModel()
                {
                    NoticeType = "Error",
                    Title = "An unexpected error occured. Please try again later." // TODO: better errors
                });
            }

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
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        #endregion
    }
}
