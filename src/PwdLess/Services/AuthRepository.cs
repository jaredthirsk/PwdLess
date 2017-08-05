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
    public class AuthRepository
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
        public bool IsContactRemovable(string userId, string contact)
        {
            return _context.UserContacts.Count(uc => uc.UserId == userId) > 1 // not last contact
                && _context.UserContacts.Any(uc => uc.Contact == contact && uc.UserId == userId); // contact actually exists
        }
        public string GetUserIdOfContact(string contact)
        {
            return _context.UserContacts.FirstOrDefault(uc => uc.Contact == contact).UserId;
        }
        public string GetContactOfNonce(string nonce) // yes, also validates nonce
        {
            // Remove all expired nonces?
            //_context.RemoveRange(_context.Nonces.Where(n => n.Expiry < _authHelper.EpochNow));

            Nonce nonceObj = _context.Nonces.FirstOrDefault(n => n.Content == nonce
                                                              && n.Expiry > _authHelper.EpochNow);

            if (nonceObj == null)
                throw new Exception(); // TODO make better

            return _context.Nonces.FirstOrDefault(n => n.Content == nonce).Contact;
        }
        public UserState GetNonceUserState(string nonce)
        {
            return _context.Nonces.FirstOrDefault(n => n.Content == nonce).UserState;
        }

        public string AddNonce(string contact, UserState userState)
        {
            RemoveNonce(contact);
            string nonce = _authHelper.GenerateNonce();
            _context.Nonces.Add(new Nonce
            {
                Contact = contact,
                UserState = userState,
                Content = nonce,
                Expiry = _authHelper.EpochNonceExpiry
            });

            return nonce;
        }
        public void RemoveNonce(string contact)
        {
            _context.Nonces.RemoveRange(_context.Nonces.Where(n => n.Contact == contact));
        }

        public string AddUser(User user)
        {
            user.DateCreated = _authHelper.EpochNow;
            user.UserId = (string.Concat(Guid.NewGuid().ToString().Replace("-", "").Take(12))); // TODO: move to AuthHelperService
            _context.Users.Add(user);
            return user.UserId;
        }

        public void AddUserContact(string userId, string contact)
        {
            _context.UserContacts.Add(new UserContact()
            {
                Contact = contact,
                UserId = userId
            });
        }
        public void RemoveUserContact(string contact, string userId)
        {
            _context.UserContacts.Remove(new UserContact() { Contact = contact, UserId = userId });
        }

        public string AddRefreshToken(string userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            string refreshToken = _authHelper.GenerateRefreshToken();
            _context.UserRefreshTokens.Add(new UserRefreshToken()
            {
                UserId = userId,
                Content = refreshToken,
                Expiry = _authHelper.EpochRefreshTokenExpiry,
            });
            return refreshToken;
        }
        public void RemoveRefreshTokens(string userId)
        {
            _context.UserRefreshTokens.RemoveRange(_context.UserRefreshTokens.Where(urf => urf.UserId == userId));
        }

        public string RefreshTokenToAccessToken(string refreshToken) // yes, also validates refresh token
        {
            // Remove all expired refresh tokens?
            //_context.RemoveRange(_context.UserRefreshTokens.Where(urf => urf.Expiry < _authHelper.EpochNow));

            User userObj = _context.Users.FirstOrDefault(u => u.UserRefreshTokens.Any(urf => urf.Content == refreshToken 
                                                                                          && urf.Expiry > _authHelper.EpochNow));

            if (userObj == null || refreshToken == "")
                throw new Exception(); // TODO make better
            
            return _authHelper.GenerateAccessToken(userObj, _context.UserContacts.Where(uc => uc.UserId == userObj.UserId).Select(uc => uc.Contact).ToList());
        }

        public async Task SaveDbChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

    }
    
}
