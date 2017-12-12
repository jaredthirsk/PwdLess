using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PwdLess.Models;
using PwdLess.Models.AccountViewModels;
using PwdLess.Services;

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

        [TempData] 
        public string ErrorMessage { get; set; } // TODO: research & maybe just remove?

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                AuthOperation authOperation;
                var email = _userManager.NormalizeKey(model.Email); // TODO: rename to suppliedEmail?

                var existingVerifiedUser = await _userManager.FindByLoginAsync("Email", email); // TODO: rename?

                var signedInUser = await _userManager.GetUserAsync(User);

                var user = new ApplicationUser();   // TODO: ApplicationUser user;?             

                if (signedInUser != null) // User signed in
                {
                    user = signedInUser;

                    if (existingVerifiedUser != null) // Email associated with another user's account
                    {
                        if (existingVerifiedUser.Id == signedInUser.Id) // Email already added to user's account
                        {
                            return View("Message", new MessageViewModel()
                            {
                                MessageType = "Warning",
                                Message = "This email is already added to your account."
                            });
                        }
                        else // Email associated with another account that's not the user's
                        {
                            authOperation = AuthOperation.AddingOtherUserEmail;
                        }
                    }
                    else // Email not associated with any other accounts
                    {
                        authOperation = AuthOperation.AddingNovelEmail;
                    }
                }
                else // User not signed in
                {
                    if (existingVerifiedUser != null) // Email associated with an account, trying to login
                    {
                        user = existingVerifiedUser;
                        authOperation = AuthOperation.LoggingIn;
                    }
                    else // Email not associated with any other accounts, trying to register
                    {

                        user = new ApplicationUser()
                        {
                            Id = email,
                            Email = email,
                            SecurityStamp = ""
                        };

                        authOperation = AuthOperation.Registering;
                    }

                }

                var token = "";
                var purpose = "";

                // Note: 
                // purpose can be two things: "RegisterOrLogin" or {email to add to account}
                // token can be two things: "" (in case of adding other user email) or NotNullOrEmpty

                switch (authOperation)
                {
                    case AuthOperation.AddingOtherUserEmail:
                        break;
                    case AuthOperation.AddingNovelEmail:
                        purpose = email;
                        token = await _userManager.GenerateUserTokenAsync(user, "Email", purpose); // If the user is adding an email, the purpose is the supplied email
                        break;
                    case AuthOperation.Registering:
                    case AuthOperation.LoggingIn:
                        purpose = "RegisterOrLogin";
                        token = await _userManager.GenerateUserTokenAsync(user, "Email", purpose);
                        break;
                    default:
                        break;
                }

                // Note:
                // 'user.Email' is email of user used to generate token
                // doesn't have to be the same as 'email' (the just-supplied email)
                // in case of adding emails because in that case user.Email comes
                // from the signed in user not the one found from 'email'

                var callbackUrl = Url.EmailConfirmationLink(Request.Scheme, 
                    new TokenLoginViewModel
                    {
                        Token = token,
                        RememberMe = model.RememberMe,
                        ReturnUrl = returnUrl,
                        Email = user.Email, 
                        Purpose = purpose
                    });

                await _emailSender.SendEmailConfirmationAsync(email, authOperation, callbackUrl, token);

                return RedirectToAction(nameof(TokenLoginManual), 
                    new TokenLoginViewModel
                    {
                        RememberMe = model.RememberMe,
                        ReturnUrl = returnUrl,
                        Email = user.Email,
                        Purpose = purpose
                    });

            }

            // If we got this far, something failed, redisplay form
            return View(model);
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

            if (String.IsNullOrWhiteSpace(model.Email) || 
                String.IsNullOrWhiteSpace(model.Purpose) ||
                String.IsNullOrWhiteSpace(model.Token))
            {
                return View("Message", new MessageViewModel()
                {
                    MessageType = "Error",
                    Message = "Invalid parameters supplied."
                });
            }
             
            var userEmail = _userManager.NormalizeKey(model.Email);
            var registerUser = new ApplicationUser()
            {
                Id = userEmail,
                Email = userEmail,
                SecurityStamp = ""
            };
            var existingUser = await _userManager.FindByEmailAsync(userEmail);
            var existingVerifiedUser = await _userManager.FindByLoginAsync("Email", userEmail);

            var result = false;

            if (model.Purpose == "RegisterOrLogin")
            {
                result = await _userManager.VerifyUserTokenAsync(existingVerifiedUser ?? registerUser, "Email", model.Purpose, model.Token);
            }
            else
            {
                result = await _userManager.VerifyUserTokenAsync(existingVerifiedUser ?? existingUser, "Email", model.Purpose, model.Token);
            }


            // Note: if (result == true) then (existingUser != null) by default

            if (result) // Valid {token + email + purpose} supplied
            {
                if (model.Purpose == "RegisterOrLogin") // Attempting to register or login
                {
                    // Note: email that got verified is userEmail

                    if (existingVerifiedUser != null) // Attempting to login
                    {
                        await _signInManager.SignInAsync(existingVerifiedUser, isPersistent: model.RememberMe);
                    }
                    else // Attempting to register
                    {
                        var token = await _userManager.GenerateUserTokenAsync(registerUser, "Default", "Register");

                        ViewData["ReturnUrl"] = model.ReturnUrl;

                        return View("Register", new RegisterViewModel
                        {
                            RememberMe = model.RememberMe,
                            Email = userEmail,
                            Token = token
                        });
                        
                    }

                }
                else // Attempting to add email
                {
                    // Note: 
                    // email that got verified is model.Purpose,
                    // userEmail is email of user trying to add login,
                    // and since you need to be signed-in to add emails in the first place,
                    // userEmail is assumed to be verified too
                    // ie. assume existingVerifiedUser != null

                    var addUserEmail = _userManager.NormalizeKey(model.Purpose);
                    var existingVerifiedAddUser = await _userManager.FindByLoginAsync("Email", addUserEmail);

                    if (existingVerifiedAddUser != null) // Email associated with another user's account
                    {
                        if (existingVerifiedAddUser.Id == existingVerifiedUser.Id) // Email already added to user's account
                        {
                            return View("Message", new MessageViewModel()
                            {
                                MessageType = "Warning",
                                Message = "This email is already added to your account."
                            });
                        }
                        else // Email associated with another account that's not the user's
                        {
                            return View("Message", new MessageViewModel()
                            {
                                MessageType = "Error",
                                Message = "This email is associated with another account." // TODO: try to get to this line. By creating an account, requesting to add an email to it, then creating another account with that email, then veriying that you still want to add that email to the first account.
                            });
                        }
                    }
                    else // Email not associated with any other accounts // TODO: properly log all errors below
                    {
                        var user = existingVerifiedUser ?? existingUser; // TODO refactor 

                        var addResult = await _userManager.AddLoginAsync(user, new UserLoginInfo("Email", addUserEmail, "Email"));

                        if (!addResult.Succeeded)
                            return View("Message", new MessageViewModel()
                            {
                                MessageType = "Error",
                                Message = "An unexpected error occured. Please try again later."
                            });

                        user.Email = addUserEmail;
                        user.EmailConfirmed = true;
                        addResult = await _userManager.UpdateAsync(user); // TODO: return success page instead of returning login page

                        if (!addResult.Succeeded)
                            return View("Message", new MessageViewModel()
                            {
                                MessageType = "Error",
                                Message = "An unexpected error occured. Please try again later."
                            });

                    }


                }
            }
            else
            {
                return View("Message", new MessageViewModel()
                {
                    MessageType = "Error",
                    Message = "Problem validating code."
                });
            }


            // Success
            return RedirectToLocal(model.ReturnUrl);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterConfirmation(RegisterViewModel model, string returnUrl = null)
        {
            var email = _userManager.NormalizeKey(model.Email);
            UserLoginInfo info = await _signInManager.GetExternalLoginInfoAsync();
            var existingUser = await _userManager.FindByEmailAsync(email)
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
                    return View("Message", new MessageViewModel()
                    {
                        MessageType = "Error",
                        Message = "An unexpected error occured. Please try again later." // TODO: better errors
                    });
                }
            }

            existingUser.Id = null;
            var createResult = await _userManager.CreateAsync(existingUser);

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
                return View("Message", new MessageViewModel()
                {
                    MessageType = "Error",
                    Message = "An unexpected error occured. Please try again later." // TODO: better errors
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
                ErrorMessage = $"Error from external provider: {remoteError}"; // TODO: research ErrorMessage
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

                    return View("Message", new MessageViewModel()
                    {
                        MessageType = "Success",
                        Message = $"External login {info.ProviderDisplayName} successfully added to your account."
                    });
                }
                else
                {
                    return View("Message", new MessageViewModel()
                    {
                        MessageType = "Error",
                        Message = "An unexpected error occured. Please try again later."
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
