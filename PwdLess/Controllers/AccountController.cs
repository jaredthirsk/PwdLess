using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PwdLess.Data;
using PwdLess.Filters;
using PwdLess.Models;
using PwdLess.Models.AccountViewModels;
using PwdLess.Models.HomeViewModels;
using PwdLess.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PwdLess.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly EventsService _events;
        private readonly NoticeService _notice;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;

        public AccountController(
            EventsService events,
            NoticeService notice,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _events = events;
            _notice = notice;
            _configuration = configuration;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailSender = emailSender;
            _logger = logger;


        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            var user = await _userManager.GetUserAsync(User);

            var model = new LoginViewModel();

            if (user != null)
            {
                model.DidReachMaxLoginsAllowed = DidReachMaxLoginsAllowed(user);
                model.MaxLoginsAllowed = MaxLoginsAllowed;
            }

            return View(model);
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
                    // Check to see if user reached max logins
                    if (DidReachMaxLoginsAllowed(userCurrentlySignedIn))
                    {
                        _notice.AddErrors(ModelState, $"Sorry, you've reached the maximum allowed number of logins ({MaxLoginsAllowed}).");
                        return View(nameof(Login));
                    }
                    
                    attemptedOperation = AuthOperation.AddingNovelEmail;
                }
                else // Email associated with another user's account
                {
                    if (userWithConfirmedEmail.Id == userCurrentlySignedIn.Id) // Email already added to user's account
                    {
                        _notice.AddErrors(ModelState, "This email is already in your account.");
                        return View(model);
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

            // Add a space every 3 characters for readability
            token = String.Concat(token.SelectMany((c, i)
                                            => (i+1) % 3 == 0 ? $"{c} " : $"{c}"));

            var callbackUrl = Url.TokenInputLink(Request.Scheme,
                new TokenInputViewModel
                {
                    Token = token,
                    RememberMe = model.RememberMe,
                    ReturnUrl = returnUrl,
                    Email = email, 
                    Purpose = purpose
                });

            // Will not wait for email to be sent
            #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            _emailSender.SendTokenAsync(email, attemptedOperation, callbackUrl, token);
            #pragma warning restore CS4014

            return View(nameof(TokenInput), 
                new TokenInputViewModel
                {
                    RememberMe = model.RememberMe,
                    ReturnUrl = returnUrl,
                    Email = email,
                    Purpose = purpose
                });
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult TokenInput(TokenInputViewModel model)
        {   
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [ActionName(nameof(AccountController.TokenInput))]
        [ServiceFilter(typeof(ValidateRecaptchaAttribute))]
        public async Task<IActionResult> SubmitTokenInput(TokenInputViewModel model)
        {
            if (!ModelState.IsValid ||
                String.IsNullOrWhiteSpace(model.Email) ||
                String.IsNullOrWhiteSpace(model.Purpose) ||
                String.IsNullOrWhiteSpace(model.Token))
            {
                return View(model);
            }

            var email = _userManager.NormalizeKey(model.Email);
            model.Token = model.Token.Replace(" ", "");
            
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
                await _signInManager.SignOutAsync();

                isTokenValid = await _userManager.VerifyUserTokenAsync(
                    userWithConfirmedEmail  // Case: logging-in
                    ?? userEmpty,           // Case: registering,
                    "Email", model.Purpose, model.Token);
            }
            else // Trying to add email
            {
                if (userCurrentlySignedIn == null) // If the user is not signed in, prompt them to, with the return url leading back here
                    return RedirectToAction(nameof(Login), new
                    {
                        returnUrl = Request.Path + Request.QueryString
                    });

                isTokenValid = await _userManager.VerifyUserTokenAsync(
                    userCurrentlySignedIn,
                    "Email", model.Purpose, model.Token);
            }

            if (!isTokenValid)
            {
                _notice.AddErrors(ModelState, "Error validating code, it might have expired. Please try again!");
                return View(model);
            }

            // Invalidates all tokens for user when trying to login or add login
            // Note: this also invalidates any attempts to add more logins than allowed
            if ((userCurrentlySignedIn ?? userWithConfirmedEmail) != null)
            {
                var updateSecStampResult = await _userManager.UpdateSecurityStampAsync(userCurrentlySignedIn ?? userWithConfirmedEmail);
                if (!updateSecStampResult.Succeeded)
                {
                    _notice.AddErrors(ModelState);
                    return View(model);
                }
            }

            // Valid {token + email (user) + purpose} supplied

            if (model.Purpose == "RegisterOrLogin") // Trying to register or login
            {
                if (userWithConfirmedEmail == null) // Success trying to register
                {
                    var token = await _userManager.GenerateUserTokenAsync(userEmpty, "Default", "Register");

                    return View(nameof(Register), new RegisterViewModel
                    {
                        RememberMe = model.RememberMe,
                        Email = email,
                        UserName = email.Split('@')[0]?.ToLower(),
                        Token = token,
                        ReturnUrl = model.ReturnUrl
                    });
                }
                else // Success trying to login
                {
                    await _events.AddEvent(AuthEventType.Login, JsonConvert.SerializeObject(new
                    {
                        LoginProvider = "Email",
                        ProviderKey = model.Email
                    }), userWithConfirmedEmail);

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
                    {
                        _notice.AddErrors(ModelState, addLoginResult);
                        return View(model);
                    }

                    userCurrentlySignedIn.Email = email;
                    userCurrentlySignedIn.EmailConfirmed = true;
                    var updateUserResult = await _userManager.UpdateAsync(userCurrentlySignedIn);

                    if (!updateUserResult.Succeeded)
                    {
                        _notice.AddErrors(ModelState, updateUserResult);
                        return View(model);
                    }

                    await _events.AddEvent(AuthEventType.AddLogin, JsonConvert.SerializeObject(new
                    {
                        LoginProvider = "Email",
                        ProviderKey = model.Email
                    }), userCurrentlySignedIn);
                    
                }
                else // Email to be added is in use
                {
                    // Note: this area is unlikely to be reached since security stamp is changed once a login is added
                    if (userWithConfirmedEmailToAdd.Id == userCurrentlySignedIn.Id) // Email is already in user's account 
                    {
                        _notice.AddErrors(ModelState, "This email is already in your account.");
                        return View(model);
                    }
                    else // Email associated with another account (same user since both verified!)
                    {
                        _notice.AddErrors(ModelState, "This email is in another user's account. Try logging in using that email instead.");
                        return View(model);
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
            var nameFromExternalLoginProvider = _userManager.NormalizeKey(info.Principal.FindFirstValue(ClaimTypes.Name));

            if (userCurrentlySignedIn == null) // No locally signed-in user (trying to register or login)
            {
                if (userWithExternalLogin != null) // User exists and attempting to login
                {
                    var externalLoginResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

                    if (externalLoginResult.Succeeded) // Success logging in
                    {
                        await _events.AddEvent(AuthEventType.Login, JsonConvert.SerializeObject(new
                        {
                            info.LoginProvider,
                            info.ProviderKey
                        }), userWithExternalLogin);

                        return RedirectToLocal(returnUrl);
                    }

                    if (externalLoginResult.IsLockedOut || externalLoginResult.IsNotAllowed)
                        return RedirectToAction(nameof(Lockout));
                }

                // The user does not have an account, is attempting to register
                return View(nameof(Register), new RegisterViewModel
                {
                    Email = emailFromExternalLoginProvider,
                    UserName = (nameFromExternalLoginProvider?.Replace(" ","_") ?? emailFromExternalLoginProvider.Split('@')[0]).ToLower(),
                    ExternalLoginProviderDisplayName = info.ProviderDisplayName,
                    ReturnUrl = returnUrl
                });

            }
            else // A user is currently locally signed-in (trying to add external login)
            {

                if (userWithExternalLogin != null) // External login already in use
                {
                    if (userWithExternalLogin.Id == userCurrentlySignedIn.Id) // External login is already in user's account
                    {
                        _notice.AddErrors(ModelState, "This external login is already in your account.");
                        return View(nameof(Login));
                    }
                    else
                    {
                        _notice.AddErrors(ModelState, "This external login is in another user's account. Try loggin out then back in with that instead.");
                        return View(nameof(Login));
                    }
                }

                // Check to see if user reached max logins
                if (DidReachMaxLoginsAllowed(userCurrentlySignedIn))
                {
                    _notice.AddErrors(ModelState, $"Sorry, you've reached the maximum allowed number of logins ({MaxLoginsAllowed}).");
                    return View(nameof(Login));
                }

                // If email is not confirmed then update their unconfirmed email
                if (!String.IsNullOrWhiteSpace(emailFromExternalLoginProvider) &&
                    userCurrentlySignedIn.EmailConfirmed == false)
                {
                    userCurrentlySignedIn.Email = emailFromExternalLoginProvider;
                    userCurrentlySignedIn.EmailConfirmed = false;
                }
                    
                var addLoginResult = await _userManager.AddLoginAsync(userCurrentlySignedIn, info);
                if (addLoginResult.Succeeded)
                {
                    var updateResult = await _userManager.UpdateAsync(userCurrentlySignedIn);
                    if (updateResult.Succeeded)
                    {
                        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme); // Clear the existing external cookie to ensure a clean login process

                        await _events.AddEvent(AuthEventType.AddLogin, JsonConvert.SerializeObject(new
                        {
                            info.LoginProvider,
                            info.ProviderKey
                        }), userCurrentlySignedIn);

                        return RedirectToLocal(returnUrl);
                    }
                }

                _notice.AddErrors(ModelState);
                return View(nameof(Login));
            }

        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid || 
                String.IsNullOrWhiteSpace(model.Email)) //  Note: this means that external logins not providing an email are unusable.
                return View("Register", model);

            var email = _userManager.NormalizeKey(model.Email);
            
            UserLoginInfo info = await _signInManager.GetExternalLoginInfoAsync();

            var userEmpty = new ApplicationUser()
            {
                UserName = model.UserName,
                Email = email,
                DateCreated = DateTimeOffset.UtcNow,
                SecurityStamp = "",

                FullName = model.FullName,
                FavColor = model.FavColor,
            };

            userEmpty.Email = email;

            if (info == null) // User trying to register locally
            {
                userEmpty.EmailConfirmed = true;

                var userWithConfirmedEmail = await _userManager.FindByLoginAsync("Email", email);

                userEmpty.Id = email; // Only for token verification, is set to null later
                var isTokenValid = await _userManager.VerifyUserTokenAsync(userEmpty, "Default", "Register", model.Token);

                if (isTokenValid && userWithConfirmedEmail == null) // Supplied email is verified & user does not exist
                {
                    userEmpty.Id = null;
                    info = new UserLoginInfo("Email", userEmpty.Email, "Email");
                }
                else
                {
                    _notice.AddErrors(ModelState);
                    return View(nameof(Register), model);
                }

            }
            else // User trying to register after external login
            {
                userEmpty.EmailConfirmed = false;
            }
            var createResult = await _userManager.CreateAsync(userEmpty);            

            if (createResult.Succeeded)
            {
                var addLoginResult = await _userManager.AddLoginAsync(userEmpty, info);

                if (addLoginResult.Succeeded)
                {
                    var user = await _userManager.FindByNameAsync(model.UserName); // This works because usernames are unique

                    // If this is the first user ever created, make an Administrator
                    if (_userManager.Users.Count() == 1)
                    {
                        var makeAdminResult = await _userManager.AddToRoleAsync(user, "Administrator");
                    }

                    await _events.AddEvent(AuthEventType.Register, JsonConvert.SerializeObject(new
                    {
                        LoginProvider = info?.LoginProvider ?? "Email",
                        ProviderKey = info?.ProviderKey ?? email
                    }), user);
                    
                    await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);
                    return RedirectToLocal(model.ReturnUrl); // Success
                }
            }
            else
            {
                _notice.AddErrors(ModelState);
                return View(nameof(Register), model);
            }


            await _userManager.DeleteAsync(userEmpty);

            _notice.AddErrors(ModelState);
            return View(nameof(Register), model);
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

        private int MaxLoginsAllowed { get
            {
                return Int32.Parse(_configuration["PwdLess:Logins:MaxLoginsAllowed"]);
            } }

        private bool DidReachMaxLoginsAllowed(ApplicationUser user)
        {
            // Check to see if user reached max logins

            var userLoginCount = _context.UserLogins.Count(l => l.UserId == user.Id);
            return userLoginCount >= MaxLoginsAllowed;
        }

        #endregion
    }
}
