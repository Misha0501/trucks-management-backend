using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Globalization;
using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Enums;

namespace TruckManagement.Services
{
    /// <summary>
    /// Service for generating driver weekly invoice PDFs.
    /// Orchestrates data loading and invoice generation.
    /// </summary>
    public class DriverInvoiceService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly DriverInvoicePdfBuilder _pdfBuilder;
        private readonly ILogger<DriverInvoiceService> _logger;

        public DriverInvoiceService(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            DriverInvoicePdfBuilder pdfBuilder,
            ILogger<DriverInvoiceService> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _pdfBuilder = pdfBuilder;
            _logger = logger;
        }

        /// <summary>
        /// Generates a weekly invoice PDF for a driver.
        /// </summary>
        /// <param name="driverId">The driver ID</param>
        /// <param name="year">The year</param>
        /// <param name="weekNumber">The week number</param>
        /// <param name="hoursWorked">Total hours worked (can be modified by driver)</param>
        /// <param name="hourlyCompensation">Hourly compensation (can be modified by driver)</param>
        /// <param name="additionalCompensation">Additional compensation (can be modified by driver)</param>
        /// <returns>PDF file as byte array</returns>
        /// <exception cref="InvalidOperationException">When required data is missing or validation fails</exception>
        public async Task<byte[]> GenerateWeekInvoiceAsync(
            Guid driverId,
            int year,
            int weekNumber,
            decimal hoursWorked,
            decimal hourlyCompensation,
            decimal additionalCompensation)
        {
            _logger.LogInformation(
                "Starting invoice generation: DriverId={DriverId}, Year={Year}, Week={Week}",
                driverId, year, weekNumber);

            try
            {
                // Step 1: Verify driver exists and is not deleted
                var driver = await _dbContext.Drivers
                    .Include(d => d.User)
                    .Include(d => d.Company)
                    .FirstOrDefaultAsync(d => d.Id == driverId && !d.IsDeleted);

                if (driver == null)
                {
                    throw new InvalidOperationException($"Driver with ID {driverId} not found or is deleted.");
                }

                if (driver.User == null)
                {
                    throw new InvalidOperationException($"Driver user account not found for driver ID {driverId}.");
                }

                _logger.LogDebug("Driver found: {DriverName}", $"{driver.User.FirstName} {driver.User.LastName}");

                // Step 2: Verify week is signed
                var weekApproval = await _dbContext.WeekApprovals
                    .FirstOrDefaultAsync(wa => 
                        wa.DriverId == driverId && 
                        wa.Year == year && 
                        wa.WeekNr == weekNumber &&
                        wa.Status == WeekApprovalStatus.Signed);

                if (weekApproval == null)
                {
                    throw new InvalidOperationException(
                        $"Week {weekNumber} of year {year} is not signed or does not exist for driver {driverId}. " +
                        "Only signed weeks can have invoices generated.");
                }

                _logger.LogDebug("Week approval found: Status={Status}, SignedAt={SignedAt}", 
                    weekApproval.Status, weekApproval.DriverSignedAt);

                // Step 3: Load company details
                var company = driver.Company;
                if (company == null)
                {
                    // Fallback: try to get company from driver's contract
                    var anyContract = await _dbContext.EmployeeContracts
                        .Where(ec => ec.DriverId == driverId)
                        .OrderByDescending(ec => ec.DateOfEmployment)
                        .FirstOrDefaultAsync();

                    if (anyContract != null && anyContract.CompanyId.HasValue)
                    {
                        company = await _dbContext.Companies
                            .FirstOrDefaultAsync(c => c.Id == anyContract.CompanyId.Value);
                    }

                    if (company == null)
                    {
                        throw new InvalidOperationException(
                            $"Company details not found for driver {driverId}. Cannot generate invoice without company information.");
                    }
                }

                _logger.LogDebug("Company found: {CompanyName}", company.Name);

                // Step 4: Try to load driver's latest contract (to get hourly rate)
                // Contract is optional - we'll use 0 as hourly rate if no contract exists
                var contract = await _dbContext.EmployeeContracts
                    .Where(ec => ec.DriverId == driverId)
                    .OrderByDescending(ec => ec.DateOfEmployment)
                    .FirstOrDefaultAsync();

                var hourlyRate = contract?.HourlyWage100Percent ?? 0;

                if (contract == null)
                {
                    _logger.LogWarning(
                        "No contract found for driver {DriverId}. Using hourly rate = 0 for invoice.", 
                        driverId);
                }
                else if (hourlyRate == 0)
                {
                    _logger.LogWarning(
                        "Hourly rate is 0 in contract for driver {DriverId}. Invoice will show 0 hourly rate.", 
                        driverId);
                }
                else
                {
                    _logger.LogDebug("Contract found: HourlyRate={HourlyRate}", hourlyRate);
                }

                // Step 5: Get exceeding container waiting time from week executions
                // Load all executions for the year first, then filter by week number in memory
                // (EF Core can't translate GetIso8601WeekOfYear to SQL)
                var executionsInYear = await _dbContext.RideDriverExecutions
                    .Include(e => e.Ride)
                    .Where(e => e.DriverId == driverId)
                    .Where(e => e.Ride.PlannedDate.HasValue &&
                                e.Ride.PlannedDate.Value.Year == year)
                    .ToListAsync();

                // Filter by week number in memory
                var executions = executionsInYear
                    .Where(e => e.Ride.PlannedDate.HasValue &&
                                GetIso8601WeekOfYear(e.Ride.PlannedDate.Value) == weekNumber)
                    .ToList();

                var exceedingContainerWaitingTime = executions.Sum(e => e.ExceedingContainerWaitingTime ?? 0);

                _logger.LogDebug(
                    "Found {ExecutionCount} executions for week {Week}. Total exceeding container waiting time: {Time}h",
                    executions.Count, weekNumber, exceedingContainerWaitingTime);

                // Step 6: Validate all required data
                ValidateInvoiceData(driver, driver.User, company, hoursWorked, hourlyCompensation, additionalCompensation);

                // Step 7: Generate PDF
                byte[] pdfBytes;
                try
                {
                    pdfBytes = _pdfBuilder.BuildInvoicePdf(
                        driver,
                        driver.User,
                        company,
                        hourlyRate,
                        year,
                        weekNumber,
                        hoursWorked,
                        hourlyCompensation,
                        additionalCompensation,
                        exceedingContainerWaitingTime);

                    _logger.LogInformation(
                        "Invoice PDF generated successfully: DriverId={DriverId}, Year={Year}, Week={Week}, Size={Size} bytes",
                        driverId, year, weekNumber, pdfBytes.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "PDF generation failed for DriverId={DriverId}, Year={Year}, Week={Week}",
                        driverId, year, weekNumber);
                    throw new Exception("Failed to generate invoice PDF. See inner exception for details.", ex);
                }

                return pdfBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Invoice generation failed: DriverId={DriverId}, Year={Year}, Week={Week}",
                    driverId, year, weekNumber);
                throw;
            }
        }

        /// <summary>
        /// Validates that all required data for invoice generation is present.
        /// </summary>
        private void ValidateInvoiceData(
            Driver driver,
            ApplicationUser driverUser,
            Company company,
            decimal hoursWorked,
            decimal hourlyCompensation,
            decimal additionalCompensation)
        {
            var missingFields = new List<string>();

            // Driver user details
            if (string.IsNullOrWhiteSpace(driverUser.FirstName)) missingFields.Add("Driver FirstName");
            if (string.IsNullOrWhiteSpace(driverUser.LastName)) missingFields.Add("Driver LastName");

            // Company details
            if (string.IsNullOrWhiteSpace(company.Name)) missingFields.Add("Company Name");

            // Values (should not be negative)
            if (hoursWorked < 0) missingFields.Add("HoursWorked (must be >= 0)");
            if (hourlyCompensation < 0) missingFields.Add("HourlyCompensation (must be >= 0)");
            if (additionalCompensation < 0) missingFields.Add("AdditionalCompensation (must be >= 0)");

            if (missingFields.Any())
            {
                throw new InvalidOperationException(
                    $"Cannot generate invoice PDF. Missing or invalid required fields: {string.Join(", ", missingFields)}");
            }
        }

        /// <summary>
        /// Gets the ISO 8601 week number for a given date.
        /// </summary>
        private int GetIso8601WeekOfYear(DateTime date)
        {
            var day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                date = date.AddDays(3);
            }

            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
    }
}

