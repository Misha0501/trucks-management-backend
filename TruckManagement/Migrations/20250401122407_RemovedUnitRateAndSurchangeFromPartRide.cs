using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class RemovedUnitRateAndSurchangeFromPartRide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Rates_RateId",
                table: "PartRides");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Surcharges_SurchargeId",
                table: "PartRides");

            migrationBuilder.DropForeignKey(
                name: "FK_PartRides_Units_UnitId",
                table: "PartRides");

            migrationBuilder.DropIndex(
                name: "IX_PartRides_RateId",
                table: "PartRides");

            migrationBuilder.DropIndex(
                name: "IX_PartRides_SurchargeId",
                table: "PartRides");

            migrationBuilder.DropIndex(
                name: "IX_PartRides_UnitId",
                table: "PartRides");

            migrationBuilder.DropColumn(
                name: "RateId",
                table: "PartRides");

            migrationBuilder.DropColumn(
                name: "SurchargeId",
                table: "PartRides");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "PartRides");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RateId",
                table: "PartRides",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SurchargeId",
                table: "PartRides",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UnitId",
                table: "PartRides",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartRides_RateId",
                table: "PartRides",
                column: "RateId");

            migrationBuilder.CreateIndex(
                name: "IX_PartRides_SurchargeId",
                table: "PartRides",
                column: "SurchargeId");

            migrationBuilder.CreateIndex(
                name: "IX_PartRides_UnitId",
                table: "PartRides",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRides_Rates_RateId",
                table: "PartRides",
                column: "RateId",
                principalTable: "Rates",
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
    }
}
