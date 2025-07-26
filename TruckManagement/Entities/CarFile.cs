namespace TruckManagement.Entities;

public class CarFile
{
    public Guid Id { get; set; }

    public Guid? CarId { get; set; }
    public Car? Car { get; set; }

    public string FileName { get; set; } = default!;
    public string OriginalFileName { get; set; } = default!;
    public string FilePath { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public DateTime UploadedAt { get; set; }
} 