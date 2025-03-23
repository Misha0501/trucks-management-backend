using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovedByUserNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PartRideApprovals_ApprovedByUserId",
                table: "PartRideApprovals",
                column: "ApprovedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRideApprovals_AspNetUsers_ApprovedByUserId",
                table: "PartRideApprovals",
                column: "ApprovedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartRideApprovals_AspNetUsers_ApprovedByUserId",
                table: "PartRideApprovals");

            migrationBuilder.DropIndex(
                name: "IX_PartRideApprovals_ApprovedByUserId",
                table: "PartRideApprovals");
        }
    }
}
