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
        var query = _db.PartRides
            .Include(pr => pr.HoursCode)
            .Where(pr => 
                pr.DriverId == driverId && 
                pr.Date.Year == year &&
                pr.HoursCode != null &&
                pr.HoursCode.Name == TIME_FOR_TIME_CODE);
        
        // If upToMonth is specified, only include data up to that month
        if (upToMonth.HasValue)
        {
            query = query.Where(pr => pr.Date.Month <= upToMonth.Value);
        }
        
        var tvtPartRides = await query.ToListAsync();
        
        // Calculate TvT balances
        var savedTvTHours = tvtPartRides
            .Where(pr => (pr.DecimalHours ?? 0) > 0)
            .Sum(pr => pr.DecimalHours ?? 0);
        
        var usedTvTHours = Math.Abs(tvtPartRides
            .Where(pr => (pr.DecimalHours ?? 0) < 0)
            .Sum(pr => pr.DecimalHours ?? 0));
        
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