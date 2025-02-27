using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePartRideNullablePropertiesTwo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Clients_ClientId",
                table: "PartRides");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClientId",
                table: "PartRides",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_Clients_ClientId",
                table: "PartRides",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Clients_ClientId",
                table: "PartRides");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClientId",
                table: "PartRides",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_Clients_ClientId",
                table: "PartRides",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
