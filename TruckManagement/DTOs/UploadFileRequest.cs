namespace TruckManagement.DTOs;

public class UploadFileRequest
{
    public Guid FileId { get; set; }
    public string OriginalFileName { get; set; } = default!;
}
