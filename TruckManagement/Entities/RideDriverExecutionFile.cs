using System.ComponentModel.DataAnnotations;

namespace TruckManagement.Entities
{
    public class RideDriverExecutionFile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid RideDriverExecutionId { get; set; }
        public RideDriverExecution RideDriverExecution { get; set; } = default!;
        
        [Required]
        public string FileName { get; set; } = default!;
        
        public long FileSize { get; set; }
        
        [Required]
        public string ContentType { get; set; } = default!;
        
        [Required]
        public byte[] FileData { get; set; } = default!;
        
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        
        public string? UploadedBy { get; set; }
    }
}


