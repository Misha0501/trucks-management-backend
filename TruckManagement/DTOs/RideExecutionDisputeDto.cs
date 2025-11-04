namespace TruckManagement.DTOs;

// Request to create a dispute
public class CreateRideExecutionDisputeRequest
{
    public string Reason { get; set; } = default!;
}

// Request to close/resolve a dispute
public class CloseRideExecutionDisputeRequest
{
    public string? ResolutionNotes { get; set; }
}

// Request to add a comment to a dispute
public class AddDisputeCommentRequest
{
    public string Body { get; set; } = default!;
}

// Dispute comment DTO
public class RideExecutionDisputeCommentDto
{
    public Guid Id { get; set; }
    public Guid DisputeId { get; set; }
    public string AuthorUserId { get; set; } = default!;
    public string AuthorFirstName { get; set; } = default!;
    public string AuthorLastName { get; set; } = default!;
    public string Body { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
}

// Full dispute DTO
public class RideExecutionDisputeDto
{
    public Guid Id { get; set; }
    public Guid RideDriverExecutionId { get; set; }
    public Guid DriverId { get; set; }
    public string DriverFirstName { get; set; } = default!;
    public string DriverLastName { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public string Status { get; set; } = default!; // "Open", "Resolved", "Closed"
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public string? ResolvedById { get; set; }
    public string? ResolvedByName { get; set; }
    public string? ResolutionNotes { get; set; }
    public List<RideExecutionDisputeCommentDto> Comments { get; set; } = new();
}

