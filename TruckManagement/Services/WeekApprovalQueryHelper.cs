using TruckManagement.Entities;

namespace TruckManagement.Services;

public static class WeekApprovalQueryHelper
{
    public static IQueryable<WeekApproval> FilterWeeks(
        IQueryable<WeekApproval> source,
        Guid driverId,
        IEnumerable<Guid>? companyIds,
        WeekApprovalStatus? status = null)
    {
        var query = source.Where(w => w.DriverId == driverId);

        if (status != null)
            query = query.Where(w => w.Status == status);

        if (companyIds != null && companyIds.Any())
        {
            query = query.Where(w =>
                w.PartRides.Any(r =>
                    r.CompanyId != null &&
                    companyIds.Contains(r.CompanyId.Value)));
        }

        return query;
    }
}
