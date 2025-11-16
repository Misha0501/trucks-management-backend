using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverContractVersionEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DriverContractVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    PdfFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PdfFilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GeneratedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ContractDataSnapshot = table.Column<string>(type: "jsonb", nullable: false),
                    IsLatestVersion = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverContractVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverContractVersions_AspNetUsers_GeneratedByUserId",
                        column: x => x.GeneratedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DriverContractVersions_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DriverContractVersions_EmployeeContracts_EmployeeContractId",
                        column: x => x.EmployeeContractId,
                        principalTable: "EmployeeContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverContractVersions_DriverId_IsLatestVersion",
                table: "DriverContractVersions",
                columns: new[] { "DriverId", "IsLatestVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_DriverContractVersions_DriverId_VersionNumber",
                table: "DriverContractVersions",
                columns: new[] { "DriverId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverContractVersions_EmployeeContractId",
                table: "DriverContractVersions",
                column: "EmployeeContractId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverContractVersions_GeneratedByUserId",
                table: "DriverContractVersions",
                column: "GeneratedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverContractVersions");
        }
    }
}
