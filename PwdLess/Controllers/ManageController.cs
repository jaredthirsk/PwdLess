using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PwdLess.Data;
using PwdLess.Models.ManageViewModels;
using PwdLess.Services;

namespace PwdLess.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class ManageController : Controller
    {
        private readonly EventsService _events;
        private readonly NoticeService _notice;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger _logger;

        public ManageController(
          EventsService events,
          NoticeService notice,
          IConfiguration configuration,
          UserManager<ApplicationUser> userManager,
          SignInManager<ApplicationUser> signInManager,
          ILogger<ManageController> logger)
        {
            _events = events;
            _notice = notice;
            _configuration = configuration;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> EditUserInfo()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                await _signInManager.SignOutAsync();
                return _notice.Error(this);
            }

            var model = new EditUserInfoViewModel
            {
                UserName = user.UserName,
                Logins = await _userManager.GetLoginsAsync(user),
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                FullName = user.FullName,

                FavColor = user.FavColor
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUserInfo(EditUserInfoViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                await _signInManager.SignOutAsync();
                _notice.AddErrors(ModelState);
                return View(model);
            }

            var emailToMakePrimary = _userManager.NormalizeKey(model.Email);
            var userLogins = await _userManager.GetLoginsAsync(user);

            model.Logins = userLogins; // Since model binding doesn't work with IList

            if (!ModelState.IsValid)
                return View(model);

            user.UserName = model.UserName;
            user.FullName = model.FullName;
            user.FavColor = model.FavColor; 

            // If the user's email is confirmed (ie. local login) and they provided a different email that exists, set it to the primary
            if (user.EmailConfirmed &&
                user.NormalizedEmail != emailToMakePrimary &&
                userLogins.Any(l => l.LoginProvider == "Email" && l.ProviderKey == emailToMakePrimary))
            {
                user.Email = emailToMakePrimary;
            }

            // Update sumbitted user info, including changing email if required
            var updateResult = await _userManager.UpdateAsync(user);
            
            if (!updateResult.Succeeded)
            {
                _notice.AddErrors(ModelState, updateResult);
                return View(model);
            }
            
            await _events.AddEvent(AuthEventType.EditUserInfo, 
                JsonConvert.SerializeObject(model), user);

            await _signInManager.RefreshSignInAsync(user);
            return _notice.Success(this, "Your profile has been updated.");
        }

        [HttpGet]
        public async Task<IActionResult> Logins()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                await _signInManager.SignOutAsync();
                return _notice.Error(this);
            }

            var model = new LoginsViewModel
            {
                Logins = await _userManager.GetLoginsAsync(user)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogin(RemoveLoginViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                await _signInManager.SignOutAsync();
                _notice.AddErrors(ModelState);
                return View(model);
            }

            // If user is removing last/only login or
            // if user is trying to remove thier only confirmed email,
            // then delete account.
            // This prevents users from ever "unconfirming" themselves.
            // Why: there should always be atleast 1 other confirmed
            // email to fallback to as the confirmed email when deleting the other
            // This is also checked for in the view.
            var userLogins = await _userManager.GetLoginsAsync(user);
            if (userLogins.Count == 1 ||
               (userLogins.Where(l => l.LoginProvider == "Email").Count() == 1 && model.LoginProvider == "Email"))
            {
                //await _events.AddEvent(AuthEventType.Delete,
                //    JsonConvert.SerializeObject(model), user);

                var deleteResult = await _userManager.DeleteAsync(user);
                if (deleteResult.Succeeded)
                {
                    await _signInManager.SignOutAsync();
                    return _notice.Success(this, "Your account has been successfully deleted."); // TODO: keep account for n days feature
                }
                else
                {
                    _notice.AddErrors(ModelState);
                    return View(model);
                }
            }

            // If user trying to remove their primary email then find another one.
            if (model.LoginProvider == "Email" && (user.NormalizedEmail == _userManager.NormalizeKey(model.ProviderKey)))
            {
                var fallbackPrimaryEmailLogin = userLogins.FirstOrDefault(l => l.LoginProvider == "Email" && l.ProviderKey != model.ProviderKey);

                if (fallbackPrimaryEmailLogin == null) // This should never happen thanks to the check above.
                {
                    _notice.AddErrors(ModelState);
                    return View(model);
                }

                user.Email = fallbackPrimaryEmailLogin.ProviderKey;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    _notice.AddErrors(ModelState);
                    return View(model);
                }
            }

            await _events.AddEvent(AuthEventType.RemoveLogin,
                JsonConvert.SerializeObject(model), user);

            var removeLoginResult = await _userManager.RemoveLoginAsync(user, model.LoginProvider, model.ProviderKey);
            if (!removeLoginResult.Succeeded)
            {
                _notice.AddErrors(ModelState);
                return View(model);
            }

            await _userManager.UpdateSecurityStampAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            return _notice.Success(this, "Login successfully removed.");
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                await _signInManager.SignOutAsync();
                return _notice.Error(this);
            }

            var model = new HistoryViewModel
            {
                Events = _events.GetEvents(user)
            };

            return View(model);
        }

    }
}
