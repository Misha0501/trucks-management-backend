using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverCompensationSettingsOneToOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DriverCompensationSettings",
                table: "DriverCompensationSettings");

            migrationBuilder.DropIndex(
                name: "IX_DriverCompensationSettings_DriverId",
                table: "DriverCompensationSettings");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DriverCompensationSettings",
                table: "DriverCompensationSettings",
                column: "DriverId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DriverCompensationSettings",
                table: "DriverCompensationSettings");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DriverCompensationSettings",
                table: "DriverCompensationSettings",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_DriverCompensationSettings_DriverId",
                table: "DriverCompensationSettings",
                column: "DriverId",
                unique: true);
        }
    }
}
