namespace TruckManagement.Entities;

public class PartRideFile
{
    public Guid Id { get; set; }

    public Guid? PartRideId { get; set; }
    public PartRide? PartRide { get; set; }

    public string FileName { get; set; } = default!;
    public string FilePath { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public DateTime UploadedAt { get; set; }
}