using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PwdLess.Controllers;
using PwdLess.Models.HomeViewModels;

namespace PwdLess.Services
{
    public class NoticeService
    {
        public void AddErrors(ModelStateDictionary modelState,
                                IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                modelState.AddModelError("", error.Description);
            }
        }

        public void AddErrors(ModelStateDictionary modelState,
                                string error = "An unexpected error occured. Please try again later.")
        {
            modelState.AddModelError("", error);
        }

        public IActionResult Success(ControllerBase controller, 
                                        string title = " ", string description = " ", bool showBackButton = false)
        {
            return controller.RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
            {
                NoticeType = NoticeType.Success,
                Title = title,
                Description = description,
                ShowBackButton = showBackButton
            });

        }

        public IActionResult Error(ControllerBase controller,
                                        string title = " ", string description = " ", bool showBackButton = false)
        {
            return controller.RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
            {
                NoticeType = NoticeType.Error,
                Title = title,
                Description = description,
                ShowBackButton = showBackButton
            });

        }

        public IActionResult Warning(ControllerBase controller,
                                string title = " ", string description = " ", bool showBackButton = false)
        {
            return controller.RedirectToAction(nameof(HomeController.Notice), "Home", new NoticeViewModel
            {
                NoticeType = NoticeType.Warning,
                Title = title,
                Description = description,
                ShowBackButton = showBackButton
            });

        }
    }
}
