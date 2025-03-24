using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultiplePartRideApprovalsPerRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PartRideApprovals_PartRideId_RoleId",
                table: "PartRideApprovals");

            migrationBuilder.CreateIndex(
                name: "IX_PartRideApprovals_PartRideId_RoleId",
                table: "PartRideApprovals",
                columns: new[] { "PartRideId", "RoleId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PartRideApprovals_PartRideId_RoleId",
                table: "PartRideApprovals");

            migrationBuilder.CreateIndex(
                name: "IX_PartRideApprovals_PartRideId_RoleId",
                table: "PartRideApprovals",
                columns: new[] { "PartRideId", "RoleId" },
                unique: true);
        }
    }
}
