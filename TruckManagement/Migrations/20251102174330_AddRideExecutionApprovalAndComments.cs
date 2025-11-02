using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddRideExecutionApprovalAndComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RideDriverExecutionComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RideDriverExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideDriverExecutionComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RideDriverExecutionComments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RideDriverExecutionComments_RideDriverExecutions_RideDriver~",
                        column: x => x.RideDriverExecutionId,
                        principalTable: "RideDriverExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RideDriverExecutionComments_RideDriverExecutionId",
                table: "RideDriverExecutionComments",
                column: "RideDriverExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_RideDriverExecutionComments_UserId",
                table: "RideDriverExecutionComments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RideDriverExecutionComments");
        }
    }
}
