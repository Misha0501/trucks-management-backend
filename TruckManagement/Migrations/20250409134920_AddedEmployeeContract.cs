using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruckManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddedEmployeeContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReleaseVersion = table.Column<string>(type: "text", nullable: true),
                    NightHoursAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    KilometersAllowanceAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    CommuteKilometers = table.Column<double>(type: "double precision", nullable: false),
                    EmployeeFirstName = table.Column<string>(type: "text", nullable: false),
                    EmployeeLastName = table.Column<string>(type: "text", nullable: false),
                    EmployeeAddress = table.Column<string>(type: "text", nullable: false),
                    EmployeePostcode = table.Column<string>(type: "text", nullable: false),
                    EmployeeCity = table.Column<string>(type: "text", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BSN = table.Column<string>(type: "text", nullable: false),
                    DateOfEmployment = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastWorkingDay = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Function = table.Column<string>(type: "text", nullable: false),
                    ProbationPeriod = table.Column<string>(type: "text", nullable: true),
                    WorkweekDuration = table.Column<double>(type: "double precision", nullable: false),
                    WorkweekDurationPercentage = table.Column<double>(type: "double precision", nullable: false),
                    WeeklySchedule = table.Column<string>(type: "text", nullable: false),
                    WorkingHours = table.Column<string>(type: "text", nullable: false),
                    NoticePeriod = table.Column<string>(type: "text", nullable: false),
                    CompensationPerMonthExclBtw = table.Column<decimal>(type: "numeric", nullable: false),
                    CompensationPerMonthInclBtw = table.Column<decimal>(type: "numeric", nullable: false),
                    PayScale = table.Column<string>(type: "text", nullable: false),
                    PayScaleStep = table.Column<int>(type: "integer", nullable: false),
                    HourlyWage100Percent = table.Column<decimal>(type: "numeric", nullable: false),
                    DeviatingWage = table.Column<bool>(type: "boolean", nullable: false),
                    TravelExpenses = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxTravelExpenses = table.Column<decimal>(type: "numeric", nullable: false),
                    VacationAge = table.Column<int>(type: "integer", nullable: false),
                    VacationDays = table.Column<int>(type: "integer", nullable: false),
                    Atv = table.Column<double>(type: "double precision", nullable: false),
                    VacationAllowance = table.Column<decimal>(type: "numeric", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    EmployerName = table.Column<string>(type: "text", nullable: false),
                    CompanyAddress = table.Column<string>(type: "text", nullable: false),
                    CompanyPostcode = table.Column<string>(type: "text", nullable: false),
                    CompanyCity = table.Column<string>(type: "text", nullable: false),
                    CompanyPhoneNumber = table.Column<string>(type: "text", nullable: false),
                    CompanyBtw = table.Column<string>(type: "text", nullable: false),
                    CompanyKvk = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeContracts", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeContracts");
        }
    }
}
