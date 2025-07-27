namespace TruckManagement.Entities;

public class DriverFile
{
    public Guid Id { get; set; }

    public Guid? DriverId { get; set; }
    public Driver? Driver { get; set; }

    public string FileName { get; set; } = default!;
    public string OriginalFileName { get; set; } = default!;
    public string FilePath { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public DateTime UploadedAt { get; set; }
} 