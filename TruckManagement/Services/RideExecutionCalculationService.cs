using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Helpers;
using TruckManagement.Utilities;

namespace TruckManagement.Services
{
    public class RideExecutionCalculationService
    {
        private readonly ApplicationDbContext _db;
        private readonly PartRideCalculator _calculator;

        public RideExecutionCalculationService(ApplicationDbContext db)
        {
            _db = db;
            _calculator = new PartRideCalculator(db);
        }

        public async Task<RideDriverExecution> CalculateAndApplyAsync(RideDriverExecution execution, DateTime executionDate)
        {
            // Reuse existing PartRideCalculator logic
            var calcContext = new PartRideCalculationContext(
                Date: executionDate,
                Start: execution.ActualStartTime ?? TimeSpan.Zero,
                End: execution.ActualEndTime ?? TimeSpan.Zero,
                Rest: execution.ActualRestTime ?? TimeSpan.Zero,
                DriverId: execution.DriverId,
                HoursCodeId: execution.HoursCodeId ?? Guid.Parse("AAAA1111-1111-1111-1111-111111111111"), // Default hours code
                HoursOptionId: execution.HoursOptionId,
                ExtraKilometers: (double)(execution.ExtraKilometers ?? 0),
                CorrectionTotalHours: (double)execution.CorrectionTotalHours,
                ContainerWaitingTime: execution.ContainerWaitingTime
            );

            var result = await _calculator.CalculateAsync(calcContext);

            // Apply calculated results to execution
            execution.DecimalHours = (decimal)result.DecimalHours;
            execution.NumberOfHours = (decimal)result.NumberOfHours;
            execution.TaxFreeCompensation = (decimal)result.TaxFreeCompensation;
            execution.NightAllowance = (decimal)result.NightAllowance;
            execution.KilometerReimbursement = (decimal)result.KilometerReimbursement;
            execution.ConsignmentFee = (decimal)result.ConsignmentFee;
            execution.SaturdayHours = (decimal)result.SaturdayHours;
            execution.SundayHolidayHours = (decimal)result.SundayHolidayHours;
            execution.RestCalculated = result.RestCalculated;
            execution.PeriodNumber = result.PeriodNumber;
            execution.WeekNrInPeriod = result.WeekNrInPeriod;
            execution.VacationHoursEarned = (decimal)result.VacationHoursEarned;
            execution.HourlyCompensation = (decimal)result.HourlyCompensation;
            execution.ExceedingContainerWaitingTime = (decimal)result.ExceedingContainerWaitingTime;
            
            // Calculate week number
            execution.WeekNumber = DateHelper.GetIso8601WeekOfYear(executionDate);

            return execution;
        }
    }
}

