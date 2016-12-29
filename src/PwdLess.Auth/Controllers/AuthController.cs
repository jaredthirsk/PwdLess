using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PwdLess.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using PwdLess.Auth.Data;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace PwdLess.Auth.Controllers
{
    [Route("api/[controller]/[action]")]
    public class AuthController : Controller
    {
        private IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        
        public async Task<IActionResult> SendJwt(string email)
        {
            try
            {
                await _authService.FullLogin(email); // create account, generate jwt, send in email
            }
            catch (Exception)
            {
                return BadRequest("Something went wrong.");
            }
            

            return Ok($"Success! Sent JWT to: {email}");
        }

        public IActionResult Echo(string echo) // for testing
        {
            return Content(echo);
        }
    }
}
