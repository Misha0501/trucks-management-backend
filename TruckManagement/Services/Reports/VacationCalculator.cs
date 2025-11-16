using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.DTOs.Reports;

namespace TruckManagement.Services.Reports;

public class VacationCalculator
{
    private readonly ApplicationDbContext _db;
    
    public VacationCalculator(ApplicationDbContext db)
    {
        _db = db;
    }
    
    public async Task<VacationSection> CalculateAsync(Guid driverId, int year)
    {
        // Get driver's age to determine vacation entitlement
        var driver = await _db.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == driverId);
        
        // Get employee contract for birth date
        var contract = await _db.EmployeeContracts
            .FirstOrDefaultAsync(ec => ec.DriverId == driverId);
        
        var annualEntitlementDays = 25; // Default 25 days
        
        if (contract?.DateOfBirth != null)
        {
            var age = year - contract.DateOfBirth.Value.Year;
            
            // Find appropriate vacation right based on age
            var yearEndUtc = new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            var yearStartUtc = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var vacationRight = await _db.VacationRights
                .Where(vr => 
                    (vr.AgeFrom == null || age >= vr.AgeFrom) &&
                    (vr.AgeTo == null || age <= vr.AgeTo) &&
                    vr.StartDate <= yearEndUtc &&
                    (vr.EndDate == null || vr.EndDate >= yearStartUtc))
                .OrderByDescending(vr => vr.StartDate)
                .FirstOrDefaultAsync();
            
            if (vacationRight != null)
            {
                annualEntitlementDays = vacationRight.Right;
            }
        }
        
        var annualEntitlementHours = annualEntitlementDays * 8; // Convert to hours
        
        // Calculate vacation hours from PartRides for the current year (legacy data)
        var legacyVacationHours = await _db.PartRides
            .Where(pr => 
                pr.DriverId == driverId && 
                pr.Date.Year == year &&
                pr.VacationHours.HasValue)
            .SumAsync(pr => pr.VacationHours ?? 0);
        
        // Calculate vacation hours from RideDriverExecutions for the current year (new data)
        var executionVacationHours = await _db.RideDriverExecutions
            .Include(ex => ex.Ride)
            .Where(ex =>
                ex.DriverId == driverId &&
                ex.Ride.PlannedDate.HasValue &&
                ex.Ride.PlannedDate.Value.Year == year &&
                ex.VacationHoursEarned.HasValue)
            .SumAsync(ex => ex.VacationHoursEarned ?? 0);
        
        var totalVacationHours = (double)executionVacationHours + legacyVacationHours;
        
        // Positive hours = earned, negative hours = used
        var hoursUsed = Math.Abs(Math.Min(0, totalVacationHours)); // Only negative values (used)
        var hoursEarned = Math.Max(0, totalVacationHours); // Only positive values (earned)
        var hoursRemaining = totalVacationHours; // Net balance
        
        return new VacationSection
        {
            AnnualEntitlementHours = annualEntitlementHours,
            HoursUsed = hoursUsed,
            HoursRemaining = hoursRemaining,
            TotalVacationDays = hoursRemaining / 8.0 // Convert remaining hours to days
        };
    }
} 