using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddWeekApproval2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WeekApprovalId",
                table: "PartRides",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WeekApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    WeekNr = table.Column<int>(type: "integer", nullable: false),
                    PeriodNr = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdminAllowedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DriverSignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeekApprovals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartRides_WeekApprovalId",
                table: "PartRides",
                column: "WeekApprovalId");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_WeekApprovals_WeekApprovalId",
                table: "PartRides",
                column: "WeekApprovalId",
                principalTable: "WeekApprovals",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_WeekApprovals_WeekApprovalId",
                table: "PartRides");

            migrationBuilder.DropTable(
                name: "WeekApprovals");

            migrationBuilder.DropIndex(
                name: "IX_PartRides_WeekApprovalId",
                table: "PartRides");

            migrationBuilder.DropColumn(
                name: "WeekApprovalId",
                table: "PartRides");
        }
    }
}
