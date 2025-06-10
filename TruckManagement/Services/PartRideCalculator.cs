using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Helpers;
using TruckManagement.Utilities;

namespace TruckManagement.Services;

// ────────────────────────────────────────────────────────────────
// 1. Context → send in only what the math needs
// ────────────────────────────────────────────────────────────────
public record PartRideCalculationContext(
    DateTime Date,
    TimeSpan Start,
    TimeSpan End,
    Guid?    DriverId,
    Guid     HoursCodeId,
    Guid?    HoursOptionId,
    double   Kilometers,
    double   CorrectionTotalHours);

// ────────────────────────────────────────────────────────────────
// 2. Result → property names match PartRide exactly
// ────────────────────────────────────────────────────────────────
public record PartRideCalculationResult(
    double   DecimalHours,
    double   NumberOfHours,
    double   TaxFreeCompensation,
    double   NightAllowance,
    double   KilometerReimbursement,
    double   ConsignmentFee,
    double   SaturdayHours,
    double   SundayHolidayHours,
    TimeSpan Rest,
    int      PeriodNumber,
    int      WeekNrInPeriod);

public sealed class PartRideCalculator
{
    private readonly ApplicationDbContext _db;
    private readonly CaoService _caoService;

    public static readonly Guid DefaultHoursCodeId =
        Guid.Parse("AAAA1111-1111-1111-1111-111111111111");

    public PartRideCalculator(ApplicationDbContext db)
    {
        _db         = db;
        _caoService = new CaoService(db);
    }

