using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Enums;
using TruckManagement.Interfaces;

namespace TruckManagement.Services
{
    /// <summary>
    /// Service for generating and managing driver employment contract PDFs.
    /// Orchestrates data loading, PDF generation, file storage, and versioning.
    /// </summary>
    public class DriverContractService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly DriverContractPdfBuilder _pdfBuilder;
        private readonly IContractStorageService _storageService;
        private readonly ILogger<DriverContractService> _logger;

        public DriverContractService(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            DriverContractPdfBuilder pdfBuilder,
            IContractStorageService storageService,
            ILogger<DriverContractService> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _pdfBuilder = pdfBuilder;
            _storageService = storageService;
            _logger = logger;
        }

        /// <summary>
        /// Generates a new driver employment contract PDF.
        /// </summary>
        /// <param name="driverId">The driver ID</param>
        /// <param name="employeeContractId">The employee contract ID</param>
        /// <param name="generatedByUserId">The user ID who triggered generation (optional)</param>
        /// <returns>The created DriverContractVersion entity</returns>
        /// <exception cref="InvalidOperationException">When required data is missing</exception>
        /// <exception cref="Exception">When PDF generation or file save fails</exception>
        public async Task<DriverContractVersion> GenerateContractAsync(
            Guid driverId,
            Guid employeeContractId,
            string? generatedByUserId = null)
        {
            _logger.LogInformation(
                "Starting contract generation: DriverId={DriverId}, ContractId={ContractId}, GeneratedBy={GeneratedBy}",
                driverId, employeeContractId, generatedByUserId ?? "System");

            try
            {
                // Step 1: Load employee contract with validation
                var contract = await _dbContext.EmployeeContracts
                    .FirstOrDefaultAsync(ec => ec.Id == employeeContractId);

                if (contract == null)
                {
                    throw new InvalidOperationException($"Employee contract with ID {employeeContractId} not found.");
                }

                // Step 2: Validate driver exists
                var driverExists = await _dbContext.Drivers.AnyAsync(d => d.Id == driverId);
                if (!driverExists)
                {
                    throw new InvalidOperationException($"Driver with ID {driverId} not found.");
                }

                // Step 3: Load contract creator user (for signature)
                ApplicationUser? createdByUser = null;
                if (!string.IsNullOrWhiteSpace(contract.CreatedByUserId))
                {
                    createdByUser = await _userManager.FindByIdAsync(contract.CreatedByUserId);
                    if (createdByUser == null)
                    {
                        _logger.LogWarning(
                            "Contract creator user not found: UserId={UserId}. Will use company name for signature.",
                            contract.CreatedByUserId);
                    }
                }

                // Step 4: Calculate driver age from DateOfBirth
                if (!contract.DateOfBirth.HasValue)
                {
                    throw new InvalidOperationException("Driver date of birth is required for contract generation.");
                }

                var today = DateTime.UtcNow;
                var age = today.Year - contract.DateOfBirth.Value.Year;
                if (contract.DateOfBirth.Value.Date > today.AddYears(-age)) age--;

                _logger.LogDebug("Driver age calculated: {Age} years", age);

                // Step 5: Look up CAO pay scale (Scale + Step + Year)
                var currentYear = DateTime.UtcNow.Year;
                var payScale = await _dbContext.CAOPayScales
                    .Where(ps => ps.Scale == contract.PayScale 
                              && ps.Step == contract.PayScaleStep 
                              && ps.EffectiveYear == currentYear)
                    .FirstOrDefaultAsync();

                if (payScale == null)
                {
                    _logger.LogWarning(
                        "CAO pay scale not found for Scale={Scale}, Step={Step}, Year={Year}. Using contract hourly wage as fallback.",
                        contract.PayScale, contract.PayScaleStep, currentYear);
                    
                    // Create a fallback pay scale from contract data
                    payScale = new CAOPayScale
                    {
                        Scale = contract.PayScale ?? "D",
                        Step = contract.PayScaleStep ?? 1,
                        HourlyWage100 = contract.HourlyWage100Percent ?? 17.30m,
                        HourlyWage130 = (contract.HourlyWage100Percent ?? 17.30m) * 1.30m,
                        HourlyWage150 = (contract.HourlyWage100Percent ?? 17.30m) * 1.50m,
                        WeeklyWage = (contract.HourlyWage100Percent ?? 17.30m) * 40m,
                        FourWeekWage = (contract.HourlyWage100Percent ?? 17.30m) * 160m,
                        MonthlyWage = (contract.HourlyWage100Percent ?? 17.30m) * 173.33m,
                        EffectiveYear = currentYear,
                        EffectiveFrom = new DateTime(currentYear, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    };
                }

                _logger.LogDebug(
                    "CAO pay scale found: Scale={Scale}, Step={Step}, HourlyWage={Wage}",
                    payScale.Scale, payScale.Step, payScale.HourlyWage100);

                // Step 6: Look up vacation days (Age + Year)
                var vacationDays = await _dbContext.CAOVacationDays
                    .Where(vd => vd.AgeFrom <= age 
                              && vd.AgeTo >= age 
                              && vd.EffectiveYear == currentYear)
                    .FirstOrDefaultAsync();

                if (vacationDays == null)
                {
                    _logger.LogWarning(
                        "CAO vacation days not found for Age={Age}, Year={Year}. Using contract vacation days as fallback.",
                        age, currentYear);
                    
                    // Create fallback vacation days from contract data
                    vacationDays = new CAOVacationDays
                    {
                        AgeFrom = age,
                        AgeTo = age,
                        AgeGroupDescription = $"{age} jaar",
                        VacationDays = contract.VacationDays ?? 25,
                        EffectiveYear = currentYear,
                        EffectiveFrom = new DateTime(currentYear, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    };
                }

                _logger.LogDebug(
                    "CAO vacation days found: Age Group={AgeGroup}, Days={Days}",
                    vacationDays.AgeGroupDescription, vacationDays.VacationDays);

                // Step 7: Validate all required data is present
                ValidateContractData(contract);

                // Step 8: Generate PDF
                byte[] pdfBytes;
                try
                {
                    pdfBytes = _pdfBuilder.BuildContractPdf(contract, payScale, vacationDays, createdByUser);
                    _logger.LogInformation("PDF generated successfully: {Size} bytes", pdfBytes.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PDF generation failed for DriverId={DriverId}", driverId);
                    throw new Exception("Failed to generate contract PDF. See inner exception for details.", ex);
                }

                // Step 9: Get next version number
                var latestVersion = await _dbContext.DriverContractVersions
                    .Where(cv => cv.DriverId == driverId)
                    .OrderByDescending(cv => cv.VersionNumber)
                    .FirstOrDefaultAsync();

                var newVersionNumber = (latestVersion?.VersionNumber ?? 0) + 1;

                // Step 10: Save PDF to file system
                string filePath;
                try
                {
                    filePath = await _storageService.SaveContractPdfAsync(pdfBytes, driverId, newVersionNumber);
                    _logger.LogInformation("PDF saved to storage: {FilePath}", filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save PDF to storage for DriverId={DriverId}", driverId);
                    throw new Exception("Failed to save contract PDF to storage. See inner exception for details.", ex);
                }

                // Step 11: Create contract data snapshot (for audit trail)
                var contractSnapshot = JsonSerializer.Serialize(new
                {
                    contract.EmployeeFirstName,
                    contract.EmployeeLastName,
                    contract.EmployeeAddress,
                    contract.EmployeePostcode,
                    contract.EmployeeCity,
                    contract.DateOfBirth,
                    contract.Bsn,
                    contract.DateOfEmployment,
                    contract.LastWorkingDay,
                    contract.Function,
                    contract.ProbationPeriod,
                    contract.WorkweekDuration,
                    contract.WeeklySchedule,
                    contract.WorkingHours,
                    contract.PayScale,
                    contract.PayScaleStep,
                    contract.HourlyWage100Percent,
                    contract.TravelExpenses,
                    contract.MaxTravelExpenses,
                    contract.VacationDays,
                    contract.Atv,
                    contract.VacationAllowance,
                    contract.CompanyName,
                    contract.CompanyAddress,
                    contract.CompanyPostcode,
                    contract.CompanyCity,
                    PayScaleData = new
                    {
                        payScale.Scale,
                        payScale.Step,
                        payScale.HourlyWage100,
                        payScale.HourlyWage130,
                        payScale.HourlyWage150,
                        payScale.WeeklyWage,
                        payScale.MonthlyWage
                    },
                    VacationData = new
                    {
                        vacationDays.AgeGroupDescription,
                        vacationDays.VacationDays,
                        Age = age
                    },
                    GeneratedAt = DateTime.UtcNow,
                    GeneratedByUser = createdByUser != null ? $"{createdByUser.FirstName} {createdByUser.LastName}" : "System"
                }, new JsonSerializerOptions { WriteIndented = true });

                // Step 12: Start database transaction
                using var transaction = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    // Step 13: Mark previous versions as superseded
                    if (latestVersion != null)
                    {
                        var previousVersions = await _dbContext.DriverContractVersions
                            .Where(cv => cv.DriverId == driverId && cv.IsLatestVersion)
                            .ToListAsync();

                        foreach (var previousVersion in previousVersions)
                        {
                            previousVersion.IsLatestVersion = false;
                            previousVersion.Status = ContractVersionStatus.Superseded;
                        }

                        _logger.LogInformation(
                            "Marked {Count} previous versions as superseded for DriverId={DriverId}",
                            previousVersions.Count, driverId);
                    }

                    // Step 14: Create new DriverContractVersion record
                    var contractVersion = new DriverContractVersion
                    {
                        Id = Guid.NewGuid(),
                        DriverId = driverId,
                        EmployeeContractId = employeeContractId,
                        VersionNumber = newVersionNumber,
                        PdfFileName = _storageService.GetContractFileName(driverId, newVersionNumber),
                        PdfFilePath = filePath,
                        FileSize = pdfBytes.Length,
                        ContentType = "application/pdf",
                        GeneratedAt = DateTime.UtcNow,
                        GeneratedByUserId = generatedByUserId,
                        ContractDataSnapshot = contractSnapshot,
                        IsLatestVersion = true,
                        Status = ContractVersionStatus.Generated,
                        Notes = $"Auto-generated contract version {newVersionNumber}"
                    };

                    _dbContext.DriverContractVersions.Add(contractVersion);
                    await _dbContext.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "Contract version created successfully: DriverId={DriverId}, Version={Version}, VersionId={VersionId}",
                        driverId, newVersionNumber, contractVersion.Id);

                    return contractVersion;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Database transaction failed for DriverId={DriverId}", driverId);
                    
                    // Attempt to clean up the file
                    try
                    {
                        await _storageService.DeleteContractPdfAsync(filePath);
                        _logger.LogInformation("Cleaned up PDF file after transaction rollback: {FilePath}", filePath);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, "Failed to clean up PDF file: {FilePath}", filePath);
                    }

                    throw new Exception("Failed to save contract version to database. Transaction rolled back.", ex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Contract generation failed: DriverId={DriverId}, ContractId={ContractId}",
                    driverId, employeeContractId);
                throw;
            }
        }

        /// <summary>
        /// Validates that the employee contract has all required data for PDF generation.
        /// </summary>
        private void ValidateContractData(EmployeeContract contract)
        {
            var missingFields = new List<string>();

            if (string.IsNullOrWhiteSpace(contract.EmployeeFirstName)) missingFields.Add("EmployeeFirstName");
            if (string.IsNullOrWhiteSpace(contract.EmployeeLastName)) missingFields.Add("EmployeeLastName");
            if (string.IsNullOrWhiteSpace(contract.EmployeeAddress)) missingFields.Add("EmployeeAddress");
            if (string.IsNullOrWhiteSpace(contract.EmployeePostcode)) missingFields.Add("EmployeePostcode");
            if (string.IsNullOrWhiteSpace(contract.EmployeeCity)) missingFields.Add("EmployeeCity");
            if (!contract.DateOfBirth.HasValue) missingFields.Add("DateOfBirth");
            if (string.IsNullOrWhiteSpace(contract.Bsn)) missingFields.Add("Bsn");
            if (!contract.DateOfEmployment.HasValue) missingFields.Add("DateOfEmployment");
            if (string.IsNullOrWhiteSpace(contract.Function)) missingFields.Add("Function");
            if (!contract.WorkweekDuration.HasValue) missingFields.Add("WorkweekDuration");
            if (string.IsNullOrWhiteSpace(contract.WeeklySchedule)) missingFields.Add("WeeklySchedule");
            if (string.IsNullOrWhiteSpace(contract.WorkingHours)) missingFields.Add("WorkingHours");
            if (string.IsNullOrWhiteSpace(contract.PayScale)) missingFields.Add("PayScale");
            if (!contract.PayScaleStep.HasValue) missingFields.Add("PayScaleStep");
            if (string.IsNullOrWhiteSpace(contract.CompanyName)) missingFields.Add("CompanyName");
            if (string.IsNullOrWhiteSpace(contract.CompanyAddress)) missingFields.Add("CompanyAddress");
            if (string.IsNullOrWhiteSpace(contract.CompanyPostcode)) missingFields.Add("CompanyPostcode");
            if (string.IsNullOrWhiteSpace(contract.CompanyCity)) missingFields.Add("CompanyCity");

            if (missingFields.Any())
            {
                throw new InvalidOperationException(
                    $"Cannot generate contract PDF. Missing required fields: {string.Join(", ", missingFields)}");
            }
        }

        /// <summary>
        /// Gets the latest contract version for a driver.
        /// </summary>
        public async Task<DriverContractVersion?> GetLatestContractVersionAsync(Guid driverId)
        {
            return await _dbContext.DriverContractVersions
                .Where(cv => cv.DriverId == driverId && cv.IsLatestVersion)
                .OrderByDescending(cv => cv.GeneratedAt)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets all contract versions for a driver.
        /// </summary>
        public async Task<List<DriverContractVersion>> GetAllContractVersionsAsync(Guid driverId)
        {
            return await _dbContext.DriverContractVersions
                .Where(cv => cv.DriverId == driverId)
                .OrderByDescending(cv => cv.VersionNumber)
                .ToListAsync();
        }

        /// <summary>
        /// Gets a specific contract version by ID.
        /// </summary>
        public async Task<DriverContractVersion?> GetContractVersionByIdAsync(Guid versionId)
        {
            return await _dbContext.DriverContractVersions
                .FirstOrDefaultAsync(cv => cv.Id == versionId);
        }
    }
}


