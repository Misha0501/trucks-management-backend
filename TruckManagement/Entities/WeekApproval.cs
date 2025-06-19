namespace TruckManagement.Entities;


public enum WeekApprovalStatus
{
    PendingAdmin,   // Admin still has to allow the week
    PendingDriver,  // Driver can sign now
    Signed,         // Driver has signed
    Invalidated     // A ride changed after signature
}

public class WeekApproval
{
    public Guid Id           { get; set; }

    // Composite “natural key”
    public Guid DriverId     { get; set; }
    public int  Year         { get; set; }   // ISO-year
    public int  WeekNr       { get; set; }   // ISO week (1-53)
    public int  PeriodNr     { get; set; }   // 1-13  (pre-calculated ⇒ easy grouping)

    /* --- Workflow fields --- */
    public WeekApprovalStatus Status          { get; set; } = WeekApprovalStatus.PendingAdmin;

    public Guid?     AdminUserId    { get; set; }
    public DateTime? AdminAllowedAt { get; set; } // ⬅︎ “when the driver was allowed to sign”
    public DateTime? DriverSignedAt { get; set; }

    /* --- Navigation --- */
    public ICollection<PartRide> PartRides { get; set; } = new List<PartRide>();
}
