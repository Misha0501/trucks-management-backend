using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class fixedTypo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StandOverIntermidiateDayUntaxed",
                table: "Caos",
                newName: "StandOverIntermediateDayUntaxed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StandOverIntermediateDayUntaxed",
                table: "Caos",
                newName: "StandOverIntermidiateDayUntaxed");
        }
    }
}
