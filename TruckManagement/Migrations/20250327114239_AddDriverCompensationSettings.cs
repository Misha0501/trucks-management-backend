using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverCompensationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DriverCompensationSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    PercentageOfWork = table.Column<double>(type: "double precision", nullable: false),
                    NightHoursAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    NightHours19Percent = table.Column<bool>(type: "boolean", nullable: false),
                    DriverRatePerHour = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    NightAllowanceRate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    KilometerAllowanceEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    KilometersOneWayValue = table.Column<double>(type: "double precision", nullable: false),
                    KilometersMin = table.Column<double>(type: "double precision", nullable: false),
                    KilometersMax = table.Column<double>(type: "double precision", nullable: false),
                    KilometerAllowance = table.Column<decimal>(type: "numeric(5,3)", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Salary4Weeks = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    WeeklySalary = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    DateOfEmployment = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverCompensationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverCompensationSettings_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverCompensationSettings_DriverId",
                table: "DriverCompensationSettings",
                column: "DriverId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverCompensationSettings");
        }
    }
}
