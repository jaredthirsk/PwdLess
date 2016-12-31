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
    [Route("[controller]/[action]")]
    public class AuthController : Controller
    {
        private IAuthService _authService;
        private ISenderService _senderService;
        private ITemplateProcessor _templateProcessor;
        private IDistributedCache _cache;

        public AuthController(IAuthService authService, 
            ISenderService senderService, 
            ITemplateProcessor templateProcessor, 
            IDistributedCache cache)
        {
            _authService = authService;
            _senderService = senderService;
            _templateProcessor = templateProcessor;
            _cache = cache;
        }
        
        public async Task<IActionResult> SendTotp(string identifier)
        {
            try
            {
                var totp = await _authService.CreateAndStoreTotp(identifier);
                
                var body = _templateProcessor.ProcessTemplate(totp);

                await _senderService.SendAsync(identifier, body);

                return Ok($"Success! Sent TOTP to: {identifier}");
            }
            catch (Exception)
            {
                return BadRequest("Something went wrong.");
            }
            
        }

        public async Task<IActionResult> TotpToToken(string totp)
        {
            try
            {
                var token = await _authService.GetTokenFromTotp(totp);
                return Ok(token);
            }
            catch (IndexOutOfRangeException)
            {
                return BadRequest("TOTP not found.");
            }
            catch (Exception)
            {
                return BadRequest("Something went wrong.");
            }

        }

        public IActionResult Echo(string echo) // for testing
        {
            return Content(echo);
        }
    }
}
