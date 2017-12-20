using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PwdLess.Data;
using PwdLess.Models.HomeViewModels;

namespace PwdLess.Controllers
{
    public class HomeController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public HomeController(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!_signInManager.IsSignedIn(User))
                return RedirectToAction(nameof(AccountController.Login), "Account");
            else
                return View();
        }

        [HttpGet]
        public IActionResult Notice(NoticeViewModel model)
        {
            return View(model);
        }
    }
}