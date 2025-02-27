using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePartRideNullableProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Cars_CarId",
                table: "PartRides");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Charters_CharterId",
                table: "PartRides");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Companies_CompanyId",
                table: "PartRides");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Drivers_DriverId",
                table: "PartRides");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Rates_RateId",
                table: "PartRides");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Rides_RideId",
                table: "PartRides");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Surcharges_SurchargeId",
                table: "PartRides");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Units_UnitId",
                table: "PartRides");

            migrationBuilder.AlterColumn<Guid>(
                name: "UnitId",
                table: "PartRides",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "SurchargeId",
                table: "PartRides",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "RideId",
                table: "PartRides",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "RateId",
                table: "PartRides",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "DriverId",
                table: "PartRides",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "PartRides",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "CharterId",
                table: "PartRides",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "CarId",
                table: "PartRides",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_Cars_CarId",
                table: "PartRides",
                column: "CarId",
                principalTable: "Cars",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_Charters_CharterId",
                table: "PartRides",
                column: "CharterId",
                principalTable: "Charters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_Companies_CompanyId",
                table: "PartRides",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_Drivers_DriverId",
                table: "PartRides",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_Rates_RateId",
                table: "PartRides",
                column: "RateId",
                principalTable: "Rates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_Rides_RideId",
                table: "PartRides",
                column: "RideId",
                principalTable: "Rides",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_Surcharges_SurchargeId",
                table: "PartRides",
                column: "SurchargeId",
                principalTable: "Surcharges",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_Units_UnitId",
                table: "PartRides",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Cars_CarId",
                table: "PartRides");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Charters_CharterId",
                table: "PartRides");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Companies_CompanyId",
                table: "PartRides");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Drivers_DriverId",
                table: "PartRides");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Rates_RateId",
                table: "PartRides");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Rides_RideId",
                table: "PartRides");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Surcharges_SurchargeId",
                table: "PartRides");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Units_UnitId",
                table: "PartRides");

            migrationBuilder.AlterColumn<Guid>(
                name: "UnitId",
                table: "PartRides",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SurchargeId",
                table: "PartRides",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "RideId",
                table: "PartRides",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "RateId",
                table: "PartRides",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "DriverId",
                table: "PartRides",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "PartRides",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CharterId",
                table: "PartRides",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CarId",
                table: "PartRides",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_Cars_CarId",
                table: "PartRides",
                column: "CarId",
                principalTable: "Cars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_Charters_CharterId",
                table: "PartRides",
                column: "CharterId",
                principalTable: "Charters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_Companies_CompanyId",
                table: "PartRides",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_Drivers_DriverId",
                table: "PartRides",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_Rates_RateId",
                table: "PartRides",
                column: "RateId",
                principalTable: "Rates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_Rides_RideId",
                table: "PartRides",
                column: "RideId",
                principalTable: "Rides",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_Surcharges_SurchargeId",
                table: "PartRides",
                column: "SurchargeId",
                principalTable: "Surcharges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_Units_UnitId",
                table: "PartRides",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
