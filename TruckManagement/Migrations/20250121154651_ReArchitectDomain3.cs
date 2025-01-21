using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class ReArchitectDomain3 : Migration
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

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "ContactPersonClientCompanies",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClientId",
                table: "ContactPersonClientCompanies",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

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

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "ContactPersonClientCompanies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClientId",
                table: "ContactPersonClientCompanies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ContactPersonClientCompanies_Clients_ClientId",
                table: "ContactPersonClientCompanies",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ContactPersonClientCompanies_Companies_CompanyId",
                table: "ContactPersonClientCompanies",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
