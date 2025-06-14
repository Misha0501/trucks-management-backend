using TruckManagement.Data;
using TruckManagement.Entities;

namespace TruckManagement.Helpers;

public static class PartRideFileDeleteHelper
{
    public static void DeletePartRideFiles(
        Guid partRideId,
        List<Guid> fileIdsToDelete,
        string basePathCompanies,
        ApplicationDbContext db)
    {
        var filesToDelete = db.PartRideFiles
            .Where(f => f.PartRideId == partRideId && fileIdsToDelete.Contains(f.Id))
            .ToList();

        foreach (var file in filesToDelete)
        {
            var absolutePath = Path.Combine(basePathCompanies, file.FilePath);
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            db.PartRideFiles.Remove(file);
        }
    }
}