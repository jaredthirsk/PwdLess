using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PwdLess.Migrations
{
    public partial class userStates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRegistering",
                table: "Nonces");

            migrationBuilder.AddColumn<int>(
                name: "UserState",
                table: "Nonces",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserState",
                table: "Nonces");

            migrationBuilder.AddColumn<bool>(
                name: "IsRegistering",
                table: "Nonces",
                nullable: false,
                defaultValue: false);
        }
    }
}
