using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddHoursOptionAndCodeTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ConsignmentFee",
                table: "PartRides",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "CorrectionTotalHours",
                table: "PartRides",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ExtraKilometers",
                table: "PartRides",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<Guid>(
                name: "HoursCodeId",
                table: "PartRides",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HoursOptionId",
                table: "PartRides",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "KilometerReimbursement",
                table: "PartRides",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "NightAllowance",
                table: "PartRides",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SaturdayHours",
                table: "PartRides",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "StandOver",
                table: "PartRides",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SundayHolidayHours",
                table: "PartRides",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TaxFreeCompensation",
                table: "PartRides",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TotalHours",
                table: "PartRides",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "VariousCompensation",
                table: "PartRides",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "HoursCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoursCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HoursOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoursOptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartRides_HoursCodeId",
                table: "PartRides",
                column: "HoursCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PartRides_HoursOptionId",
                table: "PartRides",
                column: "HoursOptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_HoursCodes_HoursCodeId",
                table: "PartRides",
                column: "HoursCodeId",
                principalTable: "HoursCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_HoursOptions_HoursOptionId",
                table: "PartRides",
                column: "HoursOptionId",
                principalTable: "HoursOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_HoursCodes_HoursCodeId",
                table: "PartRides");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_HoursOptions_HoursOptionId",
                table: "PartRides");

            migrationBuilder.DropTable(
                name: "HoursCodes");

            migrationBuilder.DropTable(
                name: "HoursOptions");

            migrationBuilder.DropIndex(
                name: "IX_PartRides_HoursCodeId",
                table: "PartRides");

            migrationBuilder.DropIndex(
                name: "IX_PartRides_HoursOptionId",
                table: "PartRides");

            migrationBuilder.DropColumn(
                name: "ConsignmentFee",
                table: "PartRides");

            migrationBuilder.DropColumn(
                name: "CorrectionTotalHours",
                table: "PartRides");

            migrationBuilder.DropColumn(
                name: "ExtraKilometers",
                table: "PartRides");

            migrationBuilder.DropColumn(
                name: "HoursCodeId",
                table: "PartRides");

            migrationBuilder.DropColumn(
                name: "HoursOptionId",
                table: "PartRides");

            migrationBuilder.DropColumn(
                name: "KilometerReimbursement",
                table: "PartRides");

            migrationBuilder.DropColumn(
                name: "NightAllowance",
                table: "PartRides");

            migrationBuilder.DropColumn(
                name: "SaturdayHours",
                table: "PartRides");

            migrationBuilder.DropColumn(
                name: "StandOver",
                table: "PartRides");

            migrationBuilder.DropColumn(
                name: "SundayHolidayHours",
                table: "PartRides");

            migrationBuilder.DropColumn(
                name: "TaxFreeCompensation",
                table: "PartRides");

            migrationBuilder.DropColumn(
                name: "TotalHours",
                table: "PartRides");

            migrationBuilder.DropColumn(
                name: "VariousCompensation",
                table: "PartRides");
        }
    }
}
