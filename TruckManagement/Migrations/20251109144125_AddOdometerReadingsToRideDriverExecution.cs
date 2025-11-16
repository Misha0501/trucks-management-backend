using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddOdometerReadingsToRideDriverExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EndKilometers",
                table: "RideDriverExecutions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StartKilometers",
                table: "RideDriverExecutions",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RidePeriodApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    PeriodNr = table.Column<int>(type: "integer", nullable: false),
                    FromDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ToDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DriverSignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DriverSignatureData = table.Column<string>(type: "text", nullable: true),
                    DriverSignedIp = table.Column<string>(type: "text", nullable: true),
                    DriverSignedUserAgent = table.Column<string>(type: "text", nullable: true),
                    DriverPdfPath = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalHours = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalCompensation = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RidePeriodApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RidePeriodApprovals_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RidePeriodApprovals_DriverId_Year_PeriodNr",
                table: "RidePeriodApprovals",
                columns: new[] { "DriverId", "Year", "PeriodNr" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RidePeriodApprovals");

            migrationBuilder.DropColumn(
                name: "EndKilometers",
                table: "RideDriverExecutions");

            migrationBuilder.DropColumn(
                name: "StartKilometers",
                table: "RideDriverExecutions");
        }
    }
}
