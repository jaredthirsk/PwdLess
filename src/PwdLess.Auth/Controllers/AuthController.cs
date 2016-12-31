using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PwdLess.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

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
        private ILogger _logger;

        public AuthController(IAuthService authService, 
            ISenderService senderService, 
            ITemplateProcessor templateProcessor, 
            IDistributedCache cache,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _senderService = senderService;
            _templateProcessor = templateProcessor;
            _cache = cache;
            _logger = logger;
        }
        
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

                _logger.LogDebug($"A message was sent to: {identifier}. It contained the body: {body}");
                return Ok($"Success! Sent TOTP to: {identifier}");
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return BadRequest("Something went wrong.");
            }
            
        }
        
        public async Task<IActionResult> TotpToToken(string totp)
        {
            try
            {
                // Get a TOTP's associated token
                var token = await _authService.GetTokenFromTotp(totp);

                _logger.LogDebug("TOTP: {totp}, token sent: {token}");
                return Ok(token);
            }
            catch (IndexOutOfRangeException)
            {
                _logger.LogDebug($"A requested TOTP's token was not found. TOTP: {totp}");
                return NotFound("TOTP not found.");
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return BadRequest("Something went wrong.");
            }

        }
        
        public IActionResult Echo(string echo) // for testing
        {
            _logger.LogDebug($"Echoed: {echo}");
            return Content(echo);
        }
    }
}
