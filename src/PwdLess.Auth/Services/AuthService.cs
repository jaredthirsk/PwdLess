using Jose;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PwdLess.Auth.Services
{
    public interface IAuthService
    {
        Task CreateAndSendTotp(string email);
        Task<string> GetTokenFromTotp(string totp);
    }

    public class AuthService : IAuthService
    {
        private IConfigurationRoot _config;
        private IDistributedCache _cache;
        private ISenderService _sender;

        public AuthService(ISenderService senderService, IDistributedCache cache, IConfigurationRoot config)
        {
            _config = config;
            _cache = cache;
            _sender = senderService;
        }

        public async Task<string> GetTokenFromTotp(string totp)
        {
            byte[] token = await _cache.GetAsync(totp);

            if (token != null)
            {
                await _cache.RemoveAsync(totp);

                return Encoding.UTF8.GetString(token);
            }
            else
            {
                throw new IndexOutOfRangeException("Totp doesn't exist.");
            }
        }

        public async Task CreateAndSendTotp(string email)
        {
            var token = CreateToken(email);
            var totp = GenerateTotp();

            await AddToCache(token, totp);

            await SendTotpInUrl(totp, email);
        }

        private string CreateToken(string sub, Dictionary<string, object> claims = null)
        {
            var payload = new Dictionary<string, object>
            {
                { "sub", sub },
                { "iss", _config["PwdLess:Jwt:Issuer"]},
                { "iat", ToUnixTime(DateTime.Now) },
                { "exp", _config["PwdLess:Jwt:Expiry"] == "" ? ToUnixTime(DateTime.Now.AddDays(30)) : Int32.Parse(_config["PwdLess:Jwt:Expiry"]) },
                { "aud", _config["PwdLess:Jwt:Audience"] }
            };

            if (claims != null)
            {
                foreach (var kvPair in claims)
                    payload.Add(kvPair.Key, kvPair.Value);
            }

            string token = JWT.Encode(payload,
                Encoding.UTF8.GetBytes(_config["PwdLess:Jwt:SecretKey"]),
                JwsAlgorithm.HS256);

            return token;
        }

        private string GenerateTotp()
        {
            string guid = new String(Guid.NewGuid().ToString()
                                      .Take(Int32.Parse(_config["PwdLess:Totp:Length"]))
                                      .ToArray());
            return guid;
        }

        private async Task AddToCache(string token, string totp)
        {
            await _cache.SetAsync(totp,
                Encoding.UTF8.GetBytes(token),
                new DistributedCacheEntryOptions()
                {
                    SlidingExpiration = new TimeSpan(0, Int32.Parse(_config["PwdLess:Totp:Expiry"]), 0)
                });
        }

        private async Task SendTotpInUrl(string totp, string email)
        {
            // generate echo url & add to tmeplate
            var url = _config["PwdLess:ClientJwtUrl"].Replace("{{totp}}", totp);

            await _sender.SendEmailAsync(email, url);
        }

        private long ToUnixTime(DateTime dateTime)
        {
            return (int)(dateTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
