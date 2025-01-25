using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddNavigationProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactPersonClientCompanies_Clients_ClientId",
                table: "ContactPersonClientCompanies");

            migrationBuilder.DropForeignKey(
                name: "FK_ContactPersonClientCompanies_Companies_CompanyId",
                table: "ContactPersonClientCompanies");

            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_Companies_CompanyId",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_AspNetUserId",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_ContactPersons_AspNetUserId",
                table: "ContactPersons");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_AspNetUserId",
                table: "Drivers",
                column: "AspNetUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactPersons_AspNetUserId",
                table: "ContactPersons",
                column: "AspNetUserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ContactPersonClientCompanies_Clients_ClientId",
                table: "ContactPersonClientCompanies",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContactPersonClientCompanies_Companies_CompanyId",
                table: "ContactPersonClientCompanies",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Companies_CompanyId",
                table: "Drivers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactPersonClientCompanies_Clients_ClientId",
                table: "ContactPersonClientCompanies");

            migrationBuilder.DropForeignKey(
                name: "FK_ContactPersonClientCompanies_Companies_CompanyId",
                table: "ContactPersonClientCompanies");

            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_Companies_CompanyId",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_AspNetUserId",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_ContactPersons_AspNetUserId",
                table: "ContactPersons");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_AspNetUserId",
                table: "Drivers",
                column: "AspNetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactPersons_AspNetUserId",
                table: "ContactPersons",
                column: "AspNetUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContactPersonClientCompanies_Clients_ClientId",
                table: "ContactPersonClientCompanies",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ContactPersonClientCompanies_Companies_CompanyId",
                table: "ContactPersonClientCompanies",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Companies_CompanyId",
                table: "Drivers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");
        }
    }
}
