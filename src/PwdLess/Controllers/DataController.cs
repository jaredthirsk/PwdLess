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
    public class DataController : Controller
    {
        private ILogger _logger;
        private AuthContext _context;

        public DataController(ILogger<DataController> logger, AuthContext context)
        {
            _logger = logger;
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> UpdateUserInfo(User user)
        {
            try
            {
                string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

                user.UserId = userId;

                _context.Users.Update(user);

                // update stuffs here

                await _context.SaveChangesAsync();

                _logger.LogDebug($"User info updated: {user}.");
                return Ok($"Success! User info updated.");
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return BadRequest("Something went wrong.");
            }
        }

        [Authorize]
        public async Task<IActionResult> DeleteUser()
        {
            try
            {
                string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

                _context.Users.Remove(new User() { UserId = userId });
                _context.UserContacts.RemoveRange(_context.UserContacts.Where(uc => uc.UserId == userId));

                await _context.SaveChangesAsync();

                _logger.LogDebug($"User deleted: {userId}");
                return Ok($"Success! User deleted.");
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return BadRequest("Something went wrong.");
            }
        }

        [Authorize]
        public async Task<IActionResult> UpdateContact(string contact, ContactOperation operation)
        {
            try
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
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return BadRequest("Something went wrong.");
            }
        }


    }

    public enum ContactOperation
    {
        Add,
        Remove
    }
}
