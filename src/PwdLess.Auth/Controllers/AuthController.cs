using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PwdLess.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace PwdLess.Auth.Controllers
{
    [Route("api/[controller]/[action]")]
    public class AuthController : Controller
    {
        private IAuthService _authService;
        private IDistributedCache _cache;

        public AuthController(IAuthService authService, IDistributedCache cache)
        {
            _authService = authService;
            _cache = cache;
        }
        
        public async Task<IActionResult> SendTotp(string email)
        {
            try
            {
                await _authService.FullLogin(email); // generate token & totp, store in chache, send totp in email
            }
            catch (Exception)
            {
                return BadRequest("Something went wrong.");
            }
            

            return Ok($"Success! Sent TOTP to: {email}");
        }


        public IActionResult Echo(string echo) // for testing
        {
            return Content(echo);
        }
    }
}
