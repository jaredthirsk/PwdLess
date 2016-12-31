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
        
        /// <summary>
        /// Sends a TOTP to `identifier` (ie. email address).
        /// In the process a token is created and stored in cache by AuthService.
        /// </summary>
        /// <param name="identifier">Eg. a user's email address or phone number.</param>
        /// <returns>No significant response.</returns>
        public async Task<IActionResult> SendTotp(string identifier)
        {
            try
            {
                // create a TOTP/token pair, store them, get TOTP
                var totp = await _authService.CreateAndStoreTotp(identifier);
                
                // create body for message to be sent to user
                var body = _templateProcessor.ProcessTemplate(totp);

                // send message user
                await _senderService.SendAsync(identifier, body);

                return Ok($"Success! Sent TOTP to: {identifier}");
            }
            catch (Exception)
            {
                return BadRequest("Something went wrong.");
            }
            
        }

        /// <summary>
        /// Retrieves a TOTP's associated token and returns it.
        /// See `/SendTotp` to associate a TOTP with a token.
        /// </summary>
        /// <param name="totp">The TOTP to find an associated token for.</param>
        /// <returns>Responds with token if sucessful.</returns>
        public async Task<IActionResult> TotpToToken(string totp)
        {
            try
            {
                // Get a TOTP's associated token
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
