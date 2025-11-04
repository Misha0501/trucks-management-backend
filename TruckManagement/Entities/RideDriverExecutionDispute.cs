using System.ComponentModel.DataAnnotations.Schema;

namespace TruckManagement.Entities;

public enum RideExecutionDisputeStatus
{
    Open = 0,        // Driver created dispute, waiting for admin response
    Resolved = 1,    // Admin resolved the dispute
    Closed = 2       // Dispute closed (by admin or system)
}

public class RideDriverExecutionDispute
{
    public Guid Id { get; set; }

    // FK to the ride execution being disputed
    public Guid RideDriverExecutionId { get; set; }
    public RideDriverExecution RideDriverExecution { get; set; } = default!;

    // Who opened it (the driver who submitted the execution)
    public Guid DriverId { get; set; }
    public Driver Driver { get; set; } = default!;

    // Reason for dispute
    public string Reason { get; set; } = default!;

    // Status
    public RideExecutionDisputeStatus Status { get; set; } = RideExecutionDisputeStatus.Open;

    // Timestamps
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }

    // Who resolved it (admin)
    public string? ResolvedById { get; set; }
    [ForeignKey(nameof(ResolvedById))]
    public ApplicationUser? ResolvedBy { get; set; }

    // Resolution notes from admin
    public string? ResolutionNotes { get; set; }

    // Comments thread
    public ICollection<RideDriverExecutionDisputeComment> Comments { get; set; } = new List<RideDriverExecutionDisputeComment>();
}

