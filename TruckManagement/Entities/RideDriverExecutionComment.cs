using System.ComponentModel.DataAnnotations;

namespace TruckManagement.Entities
{
    public class RideDriverExecutionComment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid RideDriverExecutionId { get; set; }
        public RideDriverExecution RideDriverExecution { get; set; } = default!;
        
        [Required]
        public string UserId { get; set; } = default!;
        public ApplicationUser User { get; set; } = default!;
        
        [Required]
        public string Comment { get; set; } = default!;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

