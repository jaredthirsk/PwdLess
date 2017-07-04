using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PwdLess.Migrations
{
    public partial class test1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Nonces",
                columns: table => new
                {
                    Contact = table.Column<string>(nullable: false),
                    Content = table.Column<string>(nullable: false),
                    IsRegistering = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nonces", x => new { x.Contact, x.Content });
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    RefreshToken = table.Column<string>(nullable: true),
                    RefreshTokenExpiry = table.Column<long>(nullable: false),
                    RefreshTokenRevoked = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "UserContacts",
                columns: table => new
                {
                    Contact = table.Column<string>(nullable: false),
                    UserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserContacts", x => x.Contact);
                    table.ForeignKey(
                        name: "FK_UserContacts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserContacts_UserId",
                table: "UserContacts",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Nonces");

            migrationBuilder.DropTable(
                name: "UserContacts");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
