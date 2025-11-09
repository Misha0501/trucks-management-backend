using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.DTOs.Reports;
using TruckManagement.Entities;
using TruckManagement.Utilities;

namespace TruckManagement.Services.Reports;

public class ReportCalculationService
{
    private readonly ApplicationDbContext _db;
    private readonly OvertimeClassifier _overtimeClassifier;
    private readonly VacationCalculator _vacationCalculator;
    private readonly TvTCalculator _tvtCalculator;
    
    public ReportCalculationService(ApplicationDbContext db)
    {
        _db = db;
        _overtimeClassifier = new OvertimeClassifier();
        _vacationCalculator = new VacationCalculator(db);
        _tvtCalculator = new TvTCalculator(db);
    }
    
    public async Task<DriverTimesheetReport> BuildReportAsync(ReportTimeframe timeframe)
    {
        // 1. Gather raw data
        var rawData = await GatherRawDataAsync(timeframe);
        
        // 2. Build report structure
        var report = new DriverTimesheetReport
        {
            CompanyName = rawData.Company?.Name ?? "Unknown Company",
            PersonnelId = rawData.Driver.Id.ToString(),
            DriverName = $"{rawData.Driver.User?.FirstName} {rawData.Driver.User?.LastName}".Trim(),
            Year = timeframe.Year,
            PeriodNumber = timeframe.PeriodNumber ?? (timeframe.WeekNumber.HasValue ? ((timeframe.WeekNumber.Value - 1) / 4) + 1 : 1),
            PeriodRange = timeframe.GetPeriodRange(),
            
            EmployeeInfo = BuildEmployeeInfo(rawData),
            Vacation = await _vacationCalculator.CalculateAsync(timeframe.DriverId, timeframe.Year),
            TimeForTime = await _tvtCalculator.CalculateAsync(timeframe.DriverId, timeframe.Year)
        };
        
        // 3. Process daily entries
        var weeks = await ProcessWeeklyDataAsync(rawData, timeframe.GetWeekNumbers(), timeframe.Year);
        report.Weeks = weeks;
        
        // 4. Calculate summaries
        report.HoursSummary = CalculateHoursSummary(weeks);
        report.GrandTotal = CalculateGrandTotal(weeks);
        
        return report;
    }
    
    private async Task<RawReportData> GatherRawDataAsync(ReportTimeframe timeframe)
    {
        var weekNumbers = timeframe.GetWeekNumbers();
        
        // Load driver with related data
        var driver = await _db.Drivers
            .Include(d => d.User)
            .Include(d => d.Company)
            .Include(d => d.DriverCompensationSettings)
            .FirstOrDefaultAsync(d => d.Id == timeframe.DriverId);
        
        if (driver == null)
            throw new ArgumentException($"Driver with ID {timeframe.DriverId} not found");
        
        // Load employee contract
        var contract = await _db.EmployeeContracts
            .FirstOrDefaultAsync(ec => ec.DriverId == timeframe.DriverId);
        
        var executionsQuery = _db.RideDriverExecutions
            .Include(e => e.HoursCode)
            .Include(e => e.HoursOption)
            .Include(e => e.Ride)
                .ThenInclude(r => r.Client)
            .Include(e => e.Ride)
                .ThenInclude(r => r.Company)
            .Where(e => e.DriverId == timeframe.DriverId)
            .Where(e => e.Ride.PlannedDate.HasValue &&
                        e.Ride.PlannedDate.Value.Year == timeframe.Year);

        var executions = await executionsQuery.ToListAsync();

        var filteredExecutions = executions
            .Where(e =>
            {
                if (e.Ride?.PlannedDate is null)
                    return false;

                var week = e.WeekNumber ?? ISOWeek.GetWeekOfYear(e.Ride.PlannedDate.Value);
                return weekNumbers.Contains(week);
            })
            .OrderBy(e => e.Ride!.PlannedDate)
            .ThenBy(e => e.ActualStartTime ?? e.Ride.PlannedStartTime ?? TimeSpan.Zero)
            .ToList();

        return new RawReportData
        {
            Driver = driver,
            Company = driver.Company,
            Contract = contract,
            CompensationSettings = driver.DriverCompensationSettings,
            DriverExecutions = filteredExecutions
        };
    }
    
    private EmployeeInfoSection BuildEmployeeInfo(RawReportData data)
    {
        return new EmployeeInfoSection
        {
            EmploymentType = "fultime", // Could be derived from contract
            EmploymentPercentage = data.CompensationSettings?.PercentageOfWork ?? 100,
            BirthDate = data.Contract?.DateOfBirth,
            EmploymentStartDate = data.Contract?.DateOfEmployment,
            EmploymentEndDate = data.Contract?.LastWorkingDay,
            CommuteKilometers = data.CompensationSettings?.KilometersOneWayValue ?? 0
        };
    }
    
