using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using PwdLess.Auth.Data;

namespace PwdLess.Auth.Migrations
{
    [DbContext(typeof(UsersDbContext))]
    [Migration("20161229081350_snd")]
    partial class snd
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.1");

            modelBuilder.Entity("PwdLess.Auth.Models.User", b =>
                {
                    b.Property<string>("Email");

                    b.HasKey("Email");

                    b.ToTable("Users");
                });
        }
    }
}
