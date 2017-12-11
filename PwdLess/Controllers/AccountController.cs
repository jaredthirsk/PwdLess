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
        public string ErrorMessage { get; set; }

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
                var token = "";
                AuthOperation authOperation;
                var email = _userManager.NormalizeKey(model.Email);

                var existingVerifiedUser = await _userManager.FindByLoginAsync("Email", email);
                var existingUser = await _userManager.FindByEmailAsync(email);
                var signedInUser = await _userManager.GetUserAsync(User);
                var user = new ApplicationUser();                

                if (signedInUser != null) // User signed in
                {
                    user = signedInUser;

                    if (existingVerifiedUser != null) // Email associated with another user's account
                    {
                        if (existingVerifiedUser.Id == signedInUser.Id) // Email already added to user's account
                        {
                            return View("Message", new MessageViewModel() { MessageType = "Warning", Message = "This email is already added to your account." });
                        }
                        else // Email associated with another account that's not the user's
                        {
                            authOperation = AuthOperation.AddingOtherUserEmail;
                        }
                    }
                    else // Email not associated with any other accounts
                    {
                        if (existingUser != null)
                        {
                            // This should never happen. If an email is in someone's account then it has 
                        }
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
                        if (existingUser != null) // Temp account exists
                        {
                            user = existingUser;
                        }
                        else // Create temp account for token generation if doesn't exist
                        {
                            user = new ApplicationUser() { Email = email, UserName = Guid.NewGuid().ToString(), EmailConfirmed = false }; // TODO: customize these starters, this is how every new account starts
                            var result = await _userManager.CreateAsync(signedInUser);
                            if (!result.Succeeded)
                                return View("Message", new MessageViewModel() { MessageType = "Error", Message = "An unexpected error occured." });
                        }

                        authOperation = AuthOperation.Registering;
                    }

                }

                // Note:
                // If existingUser exists but existingVerifiedUser does not, 
                // someone tried to register with this email but didn't
                // Note: users with a User but no "Email" in UserLogins are unverified, trying to register. May need to delete from database after token expires somehow? Maybe add ApplicationUser.IsTemporary?
                // An email is only ever added to UserLogins if it has been verified for that user
                // A user can never have an Email in their account that's not in UserLogins - even in external logins, I don't just make their email the one that the external login gives me blindly!


                if (authOperation != AuthOperation.AddingOtherUserEmail)
                    token = await _userManager.GenerateUserTokenAsync(user, "Email", email); // Purpose is supplied email

                var callbackUrl = Url.EmailConfirmationLink(Request.Scheme, new TokenLoginViewModel { Token = token, RememberMe = model.RememberMe, ReturnUrl = returnUrl, Email = user.Email, Purpose = email });

                await _emailSender.SendEmailConfirmationAsync(email, authOperation, callbackUrl, token);

                return RedirectToAction(nameof(TokenLoginManual), new TokenLoginViewModel { RememberMe = model.RememberMe, ReturnUrl = returnUrl, Email = user.Email, Purpose = email });

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
        [AllowAnonymous]
        public async Task<IActionResult> TokenLogin(TokenLoginViewModel model)
        {


            if (model.Token == null)
                return View("Message", new MessageViewModel() { MessageType = "Error", Message = "No code supplied." });














            if (model.Token == null)
                return View("Message", new MessageViewModel() { MessageType = "Error", Message = "No code supplied." });
            if (model.Purpose != "Register" && model.Purpose != "Login" && model.Purpose != "AddEmail")
                return View("Message", new MessageViewModel() { MessageType = "Error", Message = "Unknown purpose." });

            var email = _userManager.NormalizeKey(model.Email);
            var user = await _userManager.FindByLoginAsync("Email", email);
            var isTryingToAddOtherUserEmail = false; 

            if (user == null)
            {
                if (model.Purpose == "Register")
                    user = await _userManager.FindByEmailAsync(email);
                else if (model.Purpose == "AddEmail")
                    user = await _userManager.GetUserAsync(User);
                else
                    return View("Message", new MessageViewModel() { MessageType = "Error", Message = "An unexpected error occured." });
            }
            else
            {
                if (model.Purpose == "AddEmail")
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (user.Id != currentUser.Id)
                    {
                        isTryingToAddOtherUserEmail = true;
                    }
                    else
                    {
                        return View("Message", new MessageViewModel() { MessageType = "Warning", Message = "This email is already added to your account." });
                    }
                }
            }

            var result = await _userManager.VerifyUserTokenAsync(user, "Email", model.Purpose, model.Token);

            if (result)
            {
                if (model.Purpose == "Register")
                {
                    // ASK USER TO INPUT REQUIRED INFO before signing them in or adding an email login
                }
                if (model.Purpose == "Login" || model.Purpose == "Register")
                {
                    await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);
                }
                if (model.Purpose == "AddEmail" || model.Purpose == "Register")
                {
                    if (isTryingToAddOtherUserEmail)
                    {
                        return View("Message", new MessageViewModel() { MessageType = "Error", Message = $"The email {email} is already in use by another account!", Description = "Try logging in with that email instead. If you still need to add it to this account then delete the other one." });
                    }
                    var addAddResult = await _userManager.AddLoginAsync(user, new UserLoginInfo("Email", email, "Email"));
                    if (addAddResult.Succeeded)
                    {
                        _logger.LogDebug($"SUCCESS, JUST ADDED NEW EMAIL: [{email}] TO USER: [{user.Id}]");
                    }
                    else
                    {
                        return View("Message", new MessageViewModel() { MessageType = "Error", Message = "Failed to add new email." });
                    }
                }
                if (model.Purpose == "AddEmail")
                {
                    if (model.MakePrimary)
                    {
                        var primaryResult = await _userManager.SetEmailAsync(user, email);
                        if (primaryResult.Succeeded)
                        {
                            _logger.LogDebug($"SUCCESS, JUST MADE EMAIL: [{email}] PRIMARY FOR USER: [{user.Id}]");
                        } else
                        {
                            return View("Message", new MessageViewModel() { MessageType = "Error", Message = "Failed to set primary email." });
                        }
                    }
                }

            } else // TRY else { AddErrors(result) } WHEN YOU'RE FEELIN' FANCY
            {
                return View("Message", new MessageViewModel() { MessageType = "Error", Message = "Problem validating code." });
            }

            // Success

            if (model.ReturnUrl == "" || model.ReturnUrl == null)
            {
                email = email.ToLower();
                var message = "";

                if (model.Purpose == "Register")
                    message = $"You have been successfully registered with the email {email}.";
                else if (model.Purpose == "Login")
                    message = $"You have successfully logged-in with the email {email}.";
                else if (model.Purpose == "AddEmail")
                    message = $"The email {email} has been successfully added to your account. {(model.MakePrimary ? "It has been made the primary email." : "")}";

                return View("Message", new MessageViewModel() { MessageType = "Success", Message = message });
            }
            
            return RedirectToLocal(model.ReturnUrl);
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
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
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
            if (user != null)
            {
                var result = await _userManager.AddLoginAsync(user, info);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    // Clear the existing external cookie to ensure a clean login process
                    await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
                    return View("Message", new MessageViewModel() { MessageType = "Success", Message = $"External login {info.ProviderDisplayName} successfully added to your account." });
                } else
                {
                    return View("Message", new MessageViewModel() { MessageType = "Error", Message = $"There was a problem adding this external login to your account." });
                }
            }


            // Sign in the user with this external login provider if the user already has a login.
            var result2 = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result2.Succeeded)
            {
                _logger.LogInformation("User logged in with {Name} provider.", info.LoginProvider);
                return RedirectToLocal(returnUrl); // TODO: again refactor this kinds thing
            }
            if (result2.IsLockedOut)
            {
                return RedirectToAction(nameof(Lockout));
            }
            else
            { // they don't have an account
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["LoginProvider"] = info.LoginProvider;
                var email = _userManager.NormalizeKey(info.Principal.FindFirstValue(ClaimTypes.Email));
                

                user = await _userManager.FindByLoginAsync("Email", email);
                if (user == null)
                {
                    user = await _userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        user = new ApplicationUser() { Email = email, UserName = Guid.NewGuid().ToString() };
                        var result3 = await _userManager.CreateAsync(user);
                        if (!result3.Succeeded)
                            _logger.LogDebug(result3.Errors.FirstOrDefault().ToString());
                    } // else someone tried to register with this email but didn't
                      // Note: users with a User but no "Email" in UserLogins are unverified, trying to register. May need to delete from database after token expires somehow? Maybe add ApplicationUser.IsTemporary?
                      // An email is only ever added to UserLogins if it has been verified for that user
                      // A user can have an Email int heir account that's not in UserLogins - means that is their primary email in case of external logins or that they didn't compelte registration
                }
                else // a verified account exists
                {
                    return View("Message", new MessageViewModel() { MessageType = "Error", Message = "This email is already associated with an account." });
                }

                // they're ready for login, just grab a few more infos
                return View("ExternalLogin", new ExternalLoginViewModel { UserName = email }); // <- change + rename external login to maybe RegisterExtraInfo 
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                    return View("Message", new MessageViewModel() { MessageType = "Error", Message = "An unexpected error occured." });


                var user = await _userManager.FindByEmailAsync(info.Principal.FindFirstValue(ClaimTypes.Email));

                if (user == null)
                    return View("Message", new MessageViewModel() { MessageType = "Error", Message = "An unexpected error occured." });

                user.UserName = model.UserName;
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    AddErrors(result);

                result = await _userManager.AddLoginAsync(user, info);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider); // TODO: do I need to start logging everything?
                    return RedirectToLocal(returnUrl);
                }
            }

            ViewData["ReturnUrl"] = returnUrl;

            if (returnUrl == "" || returnUrl == null) // TODO: sound familiar? Refactor!
            {
                return View("Message", new MessageViewModel() { MessageType = "Success", Message = "You have successfully logged-in." });
            }

            return View(nameof(ExternalLoginAsync), model);
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
                return BadRequest(); // TODO: redirect to homepage soemhow
            }
        }

        #endregion
    }
}
