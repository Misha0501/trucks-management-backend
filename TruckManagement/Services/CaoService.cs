using TruckManagement.Data;
using TruckManagement.Entities;

namespace TruckManagement.Services;

public class CaoService
{
    private readonly ApplicationDbContext _db;

    public CaoService(ApplicationDbContext db)
    {
        _db = db;
    }

    // Return the CAO entry that applies on a given date
    public Cao? GetCaoRow(DateTime date)
    {
        // For example, pick the row where StartDate <= date < EndDate (or EndDate == null)
        return _db.Caos
            .Where(c => c.StartDate <= date && (c.EndDate == null || c.EndDate >= date))
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefault();
    }
}
