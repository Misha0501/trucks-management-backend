using System.ComponentModel.DataAnnotations.Schema;

namespace TruckManagement.Entities;

public enum DisputeStatus
{
    Open,              // waiting for a reply
    AcceptedByDriver,  // driver accepted the admin’s correction
    AcceptedByAdmin,   // admin accepted the driver’s counter-argument
    Closed             // admin closed it without agreement
}

public class PartRideDispute
{
    public Guid Id           { get; set; }

    /* ─── FK to the ride that is being disputed ───────────────────────── */
    public Guid PartRideId   { get; set; }
    public PartRide PartRide { get; set; } = default!;

    /* ─── Who opened it (always an admin / contact person) ────────────── */
    public string OpenedById   { get; set; } = default!;
    [ForeignKey(nameof(OpenedById))]
    public ApplicationUser OpenedBy { get; set; } = default!;   // convenience

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAtUtc { get; set; }

    /* ─── Proposed correction in decimal hours (+ / –) ────────────────── */
    public double Correction { get; set; }

    public DisputeStatus Status { get; set; } = DisputeStatus.Open;

    /* ─── Thread of messages (“ping-pong”) ────────────────────────────── */
    public ICollection<PartRideDisputeComment> Comments { get; } = new List<PartRideDisputeComment>();
}