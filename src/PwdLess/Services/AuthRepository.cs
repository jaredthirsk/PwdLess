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
         
        public string UserIdOfContact(string contact)
        {
            return _context.UserContacts.FirstOrDefault(uc => uc.Contact == contact).UserId;
        }


        public string AddNonce(string contact, UserState userState)
        {
            DeleteNonce(contact);
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

        public void AddUser(User user)
        {
            _context.Users.Add(user);
        }

        public void AddUserContact(string userId, string contact)
        {
            _context.UserContacts.Add(new UserContact()
            {
                Contact = contact,
                UserId = userId
            });
        }
        
        public string AddRefreshToken(string userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            user.RefreshToken = _authHelper.GenerateRefreshToken();
            user.RefreshTokenExpiry = _authHelper.EpochRefreshTokenExpiry;
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

        public void DeleteNonce(string contact)
        {
            _context.Nonces.RemoveRange(_context.Nonces.Where(n => n.Contact == contact));
        }



        public string RefreshTokenToAccessToken(string refreshToken)
        {
            var user = _context.Users.FirstOrDefault(u => u.RefreshToken == refreshToken);
            return _authHelper.GenerateAccessToken(user, _context.UserContacts.Where(uc => uc.UserId == user.UserId).Select(uc => uc.Contact).ToList());
        }

        public void RevokeRefreshToken(string userId)
        {
            _context.Users.FirstOrDefault(u => u.UserId == userId).RefreshToken = "";
        }



        public void RemoveUserContact(string contact, string userId)
        {
            _context.UserContacts.Remove(new UserContact() { Contact = contact, UserId = userId });
        }

        public bool IsLastUserContact(string userId)
        {
            return  _context.UserContacts.Count(uc => uc.UserId == userId) <= 1;
        }



        public async Task SaveDbChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

    }
    
}
