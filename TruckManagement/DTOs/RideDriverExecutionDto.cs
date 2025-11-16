using TruckManagement.Enums;

namespace TruckManagement.DTOs
{
    // Request DTO for submitting execution
    public class SubmitExecutionRequest
    {
        public TimeSpan? ActualStartTime { get; set; }
        public TimeSpan? ActualEndTime { get; set; }
        public TimeSpan? ActualRestTime { get; set; }
        public TimeSpan? ContainerWaitingTime { get; set; }
        
        // Odometer readings
        public decimal? StartKilometers { get; set; }
        public decimal? EndKilometers { get; set; }
        
        public decimal? ActualKilometers { get; set; }
        public decimal? ExtraKilometers { get; set; }
        public decimal? ActualCosts { get; set; }
        public string? CostsDescription { get; set; }
        public decimal? Turnover { get; set; }
        public string? Remark { get; set; }
        public decimal CorrectionTotalHours { get; set; } = 0;
        public string? HoursCodeId { get; set; }
        public string? HoursOptionId { get; set; }
        public string? CharterId { get; set; }
        public decimal? VariousCompensation { get; set; }
        
        // Optional files to upload with execution
        public List<ExecutionFileUpload>? Files { get; set; }
    }
    
    // DTO for file upload within execution submission
    public class ExecutionFileUpload
    {
        public string FileName { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public string FileDataBase64 { get; set; } = default!;
    }

    // Response DTO for execution data
    public class RideDriverExecutionDto
    {
        public Guid Id { get; set; }
        public Guid RideId { get; set; }
        public Guid DriverId { get; set; }
        public bool IsPrimary { get; set; }
        
        // Driver info
        public string? DriverFirstName { get; set; }
        public string? DriverLastName { get; set; }
        public string DriverFullName => $"{DriverFirstName} {DriverLastName}";
        
        // Time & Work Fields
        public TimeSpan? ActualStartTime { get; set; }
        public TimeSpan? ActualEndTime { get; set; }
        public TimeSpan? ActualRestTime { get; set; }
        public TimeSpan? RestCalculated { get; set; }
        public TimeSpan? ContainerWaitingTime { get; set; }
        
        // Odometer readings
        public decimal? StartKilometers { get; set; }
        public decimal? EndKilometers { get; set; }
        
        public decimal? ActualKilometers { get; set; }
        public decimal? ExtraKilometers { get; set; }
        public decimal? ActualCosts { get; set; }
        public string? CostsDescription { get; set; }
        public decimal? Turnover { get; set; }
        public string? Remark { get; set; }
        public decimal CorrectionTotalHours { get; set; }
        
        // Calculated Fields
        public decimal? DecimalHours { get; set; }
        public decimal? NumberOfHours { get; set; }
        public int? PeriodNumber { get; set; }
        public int? WeekNrInPeriod { get; set; }
        public int? WeekNumber { get; set; }
        
        // Compensation Fields
        public decimal? HourlyCompensation { get; set; }
        public decimal? NightAllowance { get; set; }
        public decimal? KilometerReimbursement { get; set; }
        public decimal? ConsignmentFee { get; set; }
        public decimal? TaxFreeCompensation { get; set; }
        public decimal? VariousCompensation { get; set; }
        public decimal? StandOver { get; set; }
        public decimal? SaturdayHours { get; set; }
        public decimal? SundayHolidayHours { get; set; }
        public decimal? VacationHoursEarned { get; set; }
        public decimal? ExceedingContainerWaitingTime { get; set; }
        
        // Status & References
        public RideDriverExecutionStatus Status { get; set; }
        public Guid? HoursCodeId { get; set; }
        public string? HoursCodeName { get; set; }
        public Guid? HoursOptionId { get; set; }
        public string? HoursOptionName { get; set; }
        public Guid? CharterId { get; set; }
        
        // Audit Fields
        public DateTime? SubmittedAt { get; set; }
        public DateTime LastModifiedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedBy { get; set; }
        
        // Uploaded files (if included in response)
        public List<ExecutionFileDto>? Files { get; set; }
    }

    // Response for all executions of a ride
    public class RideExecutionsDto
    {
        public Guid RideId { get; set; }
        public string? ExecutionCompletionStatus { get; set; }
        public List<RideDriverExecutionDto> Executions { get; set; } = new();
    }
    
    // Request for rejecting execution
    public class RejectExecutionRequest
    {
        public string? Comment { get; set; }
    }
}

