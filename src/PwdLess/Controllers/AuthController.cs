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
        private IDistributedCache _cache;
        private ILogger _logger;

        public AuthController(IAuthService authService, 
            ISenderService senderService, 
            ICallbackService callbackService,
            IDistributedCache cache,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _senderService = senderService;
            _callbackService = callbackService;
            _cache = cache;
            _logger = logger;
        }
        
        public async Task<IActionResult> SendNonce(string contact/*, string extraData = "email"*/)
        {
            try
            {
                //if (_authCtx.UserEmails.Contains(new UserContacts { Contact = email }))
                //    _authCtx.Nonces.Add(new Nonce { Content = _authService. })

                if (_authService.DoesContactExist(contact)) // Returning user
                    await _senderService.SendAsync(contact, await _authService.AddNonce(contact, false), "ReturningUser");
                else // New user
                    await _senderService.SendAsync(contact, await _authService.AddNonce(contact, true), "NewUser");

                // create a nonce/token pair, store them, get Nonce
                //var nonce = await _authService.CreateAndStoreNonce(email, type);
                
                // run the BeforeSendingNonce callback, put result in message body
                //var extraBodyData = await _callbackService.BeforeSendingNonce(email, type) ?? "{}";
                
                // create body for message to be sent to user & send it
                //var body = _templateProcessor.ProcessTemplate(nonce, extraBodyData, type);
                //await _senderService.SendAsync(email, body);
                
                _logger.LogDebug($"A nonce was sent to {contact}.");
                return Ok($"Success! Sent nonce to {contact}.");
            }
            catch (InvalidIdentifierException e)
            {
                _logger.LogError(e.ToString());
                return BadRequest("Identifier invalid.");
            }  
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return BadRequest("Something went wrong.");
            }
            
        }
        
        //public async Task<IActionResult> NonceToToken(string nonce)
        //{
        //    try
        //    {
        //        // get a Nonce's associated token
        //        var token = await _authService.GetTokenFromNonce(nonce);
        //
        //        // run the BeforeSendingToken callback, discard result
        //        await _callbackService.BeforeSendingToken(token);
        //
        //        _logger.LogDebug($"Nonce: {nonce}, token sent: {token}");
        //        return Ok(token);
        //    }
        //    catch (IndexOutOfRangeException)
        //    {
        //        _logger.LogDebug($"A requested nonce's token was not found. Nonce: {nonce}.");
        //        return NotFound("Nonce not found.");
        //    }
        //    catch (Exception e)
        //    {
        //        _logger.LogError(e.ToString());
        //        return BadRequest("Something went wrong.");
        //    }
        //
        //}

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
