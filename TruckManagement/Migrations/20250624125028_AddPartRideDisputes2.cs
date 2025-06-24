using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddPartRideDisputes2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DisputeComments_AspNetUsers_AuthorUserId",
                table: "DisputeComments");

            migrationBuilder.DropForeignKey(
                name: "FK_DisputeComments_Disputes_DisputeId",
                table: "DisputeComments");

            migrationBuilder.DropForeignKey(
                name: "FK_Disputes_AspNetUsers_OpenedById",
                table: "Disputes");

            migrationBuilder.DropForeignKey(
                name: "FK_Disputes_PartRides_PartRideId",
                table: "Disputes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Disputes",
                table: "Disputes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DisputeComments",
                table: "DisputeComments");

            migrationBuilder.RenameTable(
                name: "Disputes",
                newName: "PartRideDisputes");

            migrationBuilder.RenameTable(
                name: "DisputeComments",
                newName: "PartRideDisputeComments");

            migrationBuilder.RenameIndex(
                name: "IX_Disputes_PartRideId_Status",
                table: "PartRideDisputes",
                newName: "IX_PartRideDisputes_PartRideId_Status");

            migrationBuilder.RenameIndex(
                name: "IX_Disputes_OpenedById",
                table: "PartRideDisputes",
                newName: "IX_PartRideDisputes_OpenedById");

            migrationBuilder.RenameIndex(
                name: "IX_DisputeComments_DisputeId",
                table: "PartRideDisputeComments",
                newName: "IX_PartRideDisputeComments_DisputeId");

            migrationBuilder.RenameIndex(
                name: "IX_DisputeComments_AuthorUserId",
                table: "PartRideDisputeComments",
                newName: "IX_PartRideDisputeComments_AuthorUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PartRideDisputes",
                table: "PartRideDisputes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PartRideDisputeComments",
                table: "PartRideDisputeComments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRideDisputeComments_AspNetUsers_AuthorUserId",
                table: "PartRideDisputeComments",
                column: "AuthorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartRideDisputeComments_PartRideDisputes_DisputeId",
                table: "PartRideDisputeComments",
                column: "DisputeId",
                principalTable: "PartRideDisputes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartRideDisputes_AspNetUsers_OpenedById",
                table: "PartRideDisputes",
                column: "OpenedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartRideDisputes_PartRides_PartRideId",
                table: "PartRideDisputes",
                column: "PartRideId",
                principalTable: "PartRides",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartRideDisputeComments_AspNetUsers_AuthorUserId",
                table: "PartRideDisputeComments");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRideDisputeComments_PartRideDisputes_DisputeId",
                table: "PartRideDisputeComments");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRideDisputes_AspNetUsers_OpenedById",
                table: "PartRideDisputes");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRideDisputes_PartRides_PartRideId",
                table: "PartRideDisputes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PartRideDisputes",
                table: "PartRideDisputes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PartRideDisputeComments",
                table: "PartRideDisputeComments");

            migrationBuilder.RenameTable(
                name: "PartRideDisputes",
                newName: "Disputes");

            migrationBuilder.RenameTable(
                name: "PartRideDisputeComments",
                newName: "DisputeComments");

            migrationBuilder.RenameIndex(
                name: "IX_PartRideDisputes_PartRideId_Status",
                table: "Disputes",
                newName: "IX_Disputes_PartRideId_Status");

            migrationBuilder.RenameIndex(
                name: "IX_PartRideDisputes_OpenedById",
                table: "Disputes",
                newName: "IX_Disputes_OpenedById");

            migrationBuilder.RenameIndex(
                name: "IX_PartRideDisputeComments_DisputeId",
                table: "DisputeComments",
                newName: "IX_DisputeComments_DisputeId");

            migrationBuilder.RenameIndex(
                name: "IX_PartRideDisputeComments_AuthorUserId",
                table: "DisputeComments",
                newName: "IX_DisputeComments_AuthorUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Disputes",
                table: "Disputes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DisputeComments",
                table: "DisputeComments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DisputeComments_AspNetUsers_AuthorUserId",
                table: "DisputeComments",
                column: "AuthorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DisputeComments_Disputes_DisputeId",
                table: "DisputeComments",
                column: "DisputeId",
                principalTable: "Disputes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Disputes_AspNetUsers_OpenedById",
                table: "Disputes",
                column: "OpenedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Disputes_PartRides_PartRideId",
                table: "Disputes",
                column: "PartRideId",
                principalTable: "PartRides",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
