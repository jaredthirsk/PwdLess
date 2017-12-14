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
            var user = await _userManager.GetUserAsync(User); // TODO: following code is repeated literally in every method here. Maybe use a filter?
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
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                {
                    NoticeType = NoticeType.Error
                });
            }

            user.UserName = model.UserName;
            user.FavColor = model.FavColor;

            var email = _userManager.NormalizeKey(model.CommunicationEmail);
            var userLogins = await _userManager.GetLoginsAsync(user);
            
            if (user.EmailConfirmed &&
                user.NormalizedEmail != email &&
                userLogins.FirstOrDefault(l => l.LoginProvider == "Email" && l.ProviderKey == email) != null)
            {
                user.Email = email;
            }

            var updateResult = await _userManager.UpdateAsync(user);
            
            if (!updateResult.Succeeded)
            {
                AddErrors(updateResult);
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
            {
                NoticeType = NoticeType.Success,
                Title = "Your profile has been updated.",
                Description = " "
            });
        }

        [HttpGet]
        public async Task<IActionResult> Logins()
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

            var model = new LoginsViewModel { Logins = await _userManager.GetLoginsAsync(user) };

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
                return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                {
                    NoticeType = NoticeType.Error
                });
            }

            var userLogins = await _userManager.GetLoginsAsync(user);
            if (userLogins.Count <= 1)
            {
                var deleteResult = await _userManager.DeleteAsync(user);
                if (deleteResult.Succeeded)
                {
                    await _signInManager.SignOutAsync();
                    return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                    {
                        NoticeType = NoticeType.Success,
                        Title = "Your account has been successfully deleted.", // TODO: better deletion than keeps account for n days
                        Description = " "
                    });
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

            var removeLoginResult = await _userManager.RemoveLoginAsync(user, model.LoginProvider, model.ProviderKey);
            if (!removeLoginResult.Succeeded)
                return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
                {
                    NoticeType = NoticeType.Error
                });

            await _signInManager.SignInAsync(user, isPersistent: false);


            return RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
            {
                NoticeType = NoticeType.Success,
                Title = "Login successfully removed.",
                Description = " "
            });
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
