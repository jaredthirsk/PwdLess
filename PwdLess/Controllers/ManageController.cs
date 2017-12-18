using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PwdLess.Models;
using PwdLess.Models.HomeViewModels;
using PwdLess.Models.ManageViewModels;
using PwdLess.Services;

namespace PwdLess.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class ManageController : Controller
    {
        private readonly NoticeService _notice;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger _logger;

        public ManageController(
          NoticeService notice,
          IConfiguration configuration,
          UserManager<ApplicationUser> userManager,
          SignInManager<ApplicationUser> signInManager,
          ILogger<ManageController> logger,
          UrlEncoder urlEncoder)
        {
            _notice = notice;
            _configuration = configuration;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> EditUserInfo()
        {
            var user = await _userManager.GetUserAsync(User); // TODO: following code is repeated literally in every method here. Maybe use a filter?
            if (user == null)
            {
                await _signInManager.SignOutAsync();
                return _notice.Error(this);
            }

            var model = new EditUserInfoViewModel
            {
                UserName = user.UserName,
                Logins = await _userManager.GetLoginsAsync(user),
                PrimaryEmail = user.EmailConfirmed ? user.Email : user.EmailFromExternalProvider,

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

            var email = _userManager.NormalizeKey(model.PrimaryEmail);
            var userLogins = await _userManager.GetLoginsAsync(user);

            model.Logins = userLogins; //Model binding doesn't work with IList

            if (!ModelState.IsValid)
                return View(model);

            user.UserName = model.UserName;
            user.FavColor = model.FavColor;
            
            
            if (user.EmailConfirmed &&
                user.NormalizedEmail != email &&
                userLogins.Any(l => l.LoginProvider == "Email" && l.ProviderKey == email))
            {
                user.Email = email;
            }

            var updateResult = await _userManager.UpdateAsync(user);
            
            if (!updateResult.Succeeded)
            {
                _notice.AddErrors(ModelState, updateResult);
                return View(model);
            }

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
                Logins = await _userManager.GetLoginsAsync(user),
                EmailFromExternalProvider = user.EmailFromExternalProvider
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

            var userLogins = await _userManager.GetLoginsAsync(user);
            if (userLogins.Count == 1 ||
               (String.IsNullOrWhiteSpace(user.EmailFromExternalProvider) && userLogins.Where(l => l.LoginProvider == "Email").Count() == 1 && model.LoginProvider == "Email"))
            {
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

            if (model.LoginProvider == "Email")
            {
                if (user.NormalizedEmail == _userManager.NormalizeKey(model.ProviderKey))
                {
                    user.Email = userLogins.FirstOrDefault(l => l.LoginProvider == "Email" && l.ProviderKey != model.ProviderKey)?.ProviderKey;
                    user.EmailConfirmed = user.Email == null ? false : true;
                    var updateResult = await _userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        _notice.AddErrors(ModelState);
                        return View(model);
                    }
                }
            }

            var removeLoginResult = await _userManager.RemoveLoginAsync(user, model.LoginProvider, model.ProviderKey);
            if (!removeLoginResult.Succeeded)
            {
                _notice.AddErrors(ModelState);
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);


            return _notice.Success(this, "Login successfully removed.");
        }
    }
}
