using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddPartRideApprovalAndComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PartRideApproval",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartRideId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ApprovedByUserId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartRideApproval", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartRideApproval_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartRideApproval_PartRides_PartRideId",
                        column: x => x.PartRideId,
                        principalTable: "PartRides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartRideComment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartRideId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorUserId = table.Column<string>(type: "text", nullable: false),
                    AuthorRoleId = table.Column<string>(type: "text", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartRideComment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartRideComment_AspNetRoles_AuthorRoleId",
                        column: x => x.AuthorRoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PartRideComment_PartRides_PartRideId",
                        column: x => x.PartRideId,
                        principalTable: "PartRides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartRideApproval_PartRideId_RoleId",
                table: "PartRideApproval",
                columns: new[] { "PartRideId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartRideApproval_RoleId",
                table: "PartRideApproval",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PartRideComment_AuthorRoleId",
                table: "PartRideComment",
                column: "AuthorRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PartRideComment_PartRideId",
                table: "PartRideComment",
                column: "PartRideId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartRideApproval");

            migrationBuilder.DropTable(
                name: "PartRideComment");
        }
    }
}
