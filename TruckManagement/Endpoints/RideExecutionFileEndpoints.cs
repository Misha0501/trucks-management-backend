using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;
using TruckManagement.Helpers;

namespace TruckManagement.Endpoints
{
    public static class RideExecutionFileEndpoints
    {
        public static void MapRideExecutionFileEndpoints(this WebApplication app)
        {
            // POST /rides/{id}/my-execution/files - Driver uploads file to their execution
            app.MapPost("/rides/{id}/my-execution/files",
                [Authorize(Roles = "driver")]
                async (
                    Guid id,
                    [FromBody] UploadExecutionFileRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var driver = await db.Drivers
                            .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);

                        if (driver == null)
                        {
                            return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status404NotFound);
                        }

                        // Get driver's execution for this ride
                        var execution = await db.RideDriverExecutions
                            .FirstOrDefaultAsync(e => e.RideId == id && e.DriverId == driver.Id);

                        if (execution == null)
                        {
                            return ApiResponseFactory.Error("Execution not found. Please submit execution data first.", StatusCodes.Status404NotFound);
                        }

                        // Decode base64 file data
                        byte[] fileData;
                        try
                        {
                            fileData = Convert.FromBase64String(request.FileDataBase64);
                        }
                        catch
                        {
                            return ApiResponseFactory.Error("Invalid file data format.", StatusCodes.Status400BadRequest);
                        }

                        // Create file entity
                        var file = new RideDriverExecutionFile
                        {
                            RideDriverExecutionId = execution.Id,
                            FileName = request.FileName,
                            FileSize = fileData.Length,
                            ContentType = request.ContentType,
                            FileData = fileData,
                            UploadedBy = userId
                        };

                        db.RideDriverExecutionFiles.Add(file);
                        await db.SaveChangesAsync();

                        var response = new ExecutionFileDto
                        {
                            Id = file.Id,
                            RideDriverExecutionId = file.RideDriverExecutionId,
                            FileName = file.FileName,
                            FileSize = file.FileSize,
                            ContentType = file.ContentType,
                            UploadedAt = file.UploadedAt,
                            UploadedBy = file.UploadedBy
                        };

                        return ApiResponseFactory.Success(response);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error uploading file: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // GET /rides/{id}/my-execution/files - Driver gets their own execution files
            app.MapGet("/rides/{id}/my-execution/files",
                [Authorize(Roles = "driver")]
                async (
                    Guid id,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var driver = await db.Drivers
                            .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);

                        if (driver == null)
                        {
                            return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status404NotFound);
                        }

                        var execution = await db.RideDriverExecutions
                            .Include(e => e.Files)
                            .FirstOrDefaultAsync(e => e.RideId == id && e.DriverId == driver.Id);

                        if (execution == null)
                        {
                            return ApiResponseFactory.Error("Execution not found.", StatusCodes.Status404NotFound);
                        }

                        var files = execution.Files.Select(f => new ExecutionFileDto
                        {
                            Id = f.Id,
                            RideDriverExecutionId = f.RideDriverExecutionId,
                            FileName = f.FileName,
                            FileSize = f.FileSize,
                            ContentType = f.ContentType,
                            UploadedAt = f.UploadedAt,
                            UploadedBy = f.UploadedBy
                        }).ToList();

                        return ApiResponseFactory.Success(files);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error retrieving files: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // GET /rides/{id}/my-execution/files/{fileId} - Driver downloads their own file
            app.MapGet("/rides/{id}/my-execution/files/{fileId}",
                [Authorize(Roles = "driver")]
                async (
                    Guid id,
                    Guid fileId,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var driver = await db.Drivers
                            .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);

                        if (driver == null)
                        {
                            return Results.NotFound("Driver profile not found.");
                        }

                        var execution = await db.RideDriverExecutions
                            .FirstOrDefaultAsync(e => e.RideId == id && e.DriverId == driver.Id);

                        if (execution == null)
                        {
                            return Results.NotFound("Execution not found.");
                        }

                        var file = await db.RideDriverExecutionFiles
                            .FirstOrDefaultAsync(f => f.Id == fileId && f.RideDriverExecutionId == execution.Id);

                        if (file == null)
                        {
                            return Results.NotFound("File not found.");
                        }

                        return Results.File(file.FileData, file.ContentType, file.FileName);
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem($"Error downloading file: {ex.Message}");
                    }
                });

            // DELETE /rides/{id}/my-execution/files/{fileId} - Driver deletes their own file
            app.MapDelete("/rides/{id}/my-execution/files/{fileId}",
                [Authorize(Roles = "driver")]
                async (
                    Guid id,
                    Guid fileId,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var driver = await db.Drivers
                            .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);

                        if (driver == null)
                        {
                            return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status404NotFound);
                        }

                        var execution = await db.RideDriverExecutions
                            .FirstOrDefaultAsync(e => e.RideId == id && e.DriverId == driver.Id);

                        if (execution == null)
                        {
                            return ApiResponseFactory.Error("Execution not found.", StatusCodes.Status404NotFound);
                        }

                        var file = await db.RideDriverExecutionFiles
                            .FirstOrDefaultAsync(f => f.Id == fileId && f.RideDriverExecutionId == execution.Id);

                        if (file == null)
                        {
                            return ApiResponseFactory.Error("File not found.", StatusCodes.Status404NotFound);
                        }

                        db.RideDriverExecutionFiles.Remove(file);
                        await db.SaveChangesAsync();

                        return ApiResponseFactory.Success(new { Message = "File deleted successfully." });
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error deleting file: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // GET /rides/{id}/executions/{driverId}/files - Admin gets specific driver's files
            app.MapGet("/rides/{id}/executions/{driverId}/files",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
                async (
                    Guid id,
                    Guid driverId,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var user = await userManager.FindByIdAsync(userId!);
                        if (user == null) return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                        // Check access permissions
                        var ride = await db.Rides.FirstOrDefaultAsync(r => r.Id == id);
                        if (ride == null)
                        {
                            return ApiResponseFactory.Error("Ride not found.", StatusCodes.Status404NotFound);
                        }

                        var userRoles = await userManager.GetRolesAsync(user);
                        var isGlobalAdmin = userRoles.Contains("globalAdmin");

                        if (!isGlobalAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("Contact person not found.", StatusCodes.Status404NotFound);
                            }

                            var allowedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Where(cpc => cpc.CompanyId.HasValue)
                                .Select(cpc => cpc.CompanyId!.Value)
                                .ToList();

                            if (!allowedCompanyIds.Contains(ride.CompanyId))
                            {
                                return ApiResponseFactory.Error("Access denied to this ride.", StatusCodes.Status403Forbidden);
                            }
                        }

                        var execution = await db.RideDriverExecutions
                            .Include(e => e.Files)
                            .FirstOrDefaultAsync(e => e.RideId == id && e.DriverId == driverId);

                        if (execution == null)
                        {
                            return ApiResponseFactory.Error("Execution not found for this driver.", StatusCodes.Status404NotFound);
                        }

                        var files = execution.Files.Select(f => new ExecutionFileDto
                        {
                            Id = f.Id,
                            RideDriverExecutionId = f.RideDriverExecutionId,
                            FileName = f.FileName,
                            FileSize = f.FileSize,
                            ContentType = f.ContentType,
                            UploadedAt = f.UploadedAt,
                            UploadedBy = f.UploadedBy
                        }).ToList();

                        return ApiResponseFactory.Success(files);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error retrieving files: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });
        }
    }
}


