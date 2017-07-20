using Jose;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PwdLess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PwdLess.Services
{

    public interface IAuthHelperService
    {
        string GenerateAccessToken(User user, List<string> userContacts, Dictionary<string, object> claims = null);
        string GenerateNonce();
        string GenerateRefreshToken();
        long EpochNow { get; }
        long EpochNonceExpiry { get; }
        long EpochRefreshTokenExpiry { get; }
    }

    public class AuthHelperService : IAuthHelperService
    {
        private IConfigurationRoot _config;

        public AuthHelperService(IConfigurationRoot config)
        {
            _config = config;
        }


        public long EpochNow
        {
            get
            {
                return ToEpochTime(DateTime.Now);
            }
        }

        public long EpochNonceExpiry
        {
            get
            {
                return ToEpochTime(DateTime.Now.AddSeconds(Int32.Parse(_config["PwdLess:Nonce:Expiry"])));
            }
        }

        public long EpochRefreshTokenExpiry
        {
            get
            {
                return ToEpochTime(DateTime.Now.AddSeconds(Int32.Parse(_config["PwdLess:RefreshToken:Expiry"])));
            }
        }

        public string GenerateAccessToken(User user, List<string> userContacts, Dictionary<string, object> claims = null)
        {
            var payload = new Dictionary<string, object>
            {
                { "sub", user.UserId },
                { "iss", _config["PwdLess:AccessToken:Issuer"]},
                { "iat", EpochNow },
                { "exp", ToEpochTime(DateTime.Now.AddSeconds(Int32.Parse(_config["PwdLess:AccessToken:Expiry"]))) },
                { "aud", _config["PwdLess:AccessToken:Audience"] },
                { "userInfo", JsonConvert.SerializeObject(new {
                    user.FavouriteColour
                }) },
                { "userContacts",  userContacts }
            };

            if (claims != null)
            {
                foreach (var kvPair in claims)
                    payload.Add(kvPair.Key, kvPair.Value);
            }

            string token = JWT.Encode(payload,
                Encoding.UTF8.GetBytes(_config["PwdLess:AccessToken:SecretKey"]),
                JwsAlgorithm.HS256);

            return token;
        }


        public string GenerateNonce()
        {
            return GenerateRandomString(Int32.Parse(_config["PwdLess:Nonce:Length"]));
        }

        public string GenerateRefreshToken()
        {
            return GenerateRandomString(Int32.Parse(_config["PwdLess:RefreshToken:Length"]));
        }
        

        private long ToEpochTime(DateTime dateTime)
        {
            return (int)(dateTime
                .ToUniversalTime()
                .Subtract(new DateTime(1970, 1, 1)))
                .TotalSeconds;
        }

        private string GenerateRandomString(int maxLength)
        {
            // populate a byte[] with crypto RNG bytes
            Byte[] cRBytes = new Byte[maxLength];
            RandomNumberGenerator.Create().GetBytes(cRBytes);

            // SHA1 the bytes to normalize across platfroms
            byte[] sha1 = SHA1.Create().ComputeHash(cRBytes);

            // convert SHA1 bytes to string via HEX
            string cRString = BitConverter.ToString(sha1)
                .Replace("-", "")
                .Substring(0, maxLength);

            return cRString;
        }

    }
}
