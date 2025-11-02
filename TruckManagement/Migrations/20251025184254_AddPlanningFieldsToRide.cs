using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanningFieldsToRide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Rides",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "ClientId",
                table: "Rides",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Rides",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreationMethod",
                table: "Rides",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Rides",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedDate",
                table: "Rides",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PlannedHours",
                table: "Rides",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RouteFromName",
                table: "Rides",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteToName",
                table: "Rides",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rides_ClientId",
                table: "Rides",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rides_Clients_ClientId",
                table: "Rides",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rides_Clients_ClientId",
                table: "Rides");

            migrationBuilder.DropIndex(
                name: "IX_Rides_ClientId",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "CreationMethod",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "PlannedDate",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "PlannedHours",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "RouteFromName",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "RouteToName",
                table: "Rides");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Rides",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
