using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PwdLess.Models;
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

            return View();
        }

        [HttpGet]
        public IActionResult Notice(NoticeViewModel model)
        {
            return View(model);
        }
    }
}