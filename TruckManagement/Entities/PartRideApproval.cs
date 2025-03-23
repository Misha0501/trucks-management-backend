using System.ComponentModel.DataAnnotations.Schema;
using TruckManagement.Enums;

namespace TruckManagement.Entities
{
    public class PartRideApproval
    {
        public Guid Id { get; set; }

        // Link to PartRide
        public Guid PartRideId { get; set; }
        public PartRide PartRide { get; set; } = default!;

        // Link to a .NET Identity role (AspNetRoles)
        public string RoleId { get; set; } = default!;
        public ApplicationRole Role { get; set; } = default!;

        // The user who actually approved
        // If you store user IDs as string, do string? ApprovedByUserId
        public string? ApprovedByUserId { get; set; }
        
        [ForeignKey(nameof(ApprovedByUserId))]
        public ApplicationUser? ApprovedByUser { get; set; }

        // The current approval status
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

        // Optional comments or reason for changes
        public string? Comments { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}