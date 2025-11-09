using System.ComponentModel.DataAnnotations.Schema;

namespace TruckManagement.Entities;

public class RideDriverExecutionDisputeComment
{
    public Guid Id { get; set; }

    // Parent dispute
    public Guid DisputeId { get; set; }
    public RideDriverExecutionDispute Dispute { get; set; } = default!;

    // Author (can be driver or admin)
    public string AuthorUserId { get; set; } = default!;
    [ForeignKey(nameof(AuthorUserId))]
    public ApplicationUser Author { get; set; } = default!;

    // Comment content
    public string Body { get; set; } = default!;

    // Timestamp
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

