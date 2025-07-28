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
            var vacationRight = await _db.VacationRights
                .Where(vr => 
                    (vr.AgeFrom == null || age >= vr.AgeFrom) &&
                    (vr.AgeTo == null || age <= vr.AgeTo) &&
                    vr.StartDate <= new DateTime(year, 12, 31) &&
                    (vr.EndDate == null || vr.EndDate >= new DateTime(year, 1, 1)))
                .OrderByDescending(vr => vr.StartDate)
                .FirstOrDefaultAsync();
            
            if (vacationRight != null)
            {
                annualEntitlementDays = vacationRight.Right;
            }
        }
        
        var annualEntitlementHours = annualEntitlementDays * 8; // Convert to hours
        
        // Calculate vacation hours from PartRides for the current year
        var vacationHours = await _db.PartRides
            .Where(pr => 
                pr.DriverId == driverId && 
                pr.Date.Year == year &&
                pr.VacationHours.HasValue)
            .SumAsync(pr => pr.VacationHours ?? 0);
        
        // Positive hours = earned, negative hours = used
        var hoursUsed = Math.Abs(Math.Min(0, vacationHours)); // Only negative values (used)
        var hoursEarned = Math.Max(0, vacationHours); // Only positive values (earned)
        var hoursRemaining = vacationHours; // Net balance
        
        return new VacationSection
        {
            AnnualEntitlementHours = annualEntitlementHours,
            HoursUsed = hoursUsed,
            HoursRemaining = hoursRemaining,
            TotalVacationDays = hoursRemaining / 8.0 // Convert remaining hours to days
        };
    }
} 