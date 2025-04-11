using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class addedNavigationPropertiesToContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_EmployeeContracts_CompanyId",
                table: "EmployeeContracts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeContracts_DriverId",
                table: "EmployeeContracts",
                column: "DriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeContracts_Companies_CompanyId",
                table: "EmployeeContracts",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeContracts_Drivers_DriverId",
                table: "EmployeeContracts",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeContracts_Companies_CompanyId",
                table: "EmployeeContracts");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeContracts_Drivers_DriverId",
                table: "EmployeeContracts");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeContracts_CompanyId",
                table: "EmployeeContracts");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeContracts_DriverId",
                table: "EmployeeContracts");
        }
    }
}
