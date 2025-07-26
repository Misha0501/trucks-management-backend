using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class DropCarDriverAddDriverCarRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarDrivers");

            migrationBuilder.AddColumn<Guid>(
                name: "CarId",
                table: "Drivers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_CarId",
                table: "Drivers",
                column: "CarId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Cars_CarId",
                table: "Drivers",
                column: "CarId",
                principalTable: "Cars",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_Cars_CarId",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_CarId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "CarId",
                table: "Drivers");

            migrationBuilder.CreateTable(
                name: "CarDrivers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarDrivers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarDrivers_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CarDrivers_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarDrivers_CarId",
                table: "CarDrivers",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_CarDrivers_DriverId",
                table: "CarDrivers",
                column: "DriverId");
        }
    }
}
