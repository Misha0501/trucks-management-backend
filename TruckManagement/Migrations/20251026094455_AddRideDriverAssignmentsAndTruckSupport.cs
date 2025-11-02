using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddRideDriverAssignmentsAndTruckSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PlannedHours",
                table: "Rides",
                newName: "TotalPlannedHours");

            migrationBuilder.AddColumn<Guid>(
                name: "TruckId",
                table: "Rides",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RideDriverAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RideId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedHours = table.Column<decimal>(type: "numeric", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideDriverAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RideDriverAssignments_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RideDriverAssignments_Rides_RideId",
                        column: x => x.RideId,
                        principalTable: "Rides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rides_TruckId",
                table: "Rides",
                column: "TruckId");

            migrationBuilder.CreateIndex(
                name: "IX_RideDriverAssignments_DriverId",
                table: "RideDriverAssignments",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_RideDriverAssignments_RideId",
                table: "RideDriverAssignments",
                column: "RideId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rides_Cars_TruckId",
                table: "Rides",
                column: "TruckId",
                principalTable: "Cars",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rides_Cars_TruckId",
                table: "Rides");

            migrationBuilder.DropTable(
                name: "RideDriverAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Rides_TruckId",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "TruckId",
                table: "Rides");

            migrationBuilder.RenameColumn(
                name: "TotalPlannedHours",
                table: "Rides",
                newName: "PlannedHours");
        }
    }
}
