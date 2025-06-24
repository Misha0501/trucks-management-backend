using System.ComponentModel.DataAnnotations.Schema;

namespace TruckManagement.Entities;

public class PartRideDisputeComment
{
    public Guid Id { get; set; }

    /* parent dispute */
    public Guid DisputeId { get; set; }
    public PartRideDispute Dispute { get; set; } = default!;

    /* author */
    public string AuthorUserId { get; set; }
    [ForeignKey(nameof(AuthorUserId))]
    public ApplicationUser Author { get; set; } = default!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Body { get; set; } = default!;
}