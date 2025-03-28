using TruckManagement.Data;
using TruckManagement.Entities;

namespace TruckManagement.Services
{
    public class DriverCompensationService
    {
        private readonly ApplicationDbContext _dbContext;

        public DriverCompensationService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task CreateDefaultDriverCompensationSettingsAsync(Driver driver)
        {
            var settings = new DriverCompensationSettings
            {
                Id = Guid.NewGuid(),
                DriverId = driver.Id,
                PercentageOfWork = 100,
                NightHoursAllowed = true,
                NightHours19Percent = true,
                DriverRatePerHour = 18.71m,
                NightAllowanceRate = 0.19m,
                KilometerAllowanceEnabled = true,
                KilometersOneWayValue = 25,
                KilometersMin = 10,
                KilometersMax = 35,
                KilometerAllowance = 0.23m,
                Salary4Weeks = 2400m,
                WeeklySalary = 600m,
                DateOfEmployment = DateTime.UtcNow
            };

            _dbContext.DriverCompensationSettings.Add(settings);
            // No SaveChangesAsync — the calling code handles persistence
        }
    }
}