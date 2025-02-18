using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddPartRideApprovalsAndComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartRideApproval_AspNetRoles_RoleId",
                table: "PartRideApproval");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRideApproval_PartRides_PartRideId",
                table: "PartRideApproval");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRideComment_AspNetRoles_AuthorRoleId",
                table: "PartRideComment");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRideComment_PartRides_PartRideId",
                table: "PartRideComment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PartRideComment",
                table: "PartRideComment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PartRideApproval",
                table: "PartRideApproval");

            migrationBuilder.RenameTable(
                name: "PartRideComment",
                newName: "PartRideComments");

            migrationBuilder.RenameTable(
                name: "PartRideApproval",
                newName: "PartRideApprovals");

            migrationBuilder.RenameIndex(
                name: "IX_PartRideComment_PartRideId",
                table: "PartRideComments",
                newName: "IX_PartRideComments_PartRideId");

            migrationBuilder.RenameIndex(
                name: "IX_PartRideComment_AuthorRoleId",
                table: "PartRideComments",
                newName: "IX_PartRideComments_AuthorRoleId");

            migrationBuilder.RenameIndex(
                name: "IX_PartRideApproval_RoleId",
                table: "PartRideApprovals",
                newName: "IX_PartRideApprovals_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_PartRideApproval_PartRideId_RoleId",
                table: "PartRideApprovals",
                newName: "IX_PartRideApprovals_PartRideId_RoleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PartRideComments",
                table: "PartRideComments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PartRideApprovals",
                table: "PartRideApprovals",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRideApprovals_AspNetRoles_RoleId",
                table: "PartRideApprovals",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartRideApprovals_PartRides_PartRideId",
                table: "PartRideApprovals",
                column: "PartRideId",
                principalTable: "PartRides",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartRideComments_AspNetRoles_AuthorRoleId",
                table: "PartRideComments",
                column: "AuthorRoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRideComments_PartRides_PartRideId",
                table: "PartRideComments",
                column: "PartRideId",
                principalTable: "PartRides",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartRideApprovals_AspNetRoles_RoleId",
                table: "PartRideApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRideApprovals_PartRides_PartRideId",
                table: "PartRideApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRideComments_AspNetRoles_AuthorRoleId",
                table: "PartRideComments");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRideComments_PartRides_PartRideId",
                table: "PartRideComments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PartRideComments",
                table: "PartRideComments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PartRideApprovals",
                table: "PartRideApprovals");

            migrationBuilder.RenameTable(
                name: "PartRideComments",
                newName: "PartRideComment");

            migrationBuilder.RenameTable(
                name: "PartRideApprovals",
                newName: "PartRideApproval");

            migrationBuilder.RenameIndex(
                name: "IX_PartRideComments_PartRideId",
                table: "PartRideComment",
                newName: "IX_PartRideComment_PartRideId");

            migrationBuilder.RenameIndex(
                name: "IX_PartRideComments_AuthorRoleId",
                table: "PartRideComment",
                newName: "IX_PartRideComment_AuthorRoleId");

            migrationBuilder.RenameIndex(
                name: "IX_PartRideApprovals_RoleId",
                table: "PartRideApproval",
                newName: "IX_PartRideApproval_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_PartRideApprovals_PartRideId_RoleId",
                table: "PartRideApproval",
                newName: "IX_PartRideApproval_PartRideId_RoleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PartRideComment",
                table: "PartRideComment",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PartRideApproval",
                table: "PartRideApproval",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRideApproval_AspNetRoles_RoleId",
                table: "PartRideApproval",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartRideApproval_PartRides_PartRideId",
                table: "PartRideApproval",
                column: "PartRideId",
                principalTable: "PartRides",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartRideComment_AspNetRoles_AuthorRoleId",
                table: "PartRideComment",
                column: "AuthorRoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRideComment_PartRides_PartRideId",
                table: "PartRideComment",
                column: "PartRideId",
                principalTable: "PartRides",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