    private Task<List<WeeklyBreakdown>> ProcessWeeklyDataAsync(RawReportData data, List<int> weekNumbers, int year)
    {
        var weeklyBreakdowns = new List<WeeklyBreakdown>();
        
        foreach (var weekNumber in weekNumbers)
        {
            var weekExecutions = data.DriverExecutions
                .Where(ex => (ex.WeekNumber ?? 0) == weekNumber)
                .OrderBy(ex => ex.Ride?.PlannedDate)
                .ThenBy(ex => ex.ActualStartTime ?? ex.Ride?.PlannedStartTime ?? TimeSpan.Zero)
                .ToList();
            
            var weekBreakdown = new WeeklyBreakdown
            {
                WeekNumber = weekNumber,
                Days = new List<DailyEntry>()
            };
            
            // Process each day of the week
            var weekStartDate = GetWeekStartDate(year, weekNumber);
            
            for (int dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++)
            {
                var currentDate = weekStartDate.AddDays(dayOfWeek);
                var dayExecutions = weekExecutions
                    .Where(ex => ex.Ride?.PlannedDate.HasValue == true &&
                                 ex.Ride!.PlannedDate!.Value.Date == currentDate.Date)
                    .ToList();
                
                var dailyEntry = ProcessDailyEntry(currentDate, dayExecutions);
                weekBreakdown.Days.Add(dailyEntry);
            }
            
            // Calculate week totals
            weekBreakdown.WeekTotal = CalculateWeeklyTotal(weekBreakdown.Days);
            weeklyBreakdowns.Add(weekBreakdown);
        }
        
        return Task.FromResult(weeklyBreakdowns);
    }
    
    private DailyEntry ProcessDailyEntry(DateTime date, List<RideDriverExecution> dayExecutions)
    {
        var dailyEntry = new DailyEntry
        {
            WeekNumber = GetWeekNumber(date),
            DayName = GetDutchDayName(date.DayOfWeek),
            Date = date,
            ServiceCode = "",
            StartTime = null,
            EndTime = null,
            BreakTime = null,
            Corrections = 0,
            TotalHours = 0
        };
        
        if (!dayExecutions.Any())
        {
            return dailyEntry; // Empty day
        }
        
        // Aggregate multiple part rides for the same day
        var totalHours = dayExecutions.Sum(ex => (double)(ex.DecimalHours ?? 0m));
        var corrections = dayExecutions.Sum(ex => (double)ex.CorrectionTotalHours);
        
        // Take first part ride for basic info
        var mainExecution = dayExecutions
            .OrderBy(ex => ex.ActualStartTime ?? ex.Ride?.PlannedStartTime ?? TimeSpan.Zero)
            .First();
        
        // Check if it's a holiday
        var workCalc = new WorkHoursCalculator(new Cao()); // You might need to get actual CAO data
        var isHoliday = !string.IsNullOrEmpty(workCalc.GetHolidayName(date, mainExecution.HoursOption?.Name));
        var isNightShift = _overtimeClassifier.IsNightShift(mainExecution);
        
        // Classify hours using overtime rules
        var weeklyTotal = 0.0; // This would need to be calculated from the week's context
        var hourBreakdown = _overtimeClassifier.ClassifyHours(mainExecution, weeklyTotal, isHoliday, isNightShift);
        
        // Populate daily entry
        dailyEntry.ServiceCode = mainExecution.HoursCode?.Name ?? "";
        dailyEntry.StartTime = mainExecution.ActualStartTime ?? mainExecution.Ride?.PlannedStartTime;
        dailyEntry.EndTime = mainExecution.ActualEndTime ?? mainExecution.Ride?.PlannedEndTime;
        dailyEntry.BreakTime = mainExecution.ActualRestTime ?? mainExecution.RestCalculated;
        dailyEntry.Corrections = corrections;
        dailyEntry.TotalHours = totalHours;
        
        // Hour categories
        dailyEntry.Hours100 = hourBreakdown.Regular100;
        dailyEntry.Hours130 = hourBreakdown.Overtime130;
        dailyEntry.Hours150 = hourBreakdown.Overtime150;
        dailyEntry.Hours200 = hourBreakdown.Premium200;
        
        // Allowances
        dailyEntry.TravelAllowance = dayExecutions.Sum(ex => ex.KilometerReimbursement ?? 0);
        dailyEntry.ConsignmentFee = dayExecutions.Sum(ex => ex.ConsignmentFee ?? 0);
        dailyEntry.VariousCompensation = dayExecutions.Sum(ex => ex.VariousCompensation ?? 0);
        dailyEntry.TaxFreeAmount = dayExecutions.Sum(ex => ex.TaxFreeCompensation ?? 0);
        dailyEntry.TaxableAmount = dayExecutions.Sum(ex => ex.ActualCosts ?? 0);
        dailyEntry.NightAllowanceAmount = dayExecutions.Sum(ex => ex.NightAllowance ?? 0);
        dailyEntry.Kilometers = dayExecutions.Sum(ex => (double)((ex.ActualKilometers ?? 0) + (ex.ExtraKilometers ?? 0)));
        dailyEntry.KilometerAllowance = dayExecutions.Sum(ex => ex.KilometerReimbursement ?? 0);
        dailyEntry.DiverseAllowance = dayExecutions.Sum(ex => ex.VariousCompensation ?? 0);
        dailyEntry.AccommodationAllowance = dayExecutions.Sum(ex => ex.StandOver ?? 0);
        
        // TvT hours (only for Time-for-Time entries)
        dailyEntry.TvTHours = dayExecutions
            .Where(ex => ex.HoursCode?.Name == "Time for time")
            .Sum(ex => (double)(ex.DecimalHours ?? 0m));
        
        dailyEntry.Remarks = string.Join("; ", dayExecutions
            .Where(ex => !string.IsNullOrWhiteSpace(ex.Remark))
            .Select(ex => ex.Remark));
        
        return dailyEntry;
    }
    
