using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PwdLess.Models;
using PwdLess.Models.HomeViewModels;
using PwdLess.Models.ManageViewModels;

namespace PwdLess.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class ManageController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger _logger;

        public ManageController(
          UserManager<ApplicationUser> userManager,
          SignInManager<ApplicationUser> signInManager,
          ILogger<ManageController> logger,
          UrlEncoder urlEncoder)
        {
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
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                {
                    NoticeType = NoticeType.Error
                });
            }

            var model = new EditUserInfoViewModel
            {
                UserName = user.UserName,
                Logins = await _userManager.GetLoginsAsync(user),
                CommunicationEmail = user.EmailConfirmed ? user.Email : user.EmailFromExternalProvider,

                FavColor = user.FavColor
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUserInfo(EditUserInfoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            user.UserName = model.UserName;
            user.FavColor = model.FavColor;
            

            var userLogins = await _userManager.GetLoginsAsync(user);
            var commEmail = _userManager.NormalizeKey(model.CommunicationEmail);

            if (user.EmailConfirmed &&
                user.NormalizedEmail != commEmail &&
                userLogins.FirstOrDefault(l => l.LoginProvider == "Email" && l.ProviderKey == commEmail) != null)
            {
                user.Email = commEmail;
            }

            var updateResult = await _userManager.UpdateAsync(user);
            
            if (!updateResult.Succeeded)
            {
                AddErrors(updateResult);
                return View(model);
                throw new ApplicationException($"Unexpected error occurred updating user with ID '{user.Id}'.");
            }

            await _signInManager.RefreshSignInAsync(user);
            //StatusMessage = "Your profile has been updated";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Logins()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var model = new LoginsViewModel { Logins = await _userManager.GetLoginsAsync(user) };

            //model.StatusMessage = StatusMessage;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogin(RemoveLoginViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var userLogins = await _userManager.GetLoginsAsync(user);
            if (userLogins.Count <= 1)
            {
                var deleteResult = await _userManager.DeleteAsync(user);
                if (deleteResult.Succeeded)
                {
                    await _signInManager.SignOutAsync();
                    return RedirectToAction(nameof(HomeController.Index), "Home");
                }
                else
                {
                    return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                    {
                        NoticeType = NoticeType.Error
                    });
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
                        return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                        {
                            NoticeType = NoticeType.Error
                        });
                }
            }

            var result = await _userManager.RemoveLoginAsync(user, model.LoginProvider, model.ProviderKey);
            if (!result.Succeeded)
            {
                throw new ApplicationException($"Unexpected error occurred removing external login for user with ID '{user.Id}'.");
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            //StatusMessage = "The external login was removed.";
            return RedirectToAction(nameof(Logins));
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        #endregion
    }
}
