using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Helpers;
// Where ApiResponseFactory is assumed to exist

namespace TruckManagement.Endpoints;

public static class PartRideFilesRoutes
{
    public static void MapPartRideFilesEndpoints(this WebApplication app)
    {
        // -----------------------------------------------------------
        // GET /partride-files/{fileId}  (download a file)
        // -----------------------------------------------------------
        app.MapGet("/partride-files/{fileId}",
            [Authorize(Roles = "globalAdmin, customerAdmin, employer, customer, customerAccountant, driver")]
            async (
                string fileId,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser,
                IWebHostEnvironment env) =>
            {
                // 1. Validate GUID
                if (!Guid.TryParse(fileId, out Guid fileGuid))
                    return ApiResponseFactory.Error("Invalid file ID format.", StatusCodes.Status400BadRequest);

                // 2. Load file with related PartRide & Company
                var file = await db.PartRideFiles
                    .Include(f => f.PartRide!)
                    .ThenInclude(pr => pr.Company)
                    .FirstOrDefaultAsync(f => f.Id == fileGuid);

                if (file == null)
                    return ApiResponseFactory.Error("File not found.", StatusCodes.Status404NotFound);

                var partRide = file.PartRide!;
                var companyId = partRide.CompanyId;

                // 3. Authorization checks
                var userId = userManager.GetUserId(currentUser) ?? string.Empty;
                if (string.IsNullOrEmpty(userId))
                    return ApiResponseFactory.Error("User not authenticated.", StatusCodes.Status401Unauthorized);

                bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                bool isDriver = currentUser.IsInRole("driver");

                if (!isGlobalAdmin)
                {
                    if (isDriver)
                    {
                        // Driver must own the PartRide
                        var driverEntity = await db.Drivers
                            .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);

                        if (driverEntity == null ||
                            !partRide.DriverId.HasValue ||
                            partRide.DriverId.Value != driverEntity.Id)
                        {
                            return ApiResponseFactory.Error(
                                "Drivers can only download files from their own PartRides.",
                                StatusCodes.Status403Forbidden);
                        }
                    }
                    else
                    {
                        // Contact-person roles must be linked to the company
                        var contactPerson = await db.ContactPersons
                            .Include(cp => cp.ContactPersonClientCompanies)
                            .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId && !cp.IsDeleted);

                        if (contactPerson == null)
                            return ApiResponseFactory.Error(
                                "No contact person profile found. You are not authorized.",
                                StatusCodes.Status403Forbidden);

                        var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                            .Select(c => c.CompanyId)
                            .Where(id => id.HasValue)
                            .Select(id => id!.Value)
                            .ToList();

                        if (!companyId.HasValue || !associatedCompanyIds.Contains(companyId.Value))
                            return ApiResponseFactory.Error(
                                "You are not authorized to download files for this company.",
                                StatusCodes.Status403Forbidden);
                    }
                }

                // 4. Build absolute path & verify existence
                var absolutePath = Path.Combine(env.ContentRootPath, file.FilePath);
                if (!File.Exists(absolutePath))
                    return ApiResponseFactory.Error("File missing on server.", StatusCodes.Status410Gone);

                // 5. Serve the file
                var contentTypeProvider = new FileExtensionContentTypeProvider();
                if (!contentTypeProvider.TryGetContentType(absolutePath, out var contentType))
                    contentType = file.ContentType ?? "application/octet-stream";

                var fileName = Path.GetFileName(file.FileName ?? absolutePath);

                return Results.File(File.OpenRead(absolutePath), contentType, fileName);
            });
    }
}