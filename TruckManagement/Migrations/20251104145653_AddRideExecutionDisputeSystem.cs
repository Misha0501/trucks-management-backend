using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddRideExecutionDisputeSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RideDriverExecutionDisputes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RideDriverExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedById = table.Column<string>(type: "text", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideDriverExecutionDisputes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RideDriverExecutionDisputes_AspNetUsers_ResolvedById",
                        column: x => x.ResolvedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RideDriverExecutionDisputes_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RideDriverExecutionDisputes_RideDriverExecutions_RideDriver~",
                        column: x => x.RideDriverExecutionId,
                        principalTable: "RideDriverExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RideDriverExecutionDisputeComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisputeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorUserId = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideDriverExecutionDisputeComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RideDriverExecutionDisputeComments_AspNetUsers_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RideDriverExecutionDisputeComments_RideDriverExecutionDispu~",
                        column: x => x.DisputeId,
                        principalTable: "RideDriverExecutionDisputes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RideDriverExecutionDisputeComments_AuthorUserId",
                table: "RideDriverExecutionDisputeComments",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RideDriverExecutionDisputeComments_DisputeId",
                table: "RideDriverExecutionDisputeComments",
                column: "DisputeId");

            migrationBuilder.CreateIndex(
                name: "IX_RideDriverExecutionDisputes_DriverId",
                table: "RideDriverExecutionDisputes",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_RideDriverExecutionDisputes_ResolvedById",
                table: "RideDriverExecutionDisputes",
                column: "ResolvedById");

            migrationBuilder.CreateIndex(
                name: "IX_RideDriverExecutionDisputes_RideDriverExecutionId",
                table: "RideDriverExecutionDisputes",
                column: "RideDriverExecutionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RideDriverExecutionDisputeComments");

            migrationBuilder.DropTable(
                name: "RideDriverExecutionDisputes");
        }
    }
}
