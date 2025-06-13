using TruckManagement.Data;
using TruckManagement.Entities;
using Microsoft.AspNetCore.StaticFiles;

namespace TruckManagement.Helpers;

public static class FileUploadHelper
{
    public static void MoveUploadsToPartRide(
        Guid partRideId,
        Guid? companyId,
        IEnumerable<Guid> uploadIds,
        string tmpRoot,
        string basePathCompanies, // e.g., "Storage/Companies"
        ApplicationDbContext db)
    {
        var safeCompanyId = companyId.HasValue && companyId.Value != Guid.Empty
            ? companyId.Value.ToString()
            : "Uncategorized";

        var relativeFolderPath = Path.Combine("Storage", "Companies", safeCompanyId, "WorkDayReceipts", partRideId.ToString());
        var absoluteFolderPath = Path.Combine(basePathCompanies, safeCompanyId, "WorkDayReceipts", partRideId.ToString());
        Directory.CreateDirectory(absoluteFolderPath);

        foreach (var id in uploadIds.Distinct())
        {
            var tmpFile = Directory.EnumerateFiles(tmpRoot, $"{id}.*").FirstOrDefault();
            if (tmpFile is null)
                throw new InvalidOperationException($"Some files were not found in temp storage for ID: {id}");

            var ext = Path.GetExtension(tmpFile);
            var relativePath = Path.Combine(relativeFolderPath, $"{id}{ext}");
            var absolutePath = Path.Combine(absoluteFolderPath, $"{id}{ext}");

            File.Move(tmpFile, absolutePath);

            var provider = new FileExtensionContentTypeProvider();
            provider.TryGetContentType(absolutePath, out var contentType);

            db.PartRideFiles.Add(new PartRideFile
            {
                Id = Guid.NewGuid(),
                PartRideId = partRideId,
                FilePath = relativePath, // ✅ Store relative path
                FileName = Path.GetFileName(tmpFile),
                ContentType = contentType ?? "application/octet-stream",
                UploadedAt = DateTime.UtcNow
            });
        }
    }
}