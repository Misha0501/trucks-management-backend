using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Helpers;

namespace TruckManagement.Services;

public static class WeekApprovalService
{
    public static async Task<WeekApproval> GetOrCreateAsync(
        ApplicationDbContext db, Guid driverId, DateTime rideDate)
    {
        var (year, period, weekNrInPeriod) = DateHelper.GetPeriod(rideDate);
        var isoWeek = DateHelper.GetIso8601WeekOfYear(rideDate);
        
        var wa = await db.WeekApprovals
            .FirstOrDefaultAsync(w =>
                w.DriverId == driverId &&
                w.Year     == year &&
                w.WeekNr   == isoWeek);

        if (wa != null) return wa;

        wa = new WeekApproval
        {
            Id        = Guid.NewGuid(),
            DriverId  = driverId,
            Year      = year,
            WeekNr    = isoWeek,
            PeriodNr  = period
        };

        db.WeekApprovals.Add(wa);
        await db.SaveChangesAsync();
        return wa;
    }
}