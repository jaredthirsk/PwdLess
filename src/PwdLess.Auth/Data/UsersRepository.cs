using Microsoft.EntityFrameworkCore;
using PwdLess.Auth.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PwdLess.Auth.Data
{
    public interface IUsersRepository
    {
        bool DoesUserExist(string email);
        Task AddUser(string email);
        //Task RemoveUser(string email);
        //Task<Dictionary<string, object>> GetUserClaims(string email);
        //Task SetUserClaims(string email, Dictionary<string, object> claims);
    }

    public class InMemoryUsersRepository : IUsersRepository
    {
        private List<User> _users = new List<User>
        {
            new User() { Email = "qudware@outlook.com" },
            new User() { Email = "biarity@outlook.com" }
        };

        public bool DoesUserExist(string email)
        {
            return _users.Exists(u => u.Email == email);
        }

        public Task AddUser(string email)
        {
            _users.Add(new User() { Email = email });
            return null;
        }

        /*
        public Task RemoveUser(string email)
        {
            _users.RemoveAll(u => u.Email == email);
            return null;
        }

        public Task<Dictionary<string, object>> GetUserClaims(string email)
        {
            throw new NotImplementedException();
        }

        public Task SetUserClaims(string email, Dictionary<string, object> claims)
        {
            throw new NotImplementedException();
        }
        */
    }

    public class NpgsqlUsersRepository : IUsersRepository
    {
        private UsersDbContext _context; 
        public NpgsqlUsersRepository(UsersDbContext usersContext)
        {
            _context = usersContext;
        }

        public async Task AddUser(string email)
        {
            _context.Users.Add(new User() { Email = email });
            await _context.SaveChangesAsync();
        }

        public bool DoesUserExist(string email)
        {
            return _context.Users.Any(u => u.Email == email);
        }

        /*
        public async Task RemoveUser(string email)
        {
            _context.Users.Remove(new User() { Email = email });
            await _context.SaveChangesAsync();
        }

        public Task<Dictionary<string, object>> GetUserClaims(string email)
        {
            throw new NotImplementedException();
        }

        public Task SetUserClaims(string email, Dictionary<string, object> claims)
        {
            throw new NotImplementedException();
        }
        */
    }
}
