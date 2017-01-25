using Jose;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PwdLess.Services
{
    /// <summary>
    /// Handles creating nonces and tokens.
    /// Also handles storing them in a cache and retrieving them if present.
    /// </summary>
    public interface IAuthService
    {
        Task<string> CreateAndStoreNonce(string identifier, string type = "default");
        Task<string> GetTokenFromNonce(string nonce);
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
        
        public async Task<string> CreateAndStoreNonce(string identifier, string type)
        {
            var token = CreateJwt(identifier, new Dictionary<string, object>
            {
                { "type", type }
            });
            var nonce = GenerateNonce();

            await AddToCache(token, nonce);

            return nonce;
        }

        public async Task<string> GetTokenFromNonce(string nonce)
        {
            byte[] token = await _cache.GetAsync(nonce);

            if (token != null)
            {
                await _cache.RemoveAsync(nonce);

                return Encoding.UTF8.GetString(token);
            }
            else
            {
                throw new IndexOutOfRangeException("Nonce doesn't exist.");
            }
        }

        private string GenerateNonce()
        {
            // populate a byte[] with crypto RNG bytes
            int maxLength = Int32.Parse(_config["PwdLess:Nonce:Length"]);
            Byte[] cRBytes = new Byte[maxLength];
            RandomNumberGenerator.Create().GetBytes(cRBytes);

            // SHA1 the bytes to normalize across platfroms
            byte[] sha1 = SHA1.Create().ComputeHash(cRBytes);

            // convert bytes to string via HEX
            string cRString = BitConverter.ToString(cRBytes)
                .Replace("-", "")
                .Substring(0, maxLength);
            
            return cRString;
        }
        
        private string CreateJwt(string sub, Dictionary<string, object> claims = null)
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
        
        private async Task AddToCache(string token, string nonce)
        {
            await _cache.SetAsync(nonce,
                Encoding.UTF8.GetBytes(token),
                new DistributedCacheEntryOptions()
                {
                    SlidingExpiration = new TimeSpan(0, Int32.Parse(_config["PwdLess:Nonce:Expiry"]), 0)
                });
        }
        
        private long ToUnixTime(DateTime dateTime)
        {
            return (int)(dateTime
                .ToUniversalTime()
                .Subtract(new DateTime(1970, 1, 1)))
                .TotalSeconds;
        }

    }
}
