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
            var token = Encoding.UTF8.GetString(await _cache.GetAsync(totp));
            return token;
        }

        public async Task CreateAndSendTotp(string email)
        {
            var token = CreateToken(email);
            var totp = CreateTotp();

            await _cache.SetAsync(totp, Encoding.UTF8.GetBytes(token));

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

        private string CreateTotp()
        {
            var guid = Guid.NewGuid().ToString();
            // shorten guid according to config here
            return guid;
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
