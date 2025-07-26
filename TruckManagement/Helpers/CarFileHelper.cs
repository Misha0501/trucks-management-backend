using TruckManagement.Data;
using TruckManagement.Entities;
using Microsoft.AspNetCore.StaticFiles;
using TruckManagement.DTOs;
namespace TruckManagement.Helpers;

public static class CarFileHelper
{
    public static void MoveUploadsToCar(
        Guid carId,
        Guid? companyId,
        IEnumerable<UploadFileRequest> uploads,
        string tmpRoot,
        string basePathCompanies, // e.g., "Storage/Companies"
        ApplicationDbContext db,
        IResourceLocalizer resourceLocalizer
        )
    {
        var safeCompanyId = companyId.HasValue && companyId.Value != Guid.Empty
            ? companyId.Value.ToString()
            : "Uncategorized";

        var relativeFolderPath = Path.Combine("Companies", safeCompanyId, "Cars", carId.ToString());
        var absoluteFolderPath = Path.Combine(basePathCompanies, safeCompanyId, "Cars", carId.ToString());
        Directory.CreateDirectory(absoluteFolderPath);

        foreach (var upload in uploads.DistinctBy(u => u.FileId))
        {
            var tmpFile = Directory.EnumerateFiles(tmpRoot, $"{upload.FileId}.*").FirstOrDefault();
            if (tmpFile is null)
                throw new InvalidOperationException(resourceLocalizer.Localize("TempFileNotFound", upload.FileId));
            
            var ext = Path.GetExtension(tmpFile);
            var relativePath = Path.Combine(relativeFolderPath, $"{upload.FileId}{ext}");
            var absolutePath = Path.Combine(absoluteFolderPath, $"{upload.FileId}{ext}");

            File.Move(tmpFile, absolutePath);

            var provider = new FileExtensionContentTypeProvider();
            provider.TryGetContentType(absolutePath, out var contentType);

            db.CarFiles.Add(new CarFile
            {
                Id = Guid.NewGuid(),
                CarId = carId,
                FilePath = relativePath, // ✅ Store relative path
                FileName = Path.GetFileName(tmpFile),
                OriginalFileName = upload.OriginalFileName,
                ContentType = contentType ?? "application/octet-stream",
                UploadedAt = DateTime.UtcNow
            });
        }
    }
} 