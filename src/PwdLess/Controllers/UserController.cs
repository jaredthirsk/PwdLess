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
    public class UserController : Controller
    {
        private AuthContext _context;

        public UserController(AuthContext context)
        {
            _context = context;
        }
        
        [Authorize, HandleExceptions, ValidateModel, SetUserId]
        public async Task<IActionResult> UpdateUserInfo(User user, string userId)
        {
            user.UserId = userId;
            _context.Users.Update(user); // based on [BindRequired] & [BindNever] properties of User applied
            await _context.SaveChangesAsync();

            return Ok();
        }

        [Authorize, SetUserId, HandleExceptions]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            _context.Users.Remove(new User() { UserId = userId });
            _context.UserContacts.RemoveRange(_context.UserContacts.Where(uc => uc.UserId == userId));

            await _context.SaveChangesAsync();

            return Ok();
        }

    }
}