    private WeeklyTotal CalculateWeeklyTotal(List<DailyEntry> days)
    {
        return new WeeklyTotal
        {
            TotalHours = days.Sum(d => d.TotalHours),
            Hours100 = days.Sum(d => d.Hours100),
            Hours130 = days.Sum(d => d.Hours130),
            Hours150 = days.Sum(d => d.Hours150),
            Hours200 = days.Sum(d => d.Hours200),
            TaxFreeAmount = days.Sum(d => d.TaxFreeAmount),
            TaxableAmount = days.Sum(d => d.TaxableAmount),
            NightAllowanceAmount = days.Sum(d => d.NightAllowanceAmount),
            TotalKilometers = days.Sum(d => d.Kilometers),
            KilometerAllowance = days.Sum(d => d.KilometerAllowance),
            DiverseAllowance = days.Sum(d => d.DiverseAllowance),
            TvTHours = days.Sum(d => d.TvTHours),
            TotalAllowances = days.Sum(d =>
                d.TravelAllowance +
                d.ConsignmentFee +
                d.VariousCompensation +
                d.TaxFreeAmount +
                d.TaxableAmount +
                d.NightAllowanceAmount +
                d.KilometerAllowance +
                d.DiverseAllowance +
                d.AccommodationAllowance)
        };
    }
    
    private HoursSummarySection CalculateHoursSummary(List<WeeklyBreakdown> weeks)
    {
        return new HoursSummarySection
        {
            Hours100 = weeks.Sum(w => w.WeekTotal.Hours100),
            Hours130 = weeks.Sum(w => w.WeekTotal.Hours130),
            Hours150 = weeks.Sum(w => w.WeekTotal.Hours150),
            Hours200 = weeks.Sum(w => w.WeekTotal.Hours200),
            NightAllowance19Percent = weeks.Sum(w => w.WeekTotal.Hours200), // Assuming night = 200%
            TotalNightAllowanceAmount = weeks.Sum(w => w.WeekTotal.NightAllowanceAmount)
        };
    }
    
    private TotalSection CalculateGrandTotal(List<WeeklyBreakdown> weeks)
    {
        return new TotalSection
        {
            TotalHours = weeks.Sum(w => w.WeekTotal.TotalHours),
            Hours100 = weeks.Sum(w => w.WeekTotal.Hours100),
            Hours130 = weeks.Sum(w => w.WeekTotal.Hours130),
            Hours150 = weeks.Sum(w => w.WeekTotal.Hours150),
            Hours200 = weeks.Sum(w => w.WeekTotal.Hours200),
            TaxFreeAmount = weeks.Sum(w => w.WeekTotal.TaxFreeAmount),
            TaxableAmount = weeks.Sum(w => w.WeekTotal.TaxableAmount),
            NightAllowanceAmount = weeks.Sum(w => w.WeekTotal.NightAllowanceAmount),
            TotalKilometers = weeks.Sum(w => w.WeekTotal.TotalKilometers),
            KilometerAllowance = weeks.Sum(w => w.WeekTotal.KilometerAllowance),
            DiverseAllowance = weeks.Sum(w => w.WeekTotal.DiverseAllowance),
            TvTHours = weeks.Sum(w => w.WeekTotal.TvTHours),
            TotalAllowances = weeks.Sum(w => w.WeekTotal.TotalAllowances)
        };
    }
    
    private DateTime GetWeekStartDate(int year, int weekNumber) =>
        ISOWeek.ToDateTime(year, Math.Max(1, weekNumber), DayOfWeek.Monday);
    
    private int GetWeekNumber(DateTime date)
    {
        return ISOWeek.GetWeekOfYear(date);
    }
    
    private string GetDutchDayName(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => "Monday",
            DayOfWeek.Tuesday => "Tuesday", 
            DayOfWeek.Wednesday => "Wednesday",
            DayOfWeek.Thursday => "Thursday",
            DayOfWeek.Friday => "Friday",
            DayOfWeek.Saturday => "Saturday",
            DayOfWeek.Sunday => "Sunday",
            _ => dayOfWeek.ToString()
        };
    }
}

public class RawReportData
{
    public Driver Driver { get; set; } = default!;
    public Company? Company { get; set; }
    public EmployeeContract? Contract { get; set; }
    public DriverCompensationSettings? CompensationSettings { get; set; }
    public List<RideDriverExecution> DriverExecutions { get; set; } = new();
} 