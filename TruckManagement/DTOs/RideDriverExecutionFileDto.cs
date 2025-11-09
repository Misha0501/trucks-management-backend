namespace TruckManagement.DTOs
{
    public class ExecutionFileDto
    {
        public Guid Id { get; set; }
        public Guid RideDriverExecutionId { get; set; }
        public string FileName { get; set; } = default!;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = default!;
        public DateTime UploadedAt { get; set; }
        public string? UploadedBy { get; set; }
    }

    public class UploadExecutionFileRequest
    {
        public string FileName { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public string FileDataBase64 { get; set; } = default!; // Base64 encoded file data
    }
}


