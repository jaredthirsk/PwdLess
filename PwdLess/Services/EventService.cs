using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PwdLess.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PwdLess.Services
{
    public class EventsService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContext;

        public EventsService(ApplicationDbContext dbContext,
            IConfiguration configuration,
            IHttpContextAccessor httpContext)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _httpContext = httpContext;
        }

        public async Task<AuthEvent> AddEvent(AuthEventType eventType, string subject, ApplicationUser user = null)
        {
            var maxEventCount = Int32.Parse(_configuration["PwdLess:Events:MaxStored"]);

            if (maxEventCount <= 0)
                return null;

            var authEvent = new AuthEvent()
            {
                UserId = user?.Id,
                ClientIPAddress = _httpContext.HttpContext.Connection.RemoteIpAddress.ToString(),
                ClientUserAgent = _httpContext.HttpContext.Request.Headers["User-Agent"].ToString(),
                OccurrenceTime = DateTimeOffset.UtcNow,
                Type = eventType,
                Subject = subject
            };

            // Remove oldest event if surpassing number of max events to store
            var eventCount = _dbContext.AuthEvents.Count(e => e.UserId == user.Id);
            if (eventCount == maxEventCount)
            {
                var oldestEvent = _dbContext.AuthEvents
                    .Aggregate((agg, next) => next.OccurrenceTime < agg.OccurrenceTime ? next : agg);
                _dbContext.Remove(oldestEvent);
            }

            await _dbContext.AuthEvents.AddAsync(authEvent);
            await _dbContext.SaveChangesAsync();

            return authEvent;
        }

        public IList<AuthEvent> GetEvents(ApplicationUser user)
        {
            var maxReturned = Int32.Parse(_configuration["PwdLess:Events:MaxReturned"]);

            if (maxReturned <= 0)
                return null;

            return _dbContext.AuthEvents
                .Where(e => e.UserId == user.Id)
                .Take(maxReturned).ToList();
        }
    }
}
