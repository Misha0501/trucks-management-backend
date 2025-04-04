using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddCAO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Caos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StandardUntaxedAllowance = table.Column<decimal>(type: "numeric", nullable: false),
                    MultiDayAfter17Allowance = table.Column<decimal>(type: "numeric", nullable: false),
                    MultiDayBefore17Allowance = table.Column<decimal>(type: "numeric", nullable: false),
                    ShiftMoreThan12hAllowance = table.Column<decimal>(type: "numeric", nullable: false),
                    MultiDayTaxedAllowance = table.Column<decimal>(type: "numeric", nullable: false),
                    MultiDayUntaxedAllowance = table.Column<decimal>(type: "numeric", nullable: false),
                    ConsignmentUntaxedAllowance = table.Column<decimal>(type: "numeric", nullable: false),
                    ConsignmentTaxedAllowance = table.Column<decimal>(type: "numeric", nullable: false),
                    CommuteMinKilometers = table.Column<int>(type: "integer", nullable: false),
                    CommuteMaxKilometers = table.Column<int>(type: "integer", nullable: false),
                    KilometersAllowance = table.Column<decimal>(type: "numeric", nullable: false),
                    NightHoursAllowanceRate = table.Column<decimal>(type: "numeric", nullable: false),
                    NightTimeStart = table.Column<TimeSpan>(type: "interval", nullable: false),
                    NightTimeEnd = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Caos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Caos");
        }
    }
}
