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
        public TimeSpan? Rest { get; set; }
        public double? Kilometers { get; set; }

        public Guid? CarId { get; set; }
        public Car? Car { get; set; } = default!;

        public Guid? DriverId { get; set; }
        public Driver? Driver { get; set; } = default!;

        public decimal? Costs { get; set; }
        
        public Guid? ClientId { get; set; }
        public Client? Client { get; set; } = default!;

        public int? WeekNumber { get; set; }
        public double? DecimalHours { get; set; }

        public Guid? UnitId { get; set; }
        public Unit? Unit { get; set; } = default!;

        public Guid? RateId { get; set; }
        public Rate? Rate { get; set; } = default!;

        public string? CostsDescription { get; set; }

        public Guid? SurchargeId { get; set; }
        public Surcharge? Surcharge { get; set; } = default!;

        public decimal? Turnover { get; set; }
        public string? Remark { get; set; }

        public Guid? CompanyId { get; set; }
        public Company? Company { get; set; } = default!;

        public Guid? CharterId { get; set; }
        public Charter? Charter { get; set; } = default!;
        public double CorrectionTotalHours { get; set; }
        public double TaxFreeCompensation { get; set; }
        public double StandOver { get; set; }
        public double NightAllowance { get; set; }
        public double KilometerReimbursement { get; set; }
        public double ExtraKilometers { get; set; }
        public double ConsignmentFee { get; set; }
        public double SaturdayHours { get; set; }
        public double SundayHolidayHours { get; set; }
        public double VariousCompensation { get; set; }
        // HoursOption reference
        public Guid? HoursOptionId { get; set; }
        public HoursOption? HoursOption { get; set; }

        // HoursCode reference
        public Guid? HoursCodeId { get; set; }
        public HoursCode? HoursCode { get; set; }
        
        // Navigation to approvals and comments
        public ICollection<PartRideApproval> Approvals { get; set; } = new List<PartRideApproval>();
        public ICollection<PartRideComment> Comments { get; set; } = new List<PartRideComment>();
    }
}