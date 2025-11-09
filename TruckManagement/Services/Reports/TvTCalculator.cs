using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.DTOs.Reports;

namespace TruckManagement.Services.Reports;

public class TvTCalculator
{
    private readonly ApplicationDbContext _db;
    private const string TIME_FOR_TIME_CODE = "Time for time";
    
    public TvTCalculator(ApplicationDbContext db)
    {
        _db = db;
    }
    
    public async Task<TvTSection> CalculateAsync(Guid driverId, int year, int? upToMonth = null)
    {
        var legacyQuery = _db.PartRides
            .Include(pr => pr.HoursCode)
            .Where(pr => 
                pr.DriverId == driverId && 
                pr.Date.Year == year &&
                pr.HoursCode != null &&
                pr.HoursCode.Name == TIME_FOR_TIME_CODE);
        
        // If upToMonth is specified, only include data up to that month
        if (upToMonth.HasValue)
        {
            legacyQuery = legacyQuery.Where(pr => pr.Date.Month <= upToMonth.Value);
        }
        
        var tvtPartRides = await legacyQuery.ToListAsync();

        var executionQuery = _db.RideDriverExecutions
            .Include(ex => ex.HoursCode)
            .Include(ex => ex.Ride)
            .Where(ex =>
                ex.DriverId == driverId &&
                ex.Ride.PlannedDate.HasValue &&
                ex.Ride.PlannedDate.Value.Year == year &&
                ex.HoursCode != null &&
                ex.HoursCode.Name == TIME_FOR_TIME_CODE);

        if (upToMonth.HasValue)
        {
            executionQuery = executionQuery.Where(ex => ex.Ride.PlannedDate!.Value.Month <= upToMonth.Value);
        }

        var tvtExecutions = await executionQuery.ToListAsync();
        
        // Calculate TvT balances
        var savedTvTHours = tvtPartRides
            .Where(pr => (pr.DecimalHours ?? 0) > 0)
            .Sum(pr => pr.DecimalHours ?? 0);
        
        savedTvTHours += tvtExecutions
            .Where(ex => (ex.DecimalHours ?? 0m) > 0)
            .Sum(ex => (double)(ex.DecimalHours ?? 0m));

        var usedTvTHours = Math.Abs(tvtPartRides
            .Where(pr => (pr.DecimalHours ?? 0) < 0)
            .Sum(pr => pr.DecimalHours ?? 0));

        usedTvTHours += Math.Abs(tvtExecutions
            .Where(ex => (ex.DecimalHours ?? 0m) < 0)
            .Sum(ex => (double)(ex.DecimalHours ?? 0m)));
        
        var netTvTBalance = savedTvTHours - usedTvTHours;
        
        // For now, converted TvT hours logic is not implemented
        // This would depend on business rules for converting overtime to TvT
        var convertedTvTHours = 0.0;
        
        // Month-end balance (if calculating up to a specific month)
        var monthEndTvTHours = upToMonth.HasValue ? netTvTBalance : 0.0;
        
        return new TvTSection
        {
            SavedTvTHours = savedTvTHours,
            ConvertedTvTHours = convertedTvTHours,
            UsedTvTHours = usedTvTHours,
            MonthEndTvTHours = monthEndTvTHours
        };
    }
    
    /// <summary>
    /// Calculates TvT balance up to the end of a specific month
    /// </summary>
    public async Task<double> GetTvTBalanceAtEndOfMonth(Guid driverId, int year, int month)
    {
        var tvtSection = await CalculateAsync(driverId, year, month);
        return tvtSection.SavedTvTHours - tvtSection.UsedTvTHours;
    }
} 