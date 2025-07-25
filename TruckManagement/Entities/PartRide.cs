public enum PartRideStatus
{
    PendingAdmin,
    Dispute,
    Accepted,
    Rejected
}

namespace TruckManagement.Entities
{
    public class PartRide
    {
        public Guid Id { get; set; }

        public Guid? RideId { get; set; }
        public Ride? Ride { get; set; } = default!;

        public DateTime Date { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public TimeSpan Rest { get; set; }
        public TimeSpan? RestCalculated { get; set; }
        public double? TotalKilometers { get; set; }
        public double? ExtraKilometers { get; set; }

        public Guid? CarId { get; set; }
        public Car? Car { get; set; } = default!;

        public Guid? DriverId { get; set; }
        public Driver? Driver { get; set; } = default!;

        public decimal? Costs { get; set; }
        
        public Guid? ClientId { get; set; }
        public Client? Client { get; set; } = default!;

        public int? WeekNumber { get; set; }
        public int? PeriodNumber { get; set; }
        public int? WeekNrInPeriod { get; set; }
        public double? DecimalHours { get; set; }
        public string? CostsDescription { get; set; }
        public decimal? Turnover { get; set; }
        public string? Remark { get; set; }

        public Guid? CompanyId { get; set; }
        public Company? Company { get; set; } = default!;

        public Guid? CharterId { get; set; }
        public Charter? Charter { get; set; } = default!;
        public double CorrectionTotalHours { get; set; }
        public double? NumberOfHours { get; set; }
        public double StandOver { get; set; } 
        public double? VacationHours { get; set; }

        // Compensations
        public double NightAllowance { get; set; }
        public double KilometerReimbursement { get; set; }
        public double ConsignmentFee { get; set; }
        public double TaxFreeCompensation { get; set; }
        public double VariousCompensation { get; set; }
        // Compensations end
        public double SaturdayHours { get; set; }
        public double SundayHolidayHours { get; set; }
        // HoursOption reference
        public Guid? HoursOptionId { get; set; }
        public HoursOption? HoursOption { get; set; }

        // HoursCode reference
        public Guid? HoursCodeId { get; set; }
        public HoursCode? HoursCode { get; set; }
        public ICollection<PartRideFile> Files { get; set; } = new List<PartRideFile>();
        
        // Navigation to approvals and comments
        public ICollection<PartRideApproval> Approvals { get; set; } = new List<PartRideApproval>();
        public ICollection<PartRideComment> Comments { get; set; } = new List<PartRideComment>();
        
        public Guid? WeekApprovalId { get; set; }
        public WeekApproval? WeekApproval { get; set; }
        public ICollection<PartRideDispute> PartRideDisputes { get; set; } = new List<PartRideDispute>();

        public PartRideStatus Status { get; set; } = PartRideStatus.PendingAdmin;
    }
}