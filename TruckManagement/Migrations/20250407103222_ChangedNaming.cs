using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class ChangedNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShiftMoreThan12hAllowance",
                table: "Caos",
                newName: "ShiftMoreThan12HAllowance");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShiftMoreThan12HAllowance",
                table: "Caos",
                newName: "ShiftMoreThan12hAllowance");
        }
    }
}
