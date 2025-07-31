using TruckManagement.Data;
using TruckManagement.Entities;

namespace TruckManagement.Helpers;

public static class DriverFileDeleteHelper
{
    public static void DeleteDriverFiles(
        Guid driverId,
        List<Guid> fileIdsToDelete,
        string basePathCompanies,
        ApplicationDbContext db)
    {
        var filesToDelete = db.DriverFiles
            .Where(f => f.DriverId == driverId && fileIdsToDelete.Contains(f.Id))
            .ToList();

        foreach (var file in filesToDelete)
        {
            var absolutePath = Path.Combine(basePathCompanies, file.FilePath);
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            db.DriverFiles.Remove(file);
        }
    }
} 