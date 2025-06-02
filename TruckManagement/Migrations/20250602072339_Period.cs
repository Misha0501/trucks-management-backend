using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class Period : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PeriodApprovalId",
                table: "PartRides",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PeriodApproval",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    PeriodNr = table.Column<int>(type: "integer", nullable: false),
                    DriverSignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DriverSignedIp = table.Column<string>(type: "text", nullable: true),
                    DriverSignedUa = table.Column<string>(type: "text", nullable: true),
                    DriverPdfPath = table.Column<string>(type: "text", nullable: true),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdminSignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdminSignedIp = table.Column<string>(type: "text", nullable: true),
                    AdminSignedUa = table.Column<string>(type: "text", nullable: true),
                    AdminPdfPath = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodApproval", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartRides_PeriodApprovalId",
                table: "PartRides",
                column: "PeriodApprovalId");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_PeriodApproval_PeriodApprovalId",
                table: "PartRides",
                column: "PeriodApprovalId",
                principalTable: "PeriodApproval",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_PeriodApproval_PeriodApprovalId",
                table: "PartRides");

            migrationBuilder.DropTable(
                name: "PeriodApproval");

            migrationBuilder.DropIndex(
                name: "IX_PartRides_PeriodApprovalId",
                table: "PartRides");

            migrationBuilder.DropColumn(
                name: "PeriodApprovalId",
                table: "PartRides");
        }
    }
}
