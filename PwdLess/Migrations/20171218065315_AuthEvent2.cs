using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace PwdLess.Migrations
{
    public partial class AuthEvent2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthEvents",
                columns: table => new
                {
                    AuthEventId = table.Column<string>(nullable: false),
                    ApplicationUserId = table.Column<string>(nullable: true),
                    ClientIPAddress = table.Column<string>(nullable: true),
                    ClientUserAgent = table.Column<string>(nullable: true),
                    OccurrenceTime = table.Column<DateTimeOffset>(nullable: false),
                    Subject = table.Column<string>(nullable: true),
                    Type = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthEvents", x => x.AuthEventId);
                    table.ForeignKey(
                        name: "FK_AuthEvents_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthEvents_ApplicationUserId",
                table: "AuthEvents",
                column: "ApplicationUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthEvents");
        }
    }
}
