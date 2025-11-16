using System.ComponentModel.DataAnnotations;
using TruckManagement.Enums;

namespace TruckManagement.Entities
{
    public class RideDriverExecution
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid RideId { get; set; }
        public Ride Ride { get; set; } = default!;
        
        [Required]
        public Guid DriverId { get; set; }
        public Driver Driver { get; set; } = default!;
        
        public bool IsPrimary { get; set; }
        
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
        public decimal CorrectionTotalHours { get; set; } = 0;
        
        // Calculated Fields (auto-calculated per driver)
        public decimal? DecimalHours { get; set; }
        public decimal? NumberOfHours { get; set; }
        public int? PeriodNumber { get; set; }
        public int? WeekNrInPeriod { get; set; }
        public int? WeekNumber { get; set; }
        
        // Compensation Fields (auto-calculated per driver)
        public decimal? NightAllowance { get; set; }
        public decimal? KilometerReimbursement { get; set; }
        public decimal? ConsignmentFee { get; set; }
        public decimal? TaxFreeCompensation { get; set; }
        public decimal? VariousCompensation { get; set; }
        public decimal? StandOver { get; set; }
        public decimal? SaturdayHours { get; set; }
        public decimal? SundayHolidayHours { get; set; }
        public decimal? VacationHoursEarned { get; set; }
        
        /// <summary>
        /// Base hourly compensation: DecimalHours * DriverRatePerHour
        /// </summary>
        public decimal? HourlyCompensation { get; set; }
        
        /// <summary>
        /// Container waiting time exceeding 2 hours: max(0, ContainerWaitingTime - 2)
        /// Stored in hours as decimal
        /// </summary>
        public decimal? ExceedingContainerWaitingTime { get; set; }
        
        // Status & References
        public RideDriverExecutionStatus Status { get; set; } = RideDriverExecutionStatus.Pending;
        
        public Guid? HoursCodeId { get; set; }
        public HoursCode? HoursCode { get; set; }
        
        public Guid? HoursOptionId { get; set; }
        public HoursOption? HoursOption { get; set; }
        
        public Guid? CharterId { get; set; }
        public Charter? Charter { get; set; }
        
        // Audit Fields
        public DateTime? SubmittedAt { get; set; }
        public string? SubmittedBy { get; set; }
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
        public string? LastModifiedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedBy { get; set; }
        
        // Navigation property for files
        public ICollection<RideDriverExecutionFile> Files { get; set; } = new List<RideDriverExecutionFile>();
        
        // Navigation property for comments
        public ICollection<RideDriverExecutionComment> Comments { get; set; } = new List<RideDriverExecutionComment>();
        
        // Navigation property for disputes
        public ICollection<RideDriverExecutionDispute> Disputes { get; set; } = new List<RideDriverExecutionDispute>();
    }
}

