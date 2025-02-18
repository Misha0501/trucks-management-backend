namespace TruckManagement.Entities
{
    public class PartRideComment
    {
        public Guid Id { get; set; }

        // Link to PartRide
        public Guid PartRideId { get; set; }
        public PartRide PartRide { get; set; } = default!;

        // The user who posted the comment
        public string AuthorUserId { get; set; } = default!;
        
        // Possibly store the role or name of the user
        public string? AuthorRoleId { get; set; }
        public ApplicationRole? AuthorRole { get; set; }

        // The actual comment text
        public string Comment { get; set; } = default!;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}