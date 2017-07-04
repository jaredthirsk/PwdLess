using Jose;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using PwdLess.Models;
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
        bool DoesContactExist(string contact);
        Task<string> AddNonce(string contact, bool isRegistering);

        void ValidateNonce(string nonce);
        string ContactOfNonce(string nonce);
        bool IsNonceIsRegistering(string nonce);
        Task AddUser(User user);
        Task AddUserContact(string userId, string contact);
        string UserIdOfContact(string contact);
        Task<string> AddRefreshToken(string userId);
        Task DeleteNonce(string nonce);

        void ValidateRefreshToken(string refreshToken);
        string RefreshTokenToJwt(string refreshToken);

        Task RevokeRefreshToken(string userId);
    }

    public class AuthService : IAuthService
    {
        private IConfigurationRoot _config;
        private IDistributedCache _cache;
        private AuthContext _context;

        public AuthService(IDistributedCache cache, IConfigurationRoot config, AuthContext context)
        {
            _config = config;
            _cache = cache;
            _context = context;
        }

        public bool DoesContactExist(string contact)
        {
            return _context.UserContacts.Any(uc => uc.Contact == contact);
        }

        public async Task<string> AddNonce(string contact, bool isRegistering)
        {
            string nonce = GenerateNonce();
            _context.Nonces.Add(new Nonce
            {
                Contact = contact,
                IsRegistering = isRegistering,
                Content = nonce,
                Expiry = ToUnixTime(DateTime.Now + new TimeSpan(0, Int32.Parse(_config["PwdLess:Nonce:Expiry"]), 0))
            });

            await _context.SaveChangesAsync();
            return nonce;
        }

        public void ValidateNonce(string nonce)
        {
            Nonce nonceObj = _context.Nonces.FirstOrDefault(n => n.Content == nonce);

            if (nonceObj == null)
                throw new IndexOutOfRangeException();

            if (nonceObj.Expiry > ToUnixTime(DateTime.Now))
                throw new ExpiredException(new DateTime(nonceObj.Expiry).ToString()); // TODO make better
        }

        public string ContactOfNonce(string nonce)
        {
            return _context.Nonces.FirstOrDefault(n => n.Content == nonce).Contact;
        }

        public bool IsNonceIsRegistering(string nonce)
        {
            return _context.Nonces.FirstOrDefault(n => n.Content == nonce).IsRegistering;
        }

        public async Task AddUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task AddUserContact(string userId, string contact)
        {
            _context.UserContacts.Add(new UserContact()
            {
                Contact = contact,
                UserId = userId
            });
            await _context.SaveChangesAsync();
        }

        public string UserIdOfContact(string contact)
        {
            return _context.UserContacts.FirstOrDefault(uc => uc.Contact == contact).UserId;
        }

        public async Task<string> AddRefreshToken(string userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            user.RefreshToken = GenerateRefreshToken();
            user.RefreshTokenExpiry = ToUnixTime(DateTime.Now + new TimeSpan(Int32.Parse(_config["PwdLess:RefreshToken:Expiry"]), 0, 0, 0));
            await _context.SaveChangesAsync();
            return user.RefreshToken;
        }

        public async Task DeleteNonce(string nonce)
        {
            _context.Nonces.Remove(new Nonce() { Content = nonce });
            await _context.SaveChangesAsync();
        }

        public void ValidateRefreshToken(string refreshToken)
        {
            User userObj = _context.Users.FirstOrDefault(u => u.RefreshToken == refreshToken);

            if (userObj.RefreshToken == "")
                throw new IndexOutOfRangeException();

            if (userObj.RefreshTokenExpiry > ToUnixTime(DateTime.Now))
                throw new ExpiredException(new DateTime(userObj.RefreshTokenExpiry).ToString()); // TODO make better
            
        }

        public string RefreshTokenToJwt(string refreshToken)
        {
            var userId = _context.Users.FirstOrDefault(u => u.RefreshToken == refreshToken).UserId;
            return CreateJwt(userId);
        }

        public async Task RevokeRefreshToken(string userId)
        {
            _context.Users.FirstOrDefault(u => u.UserId == userId).RefreshToken = "";
            await _context.SaveChangesAsync();
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

        private string GenerateRefreshToken()
        {
            return GenerateNonce(); // TODO: atually implement
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


    [Serializable]
    public class ExpiredException : Exception
    {
        public ExpiredException() { }
        public ExpiredException(string message) : base(message) { }
        public ExpiredException(string message, Exception inner) : base(message, inner) { }
    }
}
