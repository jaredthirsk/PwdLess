using System;
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
using PwdLess.Models.AdminViewModels;
using PwdLess.Models.ManageViewModels;
using PwdLess.Services;

namespace PwdLess.Controllers
{
    [Authorize(Roles = "Administrator")]
    [Route("[controller]/[action]")]
    public class AdminController : Controller
    {
        private readonly EventsService _events;
        private readonly NoticeService _notice;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;

        public AdminController(
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
        public IActionResult Index()
        {
            return View(new AdminViewModel()
            {
                Users = _context.Users.Take(10).OrderBy(u => u.DateCreated).ToList(),
                UserCount = _userManager.Users.Count()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(AdminViewModel model)
        {
            var searchTerm = _userManager.NormalizeKey(model.SearchTerm);

            model.Users = _context.Users.Where(u => u.NormalizedUserName.Contains(searchTerm) ||
                                                   u.NormalizedEmail.Contains(searchTerm) ||
                                                   u.FullName.Contains(searchTerm) ||
                                                   u.Id.Contains(searchTerm))
                                        .ToList();
            model.UserCount = model.Users.Count();

            return View(model);
        }
        
        [HttpGet]
        public async Task<IActionResult> Lockout(string userName, int minutes = 120)
        {
            var user = await _userManager.FindByNameAsync(userName);

            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.UpdateSecurityStampAsync(user);
            await _userManager.SetLockoutEndDateAsync(user, new DateTimeOffset(DateTime.Now.AddMinutes(minutes)));

            return _notice.Success(this, $"User {user.UserName} locked out for {minutes} minutes.");
        }

        [HttpGet]
        public async Task<IActionResult> Impersonate(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);

            await _signInManager.SignOutAsync();
            await _signInManager.SignInAsync(user, false);

            return _notice.Success(this, $"You are now logged-in as {user.UserName}.", "Don't forget to log out later.");
        }
    }
}
