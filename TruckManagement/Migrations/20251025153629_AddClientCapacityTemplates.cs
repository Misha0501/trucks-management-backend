using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddClientCapacityTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientCapacityTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MondayTrucks = table.Column<int>(type: "integer", nullable: false),
                    TuesdayTrucks = table.Column<int>(type: "integer", nullable: false),
                    WednesdayTrucks = table.Column<int>(type: "integer", nullable: false),
                    ThursdayTrucks = table.Column<int>(type: "integer", nullable: false),
                    FridayTrucks = table.Column<int>(type: "integer", nullable: false),
                    SaturdayTrucks = table.Column<int>(type: "integer", nullable: false),
                    SundayTrucks = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientCapacityTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientCapacityTemplates_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientCapacityTemplates_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientCapacityTemplates_ClientId",
                table: "ClientCapacityTemplates",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientCapacityTemplates_CompanyId",
                table: "ClientCapacityTemplates",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientCapacityTemplates");
        }
    }
}
