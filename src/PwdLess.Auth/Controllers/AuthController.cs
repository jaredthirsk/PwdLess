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
using System.Text;

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
        
        public async Task<IActionResult> SendNonce(string identifier)
        {
            try
            {
                // create a nonce/token pair, store them, get Nonce
                var nonce = await _authService.CreateAndStoreNonce(identifier);
                
                // create body for message to be sent to user
                var body = _templateProcessor.ProcessTemplate(nonce);

                // send message user
                await _senderService.SendAsync(identifier, body);

                _logger.LogDebug($"A message was sent to: {identifier}. It contained the body: {body}.");
                return Ok($"Success! Sent Nonce to: {identifier}.");
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return BadRequest("Something went wrong.");
            }
            
        }
        
        public async Task<IActionResult> NonceToToken(string nonce)
        {
            try
            {
                // Get a Nonce's associated token
                var token = await _authService.GetTokenFromNonce(nonce);

                _logger.LogDebug($"Nonce: {nonce}, token sent: {token}");
                return Ok(token);
            }
            catch (IndexOutOfRangeException)
            {
                _logger.LogDebug($"A requested nonce's token was not found. Nonce: {nonce}.");
                return NotFound("Nonce not found.");
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return BadRequest("Something went wrong.");
            }

        }
    }
}
