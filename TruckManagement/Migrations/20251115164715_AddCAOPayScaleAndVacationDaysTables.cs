using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddCAOPayScaleAndVacationDaysTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CAOPayScales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Scale = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    Step = table.Column<int>(type: "integer", nullable: false),
                    WeeklyWage = table.Column<decimal>(type: "numeric", nullable: false),
                    FourWeekWage = table.Column<decimal>(type: "numeric", nullable: false),
                    MonthlyWage = table.Column<decimal>(type: "numeric", nullable: false),
                    HourlyWage100 = table.Column<decimal>(type: "numeric", nullable: false),
                    HourlyWage130 = table.Column<decimal>(type: "numeric", nullable: false),
                    HourlyWage150 = table.Column<decimal>(type: "numeric", nullable: false),
                    EffectiveYear = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CAOPayScales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CAOVacationDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgeFrom = table.Column<int>(type: "integer", nullable: false),
                    AgeTo = table.Column<int>(type: "integer", nullable: false),
                    AgeGroupDescription = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VacationDays = table.Column<int>(type: "integer", nullable: false),
                    EffectiveYear = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CAOVacationDays", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CAOPayScales_Scale_Step_EffectiveYear",
                table: "CAOPayScales",
                columns: new[] { "Scale", "Step", "EffectiveYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CAOVacationDays_AgeFrom_AgeTo_EffectiveYear",
                table: "CAOVacationDays",
                columns: new[] { "AgeFrom", "AgeTo", "EffectiveYear" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CAOPayScales");

            migrationBuilder.DropTable(
                name: "CAOVacationDays");
        }
    }
}
