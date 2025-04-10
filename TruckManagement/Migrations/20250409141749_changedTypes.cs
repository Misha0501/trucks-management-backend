using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class changedTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
    ALTER TABLE ""EmployeeContracts""
    ALTER COLUMN ""DeviatingWage"" TYPE numeric
    USING CASE
        WHEN ""DeviatingWage"" = TRUE THEN 1
        ELSE 0
    END;
");

            migrationBuilder.AlterColumn<decimal>(
                name: "Atv",
                table: "EmployeeContracts",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "DeviatingWage",
                table: "EmployeeContracts",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<double>(
                name: "Atv",
                table: "EmployeeContracts",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }
    }
}
