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
    [Authorize(Roles = "Administrator")]
    [Route("[controller]/[action]")]
    public class AdminController : Controller
    {
        private readonly EventsService _events;
        private readonly NoticeService _notice;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger _logger;

        public AdminController(
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
        public IActionResult Index()
        {
            return View();
        }
        
    }
}
