using TruckManagement.Data;
using TruckManagement.Entities;
using Microsoft.AspNetCore.StaticFiles;

namespace TruckManagement.Helpers;

public static class FileUploadHelper
{
    public static void MoveUploadsToPartRide(
        Guid partRideId,
        IEnumerable<Guid> uploadIds,
        string tmpRoot,
        string finalRoot,
        ApplicationDbContext db)
    {
        Directory.CreateDirectory(finalRoot);

        foreach (var id in uploadIds.Distinct())
        {
            var tmpFile = Directory.EnumerateFiles(tmpRoot, $"{id}.*").FirstOrDefault();
            if (tmpFile is null) continue;

            var ext = Path.GetExtension(tmpFile);
            var dest = Path.Combine(finalRoot, $"{id}{ext}");
            File.Move(tmpFile, dest);

            var provider = new FileExtensionContentTypeProvider();
            provider.TryGetContentType(dest, out var contentType);

            db.PartRideFiles.Add(new PartRideFile
            {
                Id = Guid.NewGuid(),
                PartRideId = partRideId,
                FilePath = dest,
                FileName = Path.GetFileName(tmpFile),
                ContentType = contentType ?? "application/octet-stream",
                UploadedAt = DateTime.UtcNow
            });
        }
    }
}