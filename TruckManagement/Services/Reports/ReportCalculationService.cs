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
        var weeks = await ProcessWeeklyDataAsync(rawData, timeframe.GetWeekNumbers());
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
        
        // Load part rides for the specified weeks
        var partRides = await _db.PartRides
            .Include(pr => pr.HoursCode)
            .Include(pr => pr.HoursOption)
            .Include(pr => pr.Client)
            .Where(pr => 
                pr.DriverId == timeframe.DriverId &&
                pr.Date.Year == timeframe.Year &&
                weekNumbers.Contains(pr.WeekNumber ?? 0))
            .OrderBy(pr => pr.Date)
            .ToListAsync();
        
        return new RawReportData
        {
            Driver = driver,
            Company = driver.Company,
            Contract = contract,
            CompensationSettings = driver.DriverCompensationSettings,
            PartRides = partRides
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
    
    private async Task<List<WeeklyBreakdown>> ProcessWeeklyDataAsync(RawReportData data, List<int> weekNumbers)
    {
        var weeklyBreakdowns = new List<WeeklyBreakdown>();
        
        foreach (var weekNumber in weekNumbers)
        {
            var weekPartRides = data.PartRides
                .Where(pr => pr.WeekNumber == weekNumber)
                .ToList();
            
            var weekBreakdown = new WeeklyBreakdown
            {
                WeekNumber = weekNumber,
                Days = new List<DailyEntry>()
            };
            
            // Process each day of the week
            var weekStartDate = GetWeekStartDate(data.PartRides.FirstOrDefault()?.Date.Year ?? DateTime.Now.Year, weekNumber);
            
            for (int dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++)
            {
                var currentDate = weekStartDate.AddDays(dayOfWeek);
                var dayPartRides = weekPartRides.Where(pr => pr.Date.Date == currentDate.Date).ToList();
                
                var dailyEntry = await ProcessDailyEntryAsync(currentDate, dayPartRides, data);
                weekBreakdown.Days.Add(dailyEntry);
            }
            
            // Calculate week totals
            weekBreakdown.WeekTotal = CalculateWeeklyTotal(weekBreakdown.Days);
            weeklyBreakdowns.Add(weekBreakdown);
        }
        
        return weeklyBreakdowns;
    }
    
    private async Task<DailyEntry> ProcessDailyEntryAsync(DateTime date, List<PartRide> dayPartRides, RawReportData data)
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
        
        if (!dayPartRides.Any())
        {
            return dailyEntry; // Empty day
        }
        
        // Aggregate multiple part rides for the same day
        var totalHours = dayPartRides.Sum(pr => pr.DecimalHours ?? 0);
        var corrections = dayPartRides.Sum(pr => pr.CorrectionTotalHours);
        
        // Take first part ride for basic info
        var mainPartRide = dayPartRides.First();
        
        // Check if it's a holiday
        var workCalc = new WorkHoursCalculator(new Cao()); // You might need to get actual CAO data
        var isHoliday = !string.IsNullOrEmpty(workCalc.GetHolidayName(date, mainPartRide.HoursOption?.Name));
        var isNightShift = _overtimeClassifier.IsNightShift(mainPartRide);
        
        // Classify hours using overtime rules
        var weeklyTotal = 0.0; // This would need to be calculated from the week's context
        var hourBreakdown = _overtimeClassifier.ClassifyHours(mainPartRide, weeklyTotal, isHoliday, isNightShift);
        
        // Populate daily entry
        dailyEntry.ServiceCode = mainPartRide.HoursCode?.Name ?? "";
        dailyEntry.StartTime = mainPartRide.Start;
        dailyEntry.EndTime = mainPartRide.End;
        dailyEntry.BreakTime = mainPartRide.Rest;
        dailyEntry.Corrections = corrections;
        dailyEntry.TotalHours = totalHours;
        
        // Hour categories
        dailyEntry.Hours100 = hourBreakdown.Regular100;
        dailyEntry.Hours130 = hourBreakdown.Overtime130;
        dailyEntry.Hours150 = hourBreakdown.Overtime150;
        dailyEntry.Hours200 = hourBreakdown.Premium200;
        
        // Allowances
        dailyEntry.TravelAllowance = (decimal)dayPartRides.Sum(pr => pr.KilometerReimbursement);
        dailyEntry.ConsignmentFee = (decimal)dayPartRides.Sum(pr => pr.ConsignmentFee);
        dailyEntry.VariousCompensation = (decimal)dayPartRides.Sum(pr => pr.VariousCompensation);
        dailyEntry.TaxFreeAmount = (decimal)dayPartRides.Sum(pr => pr.TaxFreeCompensation);
        dailyEntry.NightAllowanceAmount = (decimal)dayPartRides.Sum(pr => pr.NightAllowance);
        dailyEntry.Kilometers = dayPartRides.Sum(pr => pr.TotalKilometers ?? 0);
        
        // TvT hours (only for Time-for-Time entries)
        dailyEntry.TvTHours = dayPartRides
            .Where(pr => pr.HoursCode?.Name == "Time for time")
            .Sum(pr => pr.DecimalHours ?? 0);
        
        dailyEntry.Remarks = string.Join("; ", dayPartRides
            .Where(pr => !string.IsNullOrEmpty(pr.Remark))
            .Select(pr => pr.Remark));
        
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
            TvTHours = days.Sum(d => d.TvTHours)
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
            TvTHours = weeks.Sum(w => w.WeekTotal.TvTHours)
        };
    }
    
    private DateTime GetWeekStartDate(int year, int weekNumber)
    {
        var jan1 = new DateTime(year, 1, 1);
        var daysOffset = (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek - (int)jan1.DayOfWeek;
        var firstWeek = jan1.AddDays(daysOffset);
        
        return firstWeek.AddDays((weekNumber - 1) * 7);
    }
    
    private int GetWeekNumber(DateTime date)
    {
        var culture = CultureInfo.CurrentCulture;
        return culture.Calendar.GetWeekOfYear(date, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
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
    public List<PartRide> PartRides { get; set; } = new List<PartRide>();
} 