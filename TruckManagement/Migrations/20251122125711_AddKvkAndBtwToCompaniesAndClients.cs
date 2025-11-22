using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddKvkAndBtwToCompaniesAndClients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop table only if it exists (safe for servers that never had it)
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""DriverUsedByCompanies"";");

            migrationBuilder.AddColumn<string>(
                name: "Btw",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kvk",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Btw",
                table: "Clients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kvk",
                table: "Clients",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Btw",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Kvk",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Btw",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Kvk",
                table: "Clients");

            migrationBuilder.CreateTable(
                name: "DriverUsedByCompanies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverUsedByCompanies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverUsedByCompanies_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DriverUsedByCompanies_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverUsedByCompanies_CompanyId",
                table: "DriverUsedByCompanies",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverUsedByCompanies_DriverId_CompanyId",
                table: "DriverUsedByCompanies",
                columns: new[] { "DriverId", "CompanyId" },
                unique: true);
        }
    }
}
