using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddSingFieldsToContractsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SignatureText",
                table: "EmployeeContracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SignedAt",
                table: "EmployeeContracts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedByIp",
                table: "EmployeeContracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedFileName",
                table: "EmployeeContracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedUserAgent",
                table: "EmployeeContracts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SignatureText",
                table: "EmployeeContracts");

            migrationBuilder.DropColumn(
                name: "SignedAt",
                table: "EmployeeContracts");

            migrationBuilder.DropColumn(
                name: "SignedByIp",
                table: "EmployeeContracts");

            migrationBuilder.DropColumn(
                name: "SignedFileName",
                table: "EmployeeContracts");

            migrationBuilder.DropColumn(
                name: "SignedUserAgent",
                table: "EmployeeContracts");
        }
    }
}