    public async Task<PartRideCalculationResult> CalculateAsync(PartRideCalculationContext c)
    {
        // 1. Load reference data
        var hoursCode = await _db.HoursCodes.FindAsync(
                            c.HoursCodeId == Guid.Empty ? DefaultHoursCodeId : c.HoursCodeId)
                       ?? throw new InvalidOperationException("HoursCode not found.");

        HoursOption? hoursOption = null;
        if (c.HoursOptionId is Guid optId)
            hoursOption = await _db.HoursOptions.FindAsync(optId);

        var compensation = await _db.DriverCompensationSettings
                             .FirstOrDefaultAsync(x => x.DriverId == c.DriverId)
                         ?? throw new InvalidOperationException("DriverCompensationSettings not found.");

        var caoRow = _caoService.GetCaoRow(c.Date)
                    ?? throw new InvalidOperationException("No CAO entry for this date.");

        // 2. Helper calculators
        var work      = new WorkHoursCalculator(caoRow);
        var kmCalc    = new KilometersAllowance(caoRow);
        var nightCalc = new NightAllowanceCalculator(caoRow);

        // 3. Calculations (same as before, variable names unchanged)
        double startTimeDecimal = c.Start.TotalHours;
        double endTimeDecimal   = c.End.TotalHours;

        string holidayName = work.GetHolidayName(c.Date, hoursOption?.Name);

        double untaxedAllowanceNormalDayPartial =
            work.CalculateUntaxedAllowanceNormalDayPartial(
                startOfShift: startTimeDecimal,
                endOfShift  : endTimeDecimal,
                isHoliday   : !string.IsNullOrWhiteSpace(holidayName));

        double untaxedAllowanceSingleDay =
            work.CalculateUntaxedAllowanceSingleDay(
                hourCode                    : hoursCode.Name,
                startTime                   : startTimeDecimal,
                endTime                     : endTimeDecimal,
                untaxedAllowanceNormalDayPartial: untaxedAllowanceNormalDayPartial);

        double untaxedAllowanceDepartureDay =
            work.CalculateUntaxedAllowanceDepartureDay(
                hourCode            : hoursCode.Name,
                departureStartTime  : startTimeDecimal);

        double untaxedAllowanceIntermediateDay =
            work.CalculateUntaxedAllowanceIntermediateDay(
                hourCode   : hoursCode.Name,
                hourOption : hoursOption?.Name,
                startTime  : startTimeDecimal,
                endTime    : endTimeDecimal);

        double untaxedAllowanceArrivalDay =
            work.CalculateUntaxedAllowanceArrivalDay(
                hourCode      : hoursCode.Name,
                arrivalEndTime: endTimeDecimal);

        double taxFreeCompensation = Math.Round(
            untaxedAllowanceSingleDay     +
            untaxedAllowanceDepartureDay  +
            untaxedAllowanceIntermediateDay +
            untaxedAllowanceArrivalDay, 2);

        double sickHours = work.CalculateSickHours(
            hourCode        : hoursCode.Name,
            holidayName     : holidayName,
            weeklyPercentage: compensation.PercentageOfWork,
            startTime       : startTimeDecimal,
            endTime         : endTimeDecimal);

        double vacationHours = work.CalculateVacationHours(
            hourCode        : hoursCode.Name,
            weeklyPercentage: compensation.PercentageOfWork,
            startTime       : startTimeDecimal,
            endTime         : endTimeDecimal);

        double totalBreak = work.CalculateTotalBreak(
            breakScheduleOn: true,
            startTime      : startTimeDecimal,
            endTime        : endTimeDecimal,
            hourCode       : hoursCode.Name,
            sickHours      : sickHours,
            vacationHours  : vacationHours);

        double totalHours = work.CalculateTotalHours(
            shiftStart     : startTimeDecimal,
            shiftEnd       : endTimeDecimal,
            breakDuration  : totalBreak,
            manualAdjustment: c.CorrectionTotalHours);

        double netHours = work.CalculateNetHours(
            hourCode        : hoursCode.Name,
            day             : c.Date,
            isHoliday       : !string.IsNullOrWhiteSpace(holidayName),
            totalHours      : totalHours,
            weeklyPercentage: compensation.PercentageOfWork);

        double homeWorkDistance = kmCalc.HomeWorkDistance(
            kilometerAllowanceEnabled: compensation.KilometerAllowanceEnabled,
            oneWayValue              : compensation.KilometersOneWayValue);

        double kilometerReimbursement = kmCalc.CalculateKilometersAllowance(
            extraKilometers : c.Kilometers,
            hourCode        : hoursCode.Name,
            hourOption      : hoursOption?.Name,
            totalHours      : netHours,
            homeWorkDistance: homeWorkDistance);

        double nightAllowance = nightCalc.CalculateNightAllowance(
            startTime         : startTimeDecimal,
            endTime           : endTimeDecimal,
            nightHoursAllowed : compensation.NightHoursAllowed,
            driverRate        : (double)compensation.DriverRatePerHour,
            nightHoursWholeHours: false);

        double consignmentFee = work.CalculateConsignmentAllowance(
            hourCode   : hoursCode.Name,
            dateLookup : c.Date,
            startTime  : startTimeDecimal,
            endTime    : endTimeDecimal);

        double saturdayHours = work.CalculateSaturdayHours(
            date        : c.Date,
            holidayName : holidayName,
            hoursCode   : hoursCode.Name,
            totalHours  : totalHours);

        double sundayHolidayHours = work.CalculateSundayHolidayHours(
            date        : c.Date,
            holidayName : holidayName,
            hourCode    : hoursCode.Name,
            totalHours  : totalHours);

        var (year, periodNr, weekNrInPeriod) = DateHelper.GetPeriod(c.Date);

        // 4. Return with your *exact* property names
        return new PartRideCalculationResult(
            DecimalHours          : netHours,
            NumberOfHours         : totalHours,
            TaxFreeCompensation   : taxFreeCompensation,
            NightAllowance        : nightAllowance,
            KilometerReimbursement: kilometerReimbursement,
            ConsignmentFee        : consignmentFee,
            SaturdayHours         : saturdayHours,
            SundayHolidayHours    : sundayHolidayHours,
            Rest                  : TimeSpan.FromHours(totalBreak),
            PeriodNumber          : periodNr,
            WeekNrInPeriod        : weekNrInPeriod);
    }
}