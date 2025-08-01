using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Helpers;

namespace TruckManagement.Endpoints;

public static class DriverFilesRoutes
{
    public static void MapDriverFilesEndpoints(this WebApplication app)
    {
        // -----------------------------------------------------------
        // GET /driver-files/{fileId}  (download a file)
        // -----------------------------------------------------------
        app.MapGet("/driver-files/{fileId}",
            [Authorize(Roles = "globalAdmin, customerAdmin, employer, customer, customerAccountant, driver")]
            async (
                string fileId,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser,
                IWebHostEnvironment env,
                IConfiguration config) =>
            {
                // 1. Validate GUID
                if (!Guid.TryParse(fileId, out Guid fileGuid))
                    return ApiResponseFactory.Error("Invalid file ID format.", StatusCodes.Status400BadRequest);

                // 2. Load file with related Driver & Company
                var file = await db.DriverFiles
                    .Include(f => f.Driver!)
                    .ThenInclude(d => d.Company)
                    .FirstOrDefaultAsync(f => f.Id == fileGuid);

                if (file == null)
                    return ApiResponseFactory.Error("File not found.", StatusCodes.Status404NotFound);

                var driver = file.Driver!;
                var companyId = driver.CompanyId ?? Guid.Empty;

                // 3. Authorization checks
                var userId = userManager.GetUserId(currentUser) ?? string.Empty;
                if (string.IsNullOrEmpty(userId))
                    return ApiResponseFactory.Error("User not authenticated.", StatusCodes.Status401Unauthorized);

                bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                bool isDriver = currentUser.IsInRole("driver");

                if (!isGlobalAdmin)
                {
                    // Load user's company associations
                    List<Guid> associatedCompanyIds = new List<Guid>();

                    if (isDriver)
                    {
                        // For drivers: only allow access to files from their own company AND their own files
                        var currentDriver = await db.Drivers.FirstOrDefaultAsync(d => d.AspNetUserId == userId);
                        if (currentDriver?.CompanyId.HasValue == true)
                        {
                            associatedCompanyIds.Add(currentDriver.CompanyId.Value);
                            
                            // Additional check: drivers can only access their own files
                            if (currentDriver.Id != driver.Id)
                                return ApiResponseFactory.Error(
                                    "You can only access your own driver files.",
                                    StatusCodes.Status403Forbidden);
                        }
                    }
                    else
                    {
                        // For other roles: check contact person associations
                        var contactPerson = await db.ContactPersons
                            .Include(cp => cp.ContactPersonClientCompanies)
                            .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                        if (contactPerson != null)
                        {
                            associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Where(cpc => cpc.CompanyId.HasValue)
                                .Select(cpc => cpc.CompanyId!.Value)
                                .Distinct()
                                .ToList();
                        }
                    }

                    // Check if user has access to this company
                    if (!associatedCompanyIds.Contains(companyId))
                        return ApiResponseFactory.Error(
                            "You are not authorized to download files for this company.",
                            StatusCodes.Status403Forbidden);
                }

                // 4. Build absolute path & verify existence
                var storageBasePath = config.GetValue<string>("Storage:BasePath") ?? env.ContentRootPath;
                var absolutePath = Path.Combine(storageBasePath, file.FilePath);
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