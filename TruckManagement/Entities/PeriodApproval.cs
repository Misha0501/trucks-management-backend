namespace TruckManagement.Entities;

public enum PeriodApprovalStatus
{
    PendingDriver,   // waiting for driver
    PendingAdmin,    // driver signed, waiting for customer-admin
    Signed,          // both signed
    Invalidated      // a ride changed after signatures
}

public class PeriodApproval
{
    public Guid Id { get; set; }

    // key
    public Guid DriverId { get; set; }
    public int  Year     { get; set; }
    public int  PeriodNr { get; set; }

    // driver signature
    public DateTime? DriverSignedAt  { get; set; }
    public string?   DriverSignedIp  { get; set; }
    public string?   DriverSignedUa  { get; set; }
    public string?   DriverPdfPath   { get; set; }

    // customer-admin signature
    public Guid?     AdminUserId     { get; set; }            
    public DateTime? AdminSignedAt   { get; set; }
    public string?   AdminSignedIp   { get; set; }
    public string?   AdminSignedUa   { get; set; }
    public string?   AdminPdfPath    { get; set; }

    public PeriodApprovalStatus Status { get; set; } = PeriodApprovalStatus.PendingAdmin;

    public ICollection<PartRide> PartRides { get; set; } = new List<PartRide>();
}