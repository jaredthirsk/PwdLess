using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PwdLess.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using PwdLess.Auth.Data;
using PwdLess.Auth.Models;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace PwdLess.Auth.Controllers
{
    [Route("api/[controller]/[action]")]
    public class AuthController : Controller
    {
        private IAuthService _authService;
        private IUsersRepository _usersData;
        public AuthController(IAuthService authService, IUsersRepository usersRepository)
        {
            _authService = authService;
            _usersData = usersRepository;
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

        /* Delete email feature: I think its useless since you can just delete form databse using SQL directly
        [Authorize]
        public async Task<IActionResult> Delete(string email)
        {
            try
            {
                var currentUser = HttpContext.User.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).Select(c => c.Value).ToList()[0];
                if (currentUser == email)
                {
                    await _usersData.RemoveUser(email);
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception e) // Note: InMemoryUsersRepository throws a NullReferenceException but you can ignore that.
            {
                Console.WriteLine(e.ToString());
                return BadRequest("Something went wrong.");
            }

            return Ok($"Success! User deleted: {email}");
        }
        */

        public IActionResult Echo(string echo) // for testing
        {
            return Content(echo);
        }
    }
}
