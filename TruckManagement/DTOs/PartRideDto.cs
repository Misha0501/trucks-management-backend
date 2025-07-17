namespace TruckManagement.DTOs;

public class PartRideDto
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public TimeSpan Rest { get; set; }
    public TimeSpan? RestCalculated { get; set; }
    public double? TotalKilometers { get; set; }
    public double? ExtraKilometers { get; set; }
    public decimal? Costs { get; set; }
    public int? WeekNumber { get; set; }
    public double? DecimalHours { get; set; }
    public string? CostsDescription { get; set; }
    public decimal? Turnover { get; set; }
    public string? Remark { get; set; }
    public double CorrectionTotalHours { get; set; }
    public double? NumberOfHours { get; set; }
    public double TaxFreeCompensation { get; set; }
    public double StandOver { get; set; }
    public double NightAllowance { get; set; }
    public double KilometerReimbursement { get; set; }
    public double ConsignmentFee { get; set; }
    public double SaturdayHours { get; set; }
    public double SundayHolidayHours { get; set; }
    public double VariousCompensation { get; set; }
    public PartRideStatus Status { get; set; }

    // Optional: Summary nested Driver info if needed
    public DriverDto? Driver { get; set; }
}