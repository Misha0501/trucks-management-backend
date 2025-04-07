using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class ChangedType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "CommuteMinKilometers",
                table: "Caos",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "CommuteMaxKilometers",
                table: "Caos",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CommuteMinKilometers",
                table: "Caos",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "CommuteMaxKilometers",
                table: "Caos",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }
    }
}
