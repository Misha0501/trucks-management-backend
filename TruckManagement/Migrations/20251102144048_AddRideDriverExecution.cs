using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddRideDriverExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExecutionCompletionStatus",
                table: "Rides",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RideDriverExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RideId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    ActualStartTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ActualEndTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ActualRestTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    RestCalculated = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ActualKilometers = table.Column<decimal>(type: "numeric", nullable: true),
                    ExtraKilometers = table.Column<decimal>(type: "numeric", nullable: true),
                    ActualCosts = table.Column<decimal>(type: "numeric", nullable: true),
                    CostsDescription = table.Column<string>(type: "text", nullable: true),
                    Turnover = table.Column<decimal>(type: "numeric", nullable: true),
                    Remark = table.Column<string>(type: "text", nullable: true),
                    CorrectionTotalHours = table.Column<decimal>(type: "numeric", nullable: false),
                    DecimalHours = table.Column<decimal>(type: "numeric", nullable: true),
                    NumberOfHours = table.Column<decimal>(type: "numeric", nullable: true),
                    PeriodNumber = table.Column<int>(type: "integer", nullable: true),
                    WeekNrInPeriod = table.Column<int>(type: "integer", nullable: true),
                    WeekNumber = table.Column<int>(type: "integer", nullable: true),
                    NightAllowance = table.Column<decimal>(type: "numeric", nullable: true),
                    KilometerReimbursement = table.Column<decimal>(type: "numeric", nullable: true),
                    ConsignmentFee = table.Column<decimal>(type: "numeric", nullable: true),
                    TaxFreeCompensation = table.Column<decimal>(type: "numeric", nullable: true),
                    VariousCompensation = table.Column<decimal>(type: "numeric", nullable: true),
                    StandOver = table.Column<decimal>(type: "numeric", nullable: true),
                    SaturdayHours = table.Column<decimal>(type: "numeric", nullable: true),
                    SundayHolidayHours = table.Column<decimal>(type: "numeric", nullable: true),
                    VacationHoursEarned = table.Column<decimal>(type: "numeric", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    HoursCodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    HoursOptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CharterId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubmittedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideDriverExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RideDriverExecutions_Charters_CharterId",
                        column: x => x.CharterId,
                        principalTable: "Charters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RideDriverExecutions_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RideDriverExecutions_HoursCodes_HoursCodeId",
                        column: x => x.HoursCodeId,
                        principalTable: "HoursCodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RideDriverExecutions_HoursOptions_HoursOptionId",
                        column: x => x.HoursOptionId,
                        principalTable: "HoursOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RideDriverExecutions_Rides_RideId",
                        column: x => x.RideId,
                        principalTable: "Rides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RideDriverExecutions_CharterId",
                table: "RideDriverExecutions",
                column: "CharterId");

            migrationBuilder.CreateIndex(
                name: "IX_RideDriverExecutions_DriverId",
                table: "RideDriverExecutions",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_RideDriverExecutions_HoursCodeId",
                table: "RideDriverExecutions",
                column: "HoursCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_RideDriverExecutions_HoursOptionId",
                table: "RideDriverExecutions",
                column: "HoursOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_RideDriverExecutions_RideId_DriverId",
                table: "RideDriverExecutions",
                columns: new[] { "RideId", "DriverId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RideDriverExecutions");

            migrationBuilder.DropColumn(
                name: "ExecutionCompletionStatus",
                table: "Rides");
        }
    }
}
