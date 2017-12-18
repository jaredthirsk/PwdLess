using Microsoft.AspNetCore.Http;
using PwdLess.Data;
using System;
using System.Threading.Tasks;

namespace PwdLess.Services
{
    public class EventsService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContext;

        public EventsService(ApplicationDbContext dbContext,
            IHttpContextAccessor httpContext)
        {
            _dbContext = dbContext;
            _httpContext = httpContext;
        }

        public async Task<AuthEvent> AddEvent(AuthEventType eventType, string subject, ApplicationUser user = null)
        {
            var authEvent = new AuthEvent()
            {
                ApplicationUserId = user?.Id,
                ClientIPAddress = _httpContext.HttpContext.Connection.RemoteIpAddress.ToString(),
                ClientUserAgent = _httpContext.HttpContext.Request.Headers["User-Agent"].ToString(),
                OccurrenceTime = DateTimeOffset.UtcNow,
                Type = eventType,
                Subject = subject
            };

            await _dbContext.AuthEvents.AddAsync(authEvent);

            return authEvent;
        }
    }
}
