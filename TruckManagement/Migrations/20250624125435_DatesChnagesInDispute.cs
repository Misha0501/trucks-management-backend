using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class DatesChnagesInDispute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OpenedAt",
                table: "PartRideDisputes",
                newName: "CreatedAtUtc");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAtUtc",
                table: "PartRideDisputes",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                table: "PartRideDisputes");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "PartRideDisputes",
                newName: "OpenedAt");
        }
    }
}
