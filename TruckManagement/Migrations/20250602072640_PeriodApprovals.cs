using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class PeriodApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_PeriodApproval_PeriodApprovalId",
                table: "PartRides");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PeriodApproval",
                table: "PeriodApproval");

            migrationBuilder.RenameTable(
                name: "PeriodApproval",
                newName: "PeriodApprovals");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PeriodApprovals",
                table: "PeriodApprovals",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_PeriodApprovals_PeriodApprovalId",
                table: "PartRides",
                column: "PeriodApprovalId",
                principalTable: "PeriodApprovals",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_PeriodApprovals_PeriodApprovalId",
                table: "PartRides");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PeriodApprovals",
                table: "PeriodApprovals");

            migrationBuilder.RenameTable(
                name: "PeriodApprovals",
                newName: "PeriodApproval");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PeriodApproval",
                table: "PeriodApproval",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_PeriodApproval_PeriodApprovalId",
                table: "PartRides",
                column: "PeriodApprovalId",
                principalTable: "PeriodApproval",
                principalColumn: "Id");
        }
    }
}
