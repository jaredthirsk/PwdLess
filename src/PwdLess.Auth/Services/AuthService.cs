using Jose;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PwdLess.Auth.Services
{
    /// <summary>
    /// Handles creating TOTPs and tokens.
    /// Also handles storing them in a cache and retrieving them if present.
    /// </summary>
    public interface IAuthService
    {
        Task<string> CreateAndStoreTotp(string identifier);
        Task<string> GetTokenFromTotp(string totp);
    }


    public class AuthService : IAuthService
    {
        private IConfigurationRoot _config;
        private IDistributedCache _cache;

        public AuthService(IDistributedCache cache, IConfigurationRoot config)
        {
            _config = config;
            _cache = cache;
        }
        
        public async Task<string> CreateAndStoreTotp(string identifier)
        {
            var token = CreateToken(identifier);
            var totp = GenerateTotp();

            await AddToCache(token, totp);

            return totp;
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
        
        private string GenerateTotp()
        {
            int maxLength = Int32.Parse(_config["PwdLess:Totp:Length"]);

            Byte[] cRBytes = new Byte[maxLength];
            RandomNumberGenerator cRNG = RandomNumberGenerator.Create();
            cRNG.GetBytes(cRBytes);

            string cRString = Convert.ToBase64String(cRBytes)
                .Replace("+", "")
                .Replace("=", "")
                .Replace("/", "")
                .Substring(0, maxLength);

            //string guid = new String(Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            //                         .Replace("=", "")
            //                         .Replace("+", "")
            //                         .Take(Int32.Parse(_config["PwdLess:Totp:Length"]))
            //                         .ToArray());
            return cRString;
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
        
        private async Task AddToCache(string token, string totp)
        {
            await _cache.SetAsync(totp,
                Encoding.UTF8.GetBytes(token),
                new DistributedCacheEntryOptions()
                {
                    SlidingExpiration = new TimeSpan(0, Int32.Parse(_config["PwdLess:Totp:Expiry"]), 0)
                });
        }
        
        private long ToUnixTime(DateTime dateTime)
        {
            return (int)(dateTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

    }
}
