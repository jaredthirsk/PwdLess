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
                var email = _userManager.NormalizeKey(model.Email);
                var user = new ApplicationUser();
                var purpose = "";

                if (_signInManager.IsSignedIn(User))
                {
                    user = await _userManager.FindByLoginAsync("Email", email);
                    if (user == null)
                    {
                        user = await _userManager.GetUserAsync(User);
                        purpose = "AddEmail";
                    } else
                    {
                        return View("Message", new MessageViewModel() { MessageType = "Warning", Message = "This email is already added to your account." });
                    }
                }
                else
                {
                    user = await _userManager.FindByLoginAsync("Email", email);
                    if (user == null)
                    {
                        user = await _userManager.FindByEmailAsync(email);
                        if (user == null)
                        {
                            user = new ApplicationUser() { Id = email, UserName = email, Email = email };
                            await _userManager.CreateAsync(user);
                        } // else someone tried to register with this email but didn't
                        // Note: users with a User but no "Email" in UserLogins are unverified, trying to register. May need to delete from database after token expires somehow? Maybe add ApplicationUser.IsTemporary?
                        purpose = "Register";
                    }
                    else
                    {
                        purpose = "Login";
                    }
                }

                _logger.LogDebug($"USER: {user.Id} ATTEMPTING PURPOSE: {purpose} WITH EMAIL: {email}");

                var token = await _userManager.GenerateUserTokenAsync(user, "Email", purpose);

                var callbackUrl = Url.EmailConfirmationLink(Request.Scheme, new TokenLoginViewModel { Token = token, Email = email, RememberMe = model.RememberMe, ReturnUrl = returnUrl, Purpose = purpose, MakePrimary = true });

                await _emailSender.SendEmailConfirmationAsync(email, callbackUrl, token);

                return RedirectToAction(nameof(TokenLoginManual), new TokenLoginViewModel { Email = email, RememberMe = model.RememberMe, ReturnUrl = returnUrl, Purpose = purpose, MakePrimary = true });

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
            if (model.Purpose != "Register" && model.Purpose != "Login" && model.Purpose != "AddEmail")
                return View("Message", new MessageViewModel() { MessageType = "Error", Message = "Unknown purpose." });

            var email = _userManager.NormalizeKey(model.Email);
            var user = await _userManager.FindByLoginAsync("Email", email);

            if (user == null)
            {
                if (model.Purpose == "Register")
                    user = await _userManager.FindByEmailAsync(email);
                else if (model.Purpose == "AddEmail")
                    user = await _userManager.GetUserAsync(User);
                else
                    return View("Message", new MessageViewModel() { MessageType = "Error", Message = "An unexpected error occured." });
            }

            var result = await _userManager.VerifyUserTokenAsync(user, "Email", model.Purpose, model.Token);

            if (result)
            {
                if (model.Purpose == "Login" || model.Purpose == "Register")
                {
                    await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);
                }
                if (model.Purpose == "AddEmail" || model.Purpose == "Register")
                {
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

            } else
            {
                return View("Message", new MessageViewModel() { MessageType = "Error", Message = "Problem validating code." });
            }

            // Success
            Console.WriteLine("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF" + model.ReturnUrl);
            _logger.LogDebug("GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG"+model.ReturnUrl);

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
            return View("Message", new MessageViewModel() { MessageType = "Success", Message = "You have been successfully logged out." });
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
