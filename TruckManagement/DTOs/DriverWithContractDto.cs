namespace TruckManagement.DTOs
{
    public class DriverWithContractDto
    {
        // User Information
        public string UserId { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Postcode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Remark { get; set; }
        public bool IsApproved { get; set; }
        
        // Driver Information
        public Guid DriverId { get; set; }
        public Guid? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public Guid? CarId { get; set; }
        public string? CarLicensePlate { get; set; }
        public int? CarVehicleYear { get; set; }
        public DateOnly? CarRegistrationDate { get; set; }
        
        // Contract Information
        public Guid? ContractId { get; set; }
        public string ContractStatus { get; set; } = default!;
        public decimal? ReleaseVersion { get; set; }
        
        // Personal Details
        public DateTime? DateOfBirth { get; set; }
        public string? BSN { get; set; }
        public string? IBAN { get; set; }
        
        // Employment Details
        public DateTime? DateOfEmployment { get; set; }
        public DateTime? LastWorkingDay { get; set; }
        public string? Function { get; set; }
        public string? ProbationPeriod { get; set; }
        public double? WorkweekDuration { get; set; }
        public double? WorkweekDurationPercentage { get; set; }
        public string? WeeklySchedule { get; set; }
        public string? WorkingHours { get; set; }
        public string? NoticePeriod { get; set; }
        
        // Work Allowances & Settings
        public bool? NightHoursAllowed { get; set; }
        public bool? KilometersAllowanceAllowed { get; set; }
        public bool? PermanentContract { get; set; }
        public double? CommuteKilometers { get; set; }
        
        // Compensation Details
        public string? PayScale { get; set; }
        public int? PayScaleStep { get; set; }
        public decimal? CompensationPerMonthExclBtw { get; set; }
        public decimal? CompensationPerMonthInclBtw { get; set; }
        public decimal? HourlyWage100Percent { get; set; }
        public decimal? DeviatingWage { get; set; }
        
        // Travel & Expenses
        public decimal? TravelExpenses { get; set; }
        public decimal? MaxTravelExpenses { get; set; }
        
        // Vacation & Benefits
        public int? VacationAge { get; set; }
        public int? VacationDays { get; set; }
        public decimal? Atv { get; set; }
        public decimal? VacationAllowance { get; set; }
        public double VacationHoursLeft { get; set; }
        
        // Company Details (from contract)
        public string? EmployerName { get; set; }
        public string? CompanyAddress { get; set; }
        public string? CompanyPostcode { get; set; }
        public string? CompanyCity { get; set; }
        public string? CompanyPhoneNumber { get; set; }
        public string? CompanyBtw { get; set; }
        public string? CompanyKvk { get; set; }
        
        // Contract Signing Info
        public string? AccessCode { get; set; }
        public DateTime? SignedAt { get; set; }
        public string? SignedFileName { get; set; }
        
        // Contract Creation Tracking (for document generation)
        public DateTime? ContractCreatedAt { get; set; }
        public string? ContractCreatedByUserId { get; set; }
        public string? ContractCreatedByUserName { get; set; }
        
        // Driver Files
        public List<DriverFileDto> Files { get; set; } = new List<DriverFileDto>();
        
        // Companies that can use this driver
        public List<CompanySimpleDto> UsedByCompanies { get; set; } = new List<CompanySimpleDto>();
        
        // Timestamps
        public DateTime CreatedAt { get; set; }
    }
    
    public class DriverFileDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = default!;
        public string OriginalFileName { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public DateTime UploadedAt { get; set; }
    }
} 