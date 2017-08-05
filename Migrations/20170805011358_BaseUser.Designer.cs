using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using PwdLess.Models;

namespace PwdLess.Migrations
{
    [DbContext(typeof(AuthContext))]
    [Migration("20170805011358_BaseUser")]
    partial class BaseUser
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.2")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("PwdLess.Models.Nonce", b =>
                {
                    b.Property<string>("Contact");

                    b.Property<string>("Content");

                    b.Property<long>("Expiry");

                    b.Property<int>("UserState");

                    b.HasKey("Contact", "Content");

                    b.ToTable("Nonces");
                });

            modelBuilder.Entity("PwdLess.Models.User", b =>
                {
                    b.Property<string>("UserId")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("DateCreated");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasMaxLength(15);

                    b.Property<string>("FavouriteColour")
                        .IsRequired();

                    b.HasKey("UserId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("PwdLess.Models.UserContact", b =>
                {
                    b.Property<string>("Contact")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("UserId");

                    b.HasKey("Contact");

                    b.HasIndex("UserId");

                    b.ToTable("UserContacts");
                });

            modelBuilder.Entity("PwdLess.Models.UserRefreshToken", b =>
                {
                    b.Property<string>("Content")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("Expiry");

                    b.Property<string>("UserId");

                    b.HasKey("Content");

                    b.HasIndex("UserId");

                    b.ToTable("UserRefreshTokens");
                });

            modelBuilder.Entity("PwdLess.Models.UserContact", b =>
                {
                    b.HasOne("PwdLess.Models.User", "User")
                        .WithMany("UserContacts")
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("PwdLess.Models.UserRefreshToken", b =>
                {
                    b.HasOne("PwdLess.Models.User", "User")
                        .WithMany("UserRefreshTokens")
                        .HasForeignKey("UserId");
                });
        }
    }
}
