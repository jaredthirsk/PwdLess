using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace PwdLess.Models
{
    public class AuthContext : DbContext
    {
        public AuthContext(DbContextOptions<AuthContext> options) :  base(options){ }

        public DbSet<User> Users { get; set; }
        public DbSet<UserContact> UserContacts { get; set; }
        public DbSet<Nonce> Nonces { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Nonce>()
                .HasKey(n => new { n.Contact, n.Content });
        }
    }
}
