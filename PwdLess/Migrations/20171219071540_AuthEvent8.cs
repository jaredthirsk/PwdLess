using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace PwdLess.Migrations
{
    public partial class AuthEvent8 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthEvents_AspNetUsers_ApplicationUserId",
                table: "AuthEvents");

            migrationBuilder.RenameColumn(
                name: "ApplicationUserId",
                table: "AuthEvents",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AuthEvents_ApplicationUserId",
                table: "AuthEvents",
                newName: "IX_AuthEvents_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthEvents_AspNetUsers_UserId",
                table: "AuthEvents",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthEvents_AspNetUsers_UserId",
                table: "AuthEvents");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "AuthEvents",
                newName: "ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_AuthEvents_UserId",
                table: "AuthEvents",
                newName: "IX_AuthEvents_ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthEvents_AspNetUsers_ApplicationUserId",
                table: "AuthEvents",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
