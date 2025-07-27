namespace TruckManagement.DTOs
{
    public class UpdateDriverWithContractRequest
    {
        // User Identity (Optional updates)
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        
        // Company Assignment (Optional)
        public string? CompanyId { get; set; }
        
        // Contract Essentials (Optional)
        public DateTime? DateOfEmployment { get; set; }
        public double? WorkweekDuration { get; set; }
        public string? Function { get; set; }
        
        // Extended User Info (Optional)
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Postcode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Remark { get; set; }
        
        // Contract Details (Optional)
        public DateTime? DateOfBirth { get; set; }
        public string? ProbationPeriod { get; set; }
        public string? WeeklySchedule { get; set; }
        public string? WorkingHours { get; set; }
        public string? NoticePeriod { get; set; }
        public string? PayScale { get; set; }
        public int? PayScaleStep { get; set; }
        public string? BSN { get; set; }
        public DateTime? LastWorkingDay { get; set; }
        
        // Additional Contract Fields
        public int? VacationDays { get; set; }
        public int? VacationAge { get; set; }
        public double? WorkweekDurationPercentage { get; set; }
        
        // Allowances & Settings
        public bool? NightHoursAllowed { get; set; }
        public bool? KilometersAllowanceAllowed { get; set; }
        public double? CommuteKilometers { get; set; }
        
        // Compensation Details
        public decimal? CompensationPerMonthExclBtw { get; set; }
        public decimal? CompensationPerMonthInclBtw { get; set; }
        public decimal? HourlyWage100Percent { get; set; }
        public decimal? DeviatingWage { get; set; }
        
        // Travel & Expenses
        public decimal? TravelExpenses { get; set; }
        public decimal? MaxTravelExpenses { get; set; }
        
        // Vacation Benefits
        public decimal? Atv { get; set; }
        public decimal? VacationAllowance { get; set; }
        
        // Company Details (Optional Override)
        public string? EmployerName { get; set; }
        public string? CompanyBtw { get; set; }
        public string? CompanyKvk { get; set; }
        
        // Car Assignment
        public string? CarId { get; set; }
    }
} 