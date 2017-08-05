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
    [Route("[controller]")] // TODO: make at /api or /auth/api
    public class UsersController : Controller /* TODO + THIS IS A REST API */
    {
        private AuthContext _context;

        public UsersController(AuthContext context)
        {
            _context = context;
        }

/*
        #region GET

        [HttpGet(), Authorize(Policy = "admin"), HandleExceptions]
        public IActionResult GetAll(string filters = "", string sorts = "new", string search = "*", int page = 1, int contentPerPage = 10) // TODO: actual JSON-based filtering (ie. get me sutff with genre = this)
        {
            // Initiate query
            var users = from u in _context.Users
                        select u;

            // Filter
            //foreach (var filter in filters.Split(','))
            //    switch (filter.ToLower())
            //    {
            //        case "reading":
            //            users = users.Where(s => s.Readings.Any(r => r.UserId == currentUser));
            //            break;
            //        case "authoring":
            //            users = users.Where(s => s.AuthorUserId == currentUser);
            //            break;
            //        case "complete":
            //            users = users.Where(s => s.IsComplete);
            //            break;
            //        case "since_year": // TODO
            //        case "since_month":
            //        case "since_week":
            //        case "since_day":
            //        case "none":
            //        case "":
            //        default:
            //            users = users.Where(s => (s.AuthorUserId == currentUser || !s.IsPrivate));
            //            break;
            //    }

            // Search
            search = search.ToLower();
            if (search != "*")
                users = users.Where(u => u.UserContacts.Any(uc => uc.Contact.ToLower().Contains(search))
                                        || Newtonsoft.Json.JsonConvert.SerializeObject(u).ToLower().Contains(search));

            // Sort
            foreach (var sort in sorts.Split(','))
                switch (sort.ToLower())
                {
                        
                    case "new":
                        users = users.OrderByDescending(s => s.DateCreated);
                        break;
                    case "old":
                        users = users.OrderByDescending(s => s.DateCreated);
                        break;
                    case "none":
                    case "":
                    default:
                        break;
                }

            // Paginate
            page = page - 1;
            users = users.Skip(page * contentPerPage).Take(contentPerPage);

            // Select only specific fields
            var result = users.Select(u => new
            {
                UserId = u.UserId,
                DateCreated = u.DateCreated,
                UserContacts = u.UserContacts.Select(uc => uc.Contact)
            });

            return Ok(result);
        }


        [HttpGet("{userId}"), Authorize(Policy = "admin"), HandleExceptions] // Get specific story if not private or if author
        public IActionResult GetOne(string userId)
        {
            var user = _context.Users.Where(u => (s.AuthorUserId == currentUser || !s.IsPrivate)
                                                    && s.StoryId == storyId)
                                        .Select(s => new
                                        {
                                            StoryId = s.StoryId,
                                            AuthorUserId = s.AuthorUserId,
                                            Price = s.Price,
                                            IsComplete = s.IsComplete,
                                            IsPrivate = s.IsPrivate,
                                            SourceCode = s.SourceCode,
                                            CoverImage = s.CoverImage,
                                            Name = s.Name,
                                            Description = s.Description,
                                            Genre = s.Genre,
                                            DiscussionUrl = s.DiscussionUrl,
                                            Reads = s.Readings.Count,
                                            Stars = s.Readings.Count(r => r.DidStar),
                                            CreatedOn = s.CreatedOn,
                                            LastUpdated = s.LastUpdated
                                        }).FirstOrDefault();
            if (user == null)
                return NotFound();
                
            return Ok(user);
        }

        #endregion
*/
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