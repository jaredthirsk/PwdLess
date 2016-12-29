using Jose;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using PwdLess.Auth.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PwdLess.Auth.Services
{
    public interface IAuthService
    {
        Task FullLogin(string email);
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

        public async Task FullLogin(string email)
        {
            var token = CreateToken(email);
            var totp = CreateTotp();

            await _cache.SetAsync(totp, Encoding.UTF8.GetBytes(token));

            await SendTokenInUrl(totp, email);
        }

        private string CreateTotp()
        {
            var guid = Guid.NewGuid().ToString();
            // shorten guid according to config here
            return guid;
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

        private async Task SendTokenInUrl(string token, string email)
        {
            // generate echo url & add to tmeplate
            var url = _config["PwdLess:ClientJwtUrl"].Replace("{{token}}", token);

            await _sender.SendEmailAsync(email, url);
        }

        private long ToUnixTime(DateTime dateTime)
        {
            return (int)(dateTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
