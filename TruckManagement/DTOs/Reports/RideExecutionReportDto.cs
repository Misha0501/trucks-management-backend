namespace TruckManagement.DTOs.Reports;

public class RideExecutionReportResponseDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? DriverId { get; set; }
    public Guid? CompanyId { get; set; }
    public string? StatusFilter { get; set; }
    public List<RideExecutionReportItemDto> Items { get; set; } = new();
    public RideExecutionReportTotalsDto Totals { get; set; } = new();
    public List<RideExecutionReportDriverSummaryDto> DriverSummaries { get; set; } = new();
}

public class RideExecutionReportItemDto
{
    public Guid ExecutionId { get; set; }
    public Guid RideId { get; set; }
    public DateTime? RideDate { get; set; }
    public Guid DriverId { get; set; }
    public string DriverFirstName { get; set; } = string.Empty;
    public string DriverLastName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? HoursCodeName { get; set; }
    public string? HoursOptionName { get; set; }
    public decimal DecimalHours { get; set; }
    public decimal CorrectionTotalHours { get; set; }
    public decimal NightAllowance { get; set; }
    public decimal KilometerReimbursement { get; set; }
    public decimal ConsignmentFee { get; set; }
    public decimal TaxFreeCompensation { get; set; }
    public decimal VariousCompensation { get; set; }
    public decimal StandOver { get; set; }
    public decimal SaturdayHours { get; set; }
    public decimal SundayHolidayHours { get; set; }
    public decimal VacationHoursEarned { get; set; }
    public decimal ActualKilometers { get; set; }
    public decimal ExtraKilometers { get; set; }
    public decimal ActualCosts { get; set; }
    public decimal Turnover { get; set; }
    public string? Remark { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public decimal TotalCompensation =>
        NightAllowance +
        KilometerReimbursement +
        ConsignmentFee +
        TaxFreeCompensation +
        VariousCompensation +
        StandOver;
}

public class RideExecutionReportTotalsDto
{
    public int TotalExecutions { get; set; }
    public decimal TotalHours { get; set; }
    public decimal TotalCorrectedHours { get; set; }
    public decimal TotalNightAllowance { get; set; }
    public decimal TotalKilometerReimbursement { get; set; }
    public decimal TotalConsignmentFee { get; set; }
    public decimal TotalTaxFreeCompensation { get; set; }
    public decimal TotalVariousCompensation { get; set; }
    public decimal TotalStandOver { get; set; }
    public decimal TotalSaturdayHours { get; set; }
    public decimal TotalSundayHolidayHours { get; set; }
    public decimal TotalVacationHoursEarned { get; set; }
    public decimal TotalKilometers { get; set; }
    public decimal TotalExtraKilometers { get; set; }
    public decimal TotalActualCosts { get; set; }
    public decimal TotalTurnover { get; set; }
    public decimal TotalCompensation =>
        TotalNightAllowance +
        TotalKilometerReimbursement +
        TotalConsignmentFee +
        TotalTaxFreeCompensation +
        TotalVariousCompensation +
        TotalStandOver;
}

public class RideExecutionReportDriverSummaryDto
{
    public Guid DriverId { get; set; }
    public string DriverFirstName { get; set; } = string.Empty;
    public string DriverLastName { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public decimal TotalCompensation { get; set; }
    public decimal TotalKilometers { get; set; }
    public int ExecutionCount { get; set; }
}

