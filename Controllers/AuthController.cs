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
using PwdLess.Filters;

namespace PwdLess.Controllers
{
    [Route("[controller]/[action]")]
    public class AuthController : Controller
    {
        private AuthRepository _authRepo;
        private ISenderService _senderService;
        private ILogger _logger;

        public AuthController(AuthRepository authRepo, 
            ISenderService senderService,
            ILogger<AuthController> logger)
        {
            _authRepo = authRepo;
            _senderService = senderService;
            _logger = logger;
        }
        
        [HandleExceptions]
        public async Task<IActionResult> SendNonce(string contact, bool isAddingContact = false /*, string extraData = "email"*/)
        {
            if (_authRepo.DoesContactExist(contact)) // Returning user
                await _senderService.SendAsync(contact, _authRepo.AddNonce(contact, UserState.ReturningUser), UserState.ReturningUser);
            else if (isAddingContact) // Returning user adding contact
                await _senderService.SendAsync(contact, _authRepo.AddNonce(contact, UserState.AddingContact), UserState.AddingContact);
            else // New user
                await _senderService.SendAsync(contact, _authRepo.AddNonce(contact, UserState.NewUser), UserState.NewUser);

            await _authRepo.SaveDbChangesAsync();
            return Ok();   
        }

        [HandleExceptions]
        public async Task<IActionResult> NonceToRefreshToken(string nonce, User user = null)
        {
            string userId;
            string contact = _authRepo.GetContactOfNonce(nonce);

            if (_authRepo.GetNonceUserState(nonce) == UserState.NewUser && !_authRepo.DoesContactExist(contact))
            {
                if (!ModelState.IsValid)
                    return BadRequest("You need to supply all additional user infomation.");

                userId = _authRepo.AddUser(user);
                _authRepo.AddUserContact(userId, contact);
                await _authRepo.SaveDbChangesAsync();
            }
            else {
                userId = _authRepo.GetUserIdOfContact(contact);
            }
            
            string refreshToken = _authRepo.AddRefreshToken(userId);
            _authRepo.RemoveNonce(contact);

            await _authRepo.SaveDbChangesAsync();
            return Ok(refreshToken);
        }

        [Authorize, SetUserId, HandleExceptions]
        public async Task<IActionResult> NonceToAddContact(string nonce, string userId)
        {
            if (_authRepo.GetNonceUserState(nonce) != UserState.AddingContact)
                return BadRequest("Sorry, this nonce is not intended for that use.");

            string contact = _authRepo.GetContactOfNonce(nonce);
            _authRepo.AddUserContact(userId, contact);
            _authRepo.RemoveNonce(contact);

            await _authRepo.SaveDbChangesAsync();
            return Ok();
        }

        [Authorize, SetUserId, HandleExceptions]
        public async Task<IActionResult> RemoveContact(string contact, string userId)
        {
            if (!_authRepo.IsContactRemovable(userId, contact)) return BadRequest($"Sorry! Can't remove this contact.");

            _authRepo.RemoveUserContact(contact, userId);

            await _authRepo.SaveDbChangesAsync();
            return Ok();
        }


        [HandleExceptions]
        public IActionResult RefreshTokenToAccessToken(string refreshToken)
        {
            string accessToken = _authRepo.RefreshTokenToAccessToken(refreshToken);
            
            return Ok(accessToken);
        }

        [Authorize, SetUserId, HandleExceptions]
        public async Task<IActionResult> RevokeRefreshToken(string userId)
        {
            _authRepo.RemoveRefreshTokens(userId);
            await _authRepo.SaveDbChangesAsync();
            return Ok();
        }



        [Authorize, HandleExceptions]
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
            sb.Replace("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "sub");
            var claimsJson = sb.ToString();


            return Ok(claimsJson);
        }
    }
}
