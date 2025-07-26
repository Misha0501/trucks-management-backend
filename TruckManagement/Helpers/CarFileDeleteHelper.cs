using TruckManagement.Data;
using TruckManagement.Entities;

namespace TruckManagement.Helpers;

public static class CarFileDeleteHelper
{
    public static void DeleteCarFiles(
        Guid carId,
        List<Guid> fileIdsToDelete,
        string basePathCompanies,
        ApplicationDbContext db)
    {
        var filesToDelete = db.CarFiles
            .Where(f => f.CarId == carId && fileIdsToDelete.Contains(f.Id))
            .ToList();

        foreach (var file in filesToDelete)
        {
            var absolutePath = Path.Combine(basePathCompanies, file.FilePath);
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            db.CarFiles.Remove(file);
        }
    }
} 