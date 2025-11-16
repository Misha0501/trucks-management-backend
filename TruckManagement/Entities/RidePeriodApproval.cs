namespace TruckManagement.Entities;

public enum RidePeriodApprovalStatus
{
    NotReady = 0,      // Not all weeks are signed
    ReadyToSign = 1,   // All 4 weeks signed, period not signed
    Signed = 2,        // Period has been signed by driver
    Invalidated = 3    // Changes made after signing
}

public class RidePeriodApproval
{
    public Guid Id { get; set; }

    // Composite key
    public Guid DriverId { get; set; }
    public Driver Driver { get; set; } = default!;
    public int Year { get; set; }
    public int PeriodNr { get; set; }

    // Period dates
    public DateTime FromDate { get; set; }  // Monday of first week
    public DateTime ToDate { get; set; }    // Sunday of last week

    // Driver signature
    public DateTime? DriverSignedAt { get; set; }
    public string? DriverSignatureData { get; set; }  // Base64 signature image
    public string? DriverSignedIp { get; set; }
    public string? DriverSignedUserAgent { get; set; }
    public string? DriverPdfPath { get; set; }

    // Status
    public RidePeriodApprovalStatus Status { get; set; } = RidePeriodApprovalStatus.NotReady;

    // Calculated totals (cached for performance)
    public decimal? TotalHours { get; set; }
    public decimal? TotalCompensation { get; set; }
}


