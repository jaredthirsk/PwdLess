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


        /// <summary>
        /// Creates a TOTP and a token from identifier. 
        /// Stores Token and token in cache.
        /// Returns the TOTP.
        /// </summary>
        /// <param name="identifier">The unique user identifier to geenrate token from.</param>
        /// <returns>The TOTP.</returns>
        public async Task<string> CreateAndStoreTotp(string identifier)
        {
            var token = CreateToken(identifier);
            var totp = GenerateTotp();

            await AddToCache(token, totp);

            return totp;
        }


        /// <summary>
        /// Gets a TOTP's associated token from the cache, if it exists.
        /// Else throws an IndexOutOfRangeException.
        /// </summary>
        /// <param name="totp">The TOTP to get the associated token of.</param>
        /// <returns>The associated Token if it exists.</returns>
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

        /// <summary>
        /// Generates a random TOTP through creating a GUID.
        /// configure max length in configuration.
        /// </summary>
        /// <returns>The generated TOTP.</returns>
        private string GenerateTotp()
        {
            string guid = new String(Guid.NewGuid().ToString()
                                      .Take(Int32.Parse(_config["PwdLess:Totp:Length"]))
                                      .ToArray());
            return guid;
        }

        /// <summary>
        /// Creates a JWT token from configuration.
        /// </summary>
        /// <param name="sub">Subject claim of JWT.</param>
        /// <param name="claims">Other optional claims to incldue in JWT. (currently unused)</param>
        /// <returns>Generated JWT.</returns>
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

        /// <summary>
        /// Adds TOTP/token pair to cache with configurable expiry.
        /// </summary>
        /// <param name="token">Associated token.</param>
        /// <param name="totp">Associated TOTP.</param>
        /// <returns>Nothing.</returns>
        private async Task AddToCache(string token, string totp)
        {
            await _cache.SetAsync(totp,
                Encoding.UTF8.GetBytes(token),
                new DistributedCacheEntryOptions()
                {
                    SlidingExpiration = new TimeSpan(0, Int32.Parse(_config["PwdLess:Totp:Expiry"]), 0)
                });
        }

        /// <summary>
        /// Helper method to convert .NET DateTime type to Unix NumericDate type.
        /// </summary>
        /// <param name="dateTime">The .NET DateTime type.</param>
        /// <returns>A NumericDate.</returns>
        private long ToUnixTime(DateTime dateTime)
        {
            return (int)(dateTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

    }
}
