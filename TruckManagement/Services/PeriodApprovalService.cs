using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Helpers;

namespace TruckManagement.Services;

public static class PeriodApprovalService
{
    public static async Task<PeriodApproval> GetOrCreateAsync(
        ApplicationDbContext db, Guid driverId, DateTime rideDateUtc)
    {
        var (year, period, weekNrInPeriod) = DateHelper.GetPeriod(rideDateUtc);

        var approval = await db.PeriodApprovals
            .FirstOrDefaultAsync(a => a.DriverId == driverId &&
                                      a.Year     == year &&
                                      a.PeriodNr == period);

        if (approval != null) return approval;

        approval = new PeriodApproval
        {
            Id       = Guid.NewGuid(),
            DriverId = driverId,
            Year     = year,
            PeriodNr = period,
            Status   = PeriodApprovalStatus.PendingDriver
        };

        db.PeriodApprovals.Add(approval);
        await db.SaveChangesAsync();
        return approval;
    }
}