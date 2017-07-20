using Jose;
using Microsoft.Extensions.Caching.Distributed;
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
    /// <summary>
    /// Handles creating nonces and tokens.
    /// Also handles storing them in a cache and retrieving them if present.
    /// </summary>
    public interface IAuthRepository
    {
        bool DoesContactExist(string contact);
        Task<string> AddNonce(string contact, UserState userState);

        void ValidateNonce(string nonce);
        string ContactOfNonce(string nonce);
        UserState GetNonceUserState(string nonce);
        Task AddUser(User user);
        Task AddUserContact(string userId, string contact);
        string UserIdOfContact(string contact);
        Task<string> AddRefreshToken(string userId);
        Task DeleteNonce(string contact);

        void ValidateRefreshToken(string refreshToken);
        string RefreshTokenToAccessToken(string refreshToken);

        Task RevokeRefreshToken(string userId);
    }

    public class AuthRepository : IAuthRepository
    {
        private AuthContext _context;
        private IAuthHelperService _authHelper;

        public AuthRepository(AuthContext context, IAuthHelperService authHelper)
        {
            _context = context;
            _authHelper = authHelper;
        }


        public bool DoesContactExist(string contact)
        {
            return _context.UserContacts.Any(uc => uc.Contact == contact);
        }
         
        public string UserIdOfContact(string contact)
        {
            return _context.UserContacts.FirstOrDefault(uc => uc.Contact == contact).UserId;
        }


        public async Task<string> AddNonce(string contact, UserState userState)
        {
            await DeleteNonce(contact);
            string nonce = _authHelper.GenerateNonce();
            _context.Nonces.Add(new Nonce
            {
                Contact = contact,
                UserState = userState,
                Content = nonce,
                Expiry = _authHelper.EpochNonceExpiry
            });

            await _context.SaveChangesAsync();
            return nonce;
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
        
        public async Task<string> AddRefreshToken(string userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            user.RefreshToken = _authHelper.GenerateRefreshToken();
            user.RefreshTokenExpiry = _authHelper.EpochRefreshTokenExpiry;
            await _context.SaveChangesAsync();
            return user.RefreshToken;
        }


        public void ValidateNonce(string nonce)
        {
            Nonce nonceObj = _context.Nonces.OrderBy(n => n.Expiry)
                                            .FirstOrDefault(n => n.Content == nonce);

            if (nonceObj == null)
                throw new IndexOutOfRangeException();

            if (nonceObj.Expiry < _authHelper.EpochNow)
                throw new Exception(nonceObj.Expiry.ToString() + "     " + _authHelper.EpochNow.ToString()); // TODO make better
        }

        public void ValidateRefreshToken(string refreshToken)
        {
            User userObj = _context.Users.FirstOrDefault(u => u.RefreshToken == refreshToken);

            if (userObj.RefreshToken == "")
                throw new IndexOutOfRangeException();

            if (userObj.RefreshTokenExpiry < _authHelper.EpochNow)
                throw new Exception(new DateTime(userObj.RefreshTokenExpiry).ToString()); // TODO make better

        }


        public string ContactOfNonce(string nonce)
        {
            return _context.Nonces.FirstOrDefault(n => n.Content == nonce).Contact;
        }

        public UserState GetNonceUserState(string nonce)
        {
            return _context.Nonces.FirstOrDefault(n => n.Content == nonce).UserState;
        }

        public async Task DeleteNonce(string contact)
        {
            _context.Nonces.RemoveRange(_context.Nonces.Where(n => n.Contact == contact));
            await _context.SaveChangesAsync();
        }



        public string RefreshTokenToAccessToken(string refreshToken)
        {
            var user = _context.Users.FirstOrDefault(u => u.RefreshToken == refreshToken);
            return _authHelper.GenerateAccessToken(user, _context.UserContacts.Where(uc => uc.UserId == user.UserId).Select(uc => uc.Contact).ToList());
        }

        public async Task RevokeRefreshToken(string userId)
        {
            _context.Users.FirstOrDefault(u => u.UserId == userId).RefreshToken = "";
            await _context.SaveChangesAsync();
        }
    }
    
}
