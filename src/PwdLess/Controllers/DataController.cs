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
using Microsoft.AspNetCore.Mvc.Filters;
using PwdLess.Filters;

namespace PwdLess.Controllers
{
    [Route("[controller]/[action]")]
    public class DataController : Controller
    {
        private AuthContext _context;

        public DataController(AuthContext context)
        {
            _context = context;
        }

        [Authorize, TraceExceptions, ValidateModel, SetUserId]
        public async Task<IActionResult> UpdateUserInfo(User user, string userId) // TODO NOW: <- does [SetUserId] even fkn work
        {
            user.UserId = userId;
            _context.Users.Update(user); // [BindRequired] properties of User applied
            await _context.SaveChangesAsync();

            return Ok();
        }

        [Authorize, TraceExceptions]
        public async Task<IActionResult> DeleteUser()
        {
            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            _context.Users.Remove(new User() { UserId = userId });
            _context.UserContacts.RemoveRange(_context.UserContacts.Where(uc => uc.UserId == userId));

            await _context.SaveChangesAsync();

            return Ok();
        }

        [Authorize]
        public async Task<IActionResult> UpdateContact(string contact, ContactOperation operation) // TODO: split into NonceToAddContact(nonce) and RemoveContact(contact)
        {    
            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            switch (operation)
            {
                case ContactOperation.Add: // TODO: validate contact
                    _context.UserContacts.Add(new UserContact() { Contact = contact, UserId = userId });
                    break;
                case ContactOperation.Remove:
                    if (await _context.UserContacts.CountAsync(uc => uc.UserId == userId) <= 1)
                        return BadRequest($"Sorry! Can't Remove last contact."); // TODO: overhaul exception system
                    else
                        _context.UserContacts.Remove(new UserContact() { Contact = contact, UserId = userId });
                    break;
                default:
                    break;
            }

            await _context.SaveChangesAsync();

            _logger.LogDebug($"{operation.ToString()}ed contact: {contact}");
            return Ok($"Success! Contact Added");
        }


    }

    public enum ContactOperation
    {
        Add,
        Remove
    }
}