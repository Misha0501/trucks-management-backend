using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramNotificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TelegramChatId",
                table: "Drivers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TelegramNotificationsEnabled",
                table: "Drivers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TelegramRegisteredAt",
                table: "Drivers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelegramRegistrationToken",
                table: "Drivers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TelegramTokenExpiresAt",
                table: "Drivers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TelegramChatId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "TelegramNotificationsEnabled",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "TelegramRegisteredAt",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "TelegramRegistrationToken",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "TelegramTokenExpiresAt",
                table: "Drivers");
        }
    }
}
