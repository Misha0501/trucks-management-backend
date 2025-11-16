using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddHourlyCompensationAndExceedingContainerWaitingTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExceedingContainerWaitingTime",
                table: "RideDriverExecutions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyCompensation",
                table: "RideDriverExecutions",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExceedingContainerWaitingTime",
                table: "RideDriverExecutions");

            migrationBuilder.DropColumn(
                name: "HourlyCompensation",
                table: "RideDriverExecutions");
        }
    }
}
