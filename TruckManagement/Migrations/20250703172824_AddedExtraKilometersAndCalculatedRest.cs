using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddedExtraKilometersAndCalculatedRest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Kilometers",
                table: "PartRides",
                newName: "TotalKilometers");

            migrationBuilder.AddColumn<double>(
                name: "ExtraKilometers",
                table: "PartRides",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "RestCalculated",
                table: "PartRides",
                type: "interval",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtraKilometers",
                table: "PartRides");

            migrationBuilder.DropColumn(
                name: "RestCalculated",
                table: "PartRides");

            migrationBuilder.RenameColumn(
                name: "TotalKilometers",
                table: "PartRides",
                newName: "Kilometers");
        }
    }
}
