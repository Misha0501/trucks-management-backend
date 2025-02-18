using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyIdToRide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Rides",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Rides_CompanyId",
                table: "Rides",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rides_Companies_CompanyId",
                table: "Rides",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rides_Companies_CompanyId",
                table: "Rides");

            migrationBuilder.DropIndex(
                name: "IX_Rides_CompanyId",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Rides");
        }
    }
}
