using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PwdLess.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PwdLess.Models;

namespace PwdLess.Controllers
{
    [Route("[controller]/[action]")]
    public class AuthController : Controller
    {
        private IAuthService _authService;
        private ISenderService _senderService;
        private ICallbackService _callbackService;
        private ILogger _logger;

        public AuthController(IAuthService authService, 
            ISenderService senderService, 
            ICallbackService callbackService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _senderService = senderService;
            _callbackService = callbackService;
            _logger = logger;
        }
        
        public async Task<IActionResult> SendNonce(string contact/*, string extraData = "email"*/)
        {
            try
            {
                if (_authService.DoesContactExist(contact)) // Returning user
                    await _senderService.SendAsync(contact, await _authService.AddNonce(contact, false), "ReturningUser");
                else // New user
                    await _senderService.SendAsync(contact, await _authService.AddNonce(contact, true), "NewUser");
                
                _logger.LogDebug($"A nonce was sent to {contact}.");
                return Ok($"Success! Sent nonce to {contact}.");
            }
            //catch (InvalidContactException e)
            //{
            //    _logger.LogError(e.ToString());
            //    return BadRequest("Contact invalid.");
            //}  
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return BadRequest("Something went wrong.");
            }
            
        }

        public async Task<IActionResult> NonceToRefreshToken(string nonce, User user = null)
        {
            try
            {
                _authService.ValidateNonce(nonce);

                var contact = _authService.ContactOfNonce(nonce);

                if (_authService.IsNonceIsRegistering(nonce))
                {
                    if (!ModelState.IsValid)
                        return BadRequest("You need to supply all additional user infomation.");

                    user.UserId = (string.Concat(Guid.NewGuid().ToString().Replace("-", "").Take(12)); /* ?? user.UserId */ // Users can't choose their own UserId, or TODO: they can but it can't be "admin"

                    await _authService.AddUser(user); // TODO: batch all db CUD calls together
                    await _authService.AddUserContact(user.UserId, contact);
                }

                
                var userId = _authService.UserIdOfContact(contact);
                var refreshToken = await _authService.AddRefreshToken(userId);

                await _authService.DeleteNonce(contact);

                
                //// get a Nonce's associated token
                //var token = await _authService.GetTokenFromNonce(nonce);
                //
                //// run the BeforeSendingToken callback, discard result
                //await _callbackService.BeforeSendingToken(token);
        
                _logger.LogDebug($"Refresh token sent: {refreshToken}");
                return Ok(refreshToken);
            }
            catch (ExpiredException e)
            {
                _logger.LogDebug($"A requested nonce was expired. Nonce: {nonce}. Exception: {e}");
                return NotFound("Nonce expired.");
            }
            catch (IndexOutOfRangeException e)
            {
                _logger.LogDebug($"A requested nonce's contact was not found. Nonce: {nonce}. Exception: {e}");
                return NotFound("Nonce not found.");
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return BadRequest("Something went wrong.");
            }
        
        }

        public IActionResult RefreshTokenToAccessToken(string refreshToken)
        {
            try
            {
                _authService.ValidateRefreshToken(refreshToken);
                string accessToken = _authService.RefreshTokenToAccessToken(refreshToken);

                _logger.LogDebug($"Access token sent: {accessToken}");
                return Ok(accessToken);
            }
            catch (ExpiredException e)
            {
                _logger.LogDebug($"A refrenced refresh token was expired. Refresh token: {refreshToken}. Exception: {e}");
                return NotFound("Refresh token not found.");
            }
            catch (IndexOutOfRangeException e)
            {
                _logger.LogDebug($"A refrenced refresh token was not found. Refresh token: {refreshToken}. Exception: {e}");
                return NotFound("Refresh token not found.");
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return BadRequest("Something went wrong.");
            }
        }

        [Authorize]
        public async Task<IActionResult> RevokeRefreshToken()
        {
            try
            {
                string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

                await _authService.RevokeRefreshToken(userId);


                _logger.LogDebug($"Refresh token for user was revoked: {userId}.");
                return Ok($"Success! Refresh token revoked.");
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return BadRequest("Something went wrong.");
            }
        }



        [Authorize]
        /// Validates tokens sent via authorization header
        /// Eg. Authorization: Bearer [token]
        ///     client_id    : defaultClient
        public IActionResult ValidateToken()
        {
            // Convert claims to JSON
            var sb = new StringBuilder();
            sb.Append("{"); // add opening parens
            foreach (var claim in HttpContext.User.Claims)
            {
                // add "key : value,"
                sb.Append($"\n\t\"{claim.Type.ToString()}\" : \"{claim.Value.ToString()}\",");
            }
            sb.Length--; // remove last comma
            sb.Append("\n}"); // add closing parens
            //sb.Replace("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "sub");
            var claimsJson = sb.ToString();


            return Ok(claimsJson);
        }
    }
}
