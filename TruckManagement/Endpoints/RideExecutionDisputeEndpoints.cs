using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;
using TruckManagement.Enums;
using TruckManagement.Helpers;
using TruckManagement.Models;

namespace TruckManagement.Endpoints;

public static class RideExecutionDisputeEndpoints
{
    public static void MapRideExecutionDisputeEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /rides/{id}/my-execution/disputes - Create dispute for own execution
        app.MapPost("/rides/{id}/my-execution/disputes",
            [Authorize(Roles = "driver")]
            async (
                Guid id,
                [FromBody] CreateRideExecutionDisputeRequest request,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var user = await userManager.FindByIdAsync(userId!);
                    if (user == null)
                        return ApiResponseFactory.Error("User not found.", StatusCodes.Status401Unauthorized);

                    // Find the driver record
                    var driver = await db.Drivers
                        .FirstOrDefaultAsync(d => d.AspNetUserId == userId);
                    
                    if (driver == null)
                        return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status403Forbidden);

                    // Find the ride
                    var ride = await db.Rides
                        .Include(r => r.DriverExecutions)
                        .FirstOrDefaultAsync(r => r.Id == id);
                    
                    if (ride == null)
                        return ApiResponseFactory.Error("Ride not found.", StatusCodes.Status404NotFound);

                    // Find the driver's execution
                    var execution = ride.DriverExecutions
                        .FirstOrDefault(e => e.DriverId == driver.Id);
                    
                    if (execution == null)
                        return ApiResponseFactory.Error("You don't have an execution for this ride.", StatusCodes.Status404NotFound);

                    // Only rejected executions can be disputed
                    if (execution.Status != RideDriverExecutionStatus.Rejected)
                        return ApiResponseFactory.Error("Only rejected executions can be disputed.", StatusCodes.Status400BadRequest);

                    // Check if there's already an open dispute
                    var existingDispute = await db.RideDriverExecutionDisputes
                        .AnyAsync(d => d.RideDriverExecutionId == execution.Id && 
                                      d.Status == RideExecutionDisputeStatus.Open);
                    
                    if (existingDispute)
                        return ApiResponseFactory.Error("There is already an open dispute for this execution.", StatusCodes.Status400BadRequest);

                    // Create the dispute
                    var dispute = new RideDriverExecutionDispute
                    {
                        Id = Guid.NewGuid(),
                        RideDriverExecutionId = execution.Id,
                        DriverId = driver.Id,
                        Reason = request.Reason,
                        Status = RideExecutionDisputeStatus.Open,
                        CreatedAtUtc = DateTime.UtcNow
                    };

                    db.RideDriverExecutionDisputes.Add(dispute);

                    // Update execution status to Dispute
                    execution.Status = RideDriverExecutionStatus.Dispute;
                    execution.LastModifiedAt = DateTime.UtcNow;
                    execution.LastModifiedBy = userId;

                    await db.SaveChangesAsync();

                    return ApiResponseFactory.Success(new
                    {
                        Id = dispute.Id,
                        RideDriverExecutionId = dispute.RideDriverExecutionId,
                        DriverId = dispute.DriverId,
                        Reason = dispute.Reason,
                        Status = dispute.Status.ToString(),
                        CreatedAtUtc = dispute.CreatedAtUtc
                    }, StatusCodes.Status201Created);
                }
                catch (Exception ex)
                {
                    return ApiResponseFactory.Error($"Error creating dispute: {ex.Message}", StatusCodes.Status500InternalServerError);
                }
            });

        // GET /rides/{id}/my-execution/disputes - Get disputes for own execution
        app.MapGet("/rides/{id}/my-execution/disputes",
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
                    var user = await userManager.FindByIdAsync(userId!);
                    if (user == null)
                        return ApiResponseFactory.Error("User not found.", StatusCodes.Status401Unauthorized);

                    // Find the driver record
                    var driver = await db.Drivers
                        .FirstOrDefaultAsync(d => d.AspNetUserId == userId);
                    
                    if (driver == null)
                        return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status403Forbidden);

                    // Find the ride
                    var ride = await db.Rides
                        .Include(r => r.DriverExecutions)
                        .FirstOrDefaultAsync(r => r.Id == id);
                    
                    if (ride == null)
                        return ApiResponseFactory.Error("Ride not found.", StatusCodes.Status404NotFound);

                    // Find the driver's execution
                    var execution = ride.DriverExecutions
                        .FirstOrDefault(e => e.DriverId == driver.Id);
                    
                    if (execution == null)
                        return ApiResponseFactory.Error("You don't have an execution for this ride.", StatusCodes.Status404NotFound);

                    // Get all disputes for this execution
                    var disputes = await db.RideDriverExecutionDisputes
                        .Where(d => d.RideDriverExecutionId == execution.Id)
                        .Include(d => d.Driver)
                            .ThenInclude(dr => dr.User)
                        .Include(d => d.ResolvedBy)
                        .Include(d => d.Comments)
                            .ThenInclude(c => c.Author)
                        .OrderByDescending(d => d.CreatedAtUtc)
                        .ToListAsync();

                    var disputeDtos = disputes.Select(d => new RideExecutionDisputeDto
                    {
                        Id = d.Id,
                        RideDriverExecutionId = d.RideDriverExecutionId,
                        DriverId = d.DriverId,
                        DriverFirstName = d.Driver.User?.FirstName ?? "",
                        DriverLastName = d.Driver.User?.LastName ?? "",
                        Reason = d.Reason,
                        Status = d.Status.ToString(),
                        CreatedAtUtc = d.CreatedAtUtc,
                        ResolvedAtUtc = d.ResolvedAtUtc,
                        ClosedAtUtc = d.ClosedAtUtc,
                        ResolvedById = d.ResolvedById,
                        ResolvedByName = d.ResolvedBy != null ? $"{d.ResolvedBy.FirstName} {d.ResolvedBy.LastName}" : null,
                        ResolutionNotes = d.ResolutionNotes,
                        ResolutionType = d.ResolutionType,
                        Comments = d.Comments.Select(c => new RideExecutionDisputeCommentDto
                        {
                            Id = c.Id,
                            DisputeId = c.DisputeId,
                            AuthorUserId = c.AuthorUserId,
                            AuthorFirstName = c.Author.FirstName ?? "",
                            AuthorLastName = c.Author.LastName ?? "",
                            Body = c.Body,
                            CreatedAtUtc = c.CreatedAtUtc
                        }).OrderBy(c => c.CreatedAtUtc).ToList()
                    }).ToList();

                    return ApiResponseFactory.Success(disputeDtos);
                }
                catch (Exception ex)
                {
                    return ApiResponseFactory.Error($"Error retrieving disputes: {ex.Message}", StatusCodes.Status500InternalServerError);
                }
            });

        // GET /rides/{rideId}/executions/{driverId}/disputes - Admin view disputes for specific execution
        app.MapGet("/rides/{rideId}/executions/{driverId}/disputes",
            [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
            async (
                Guid rideId,
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
                    if (user == null)
                        return ApiResponseFactory.Error("User not found.", StatusCodes.Status401Unauthorized);

                    var userRoles = await userManager.GetRolesAsync(user);
                    var isGlobalAdmin = userRoles.Contains("globalAdmin");

                    // Get user's accessible companies
                    List<Guid> allowedCompanyIds = new();
                    if (isGlobalAdmin)
                    {
                        allowedCompanyIds = await db.Companies.Select(c => c.Id).ToListAsync();
                    }
                    else
                    {
                        allowedCompanyIds = await db.ContactPersonClientCompanies
                            .Where(cp => cp.ContactPerson.AspNetUserId == userId)
                            .Select(cp => cp.CompanyId ?? Guid.Empty)
                            .Where(id => id != Guid.Empty)
                            .Distinct()
                            .ToListAsync();
                    }

                    // Find the ride
                    var ride = await db.Rides
                        .Include(r => r.DriverExecutions)
                        .FirstOrDefaultAsync(r => r.Id == rideId);
                    
                    if (ride == null || !allowedCompanyIds.Contains(ride.CompanyId))
                        return ApiResponseFactory.Error("Ride not found or access denied.", StatusCodes.Status404NotFound);

                    // Find the execution
                    var execution = ride.DriverExecutions
                        .FirstOrDefault(e => e.DriverId == driverId);
                    
                    if (execution == null)
                        return ApiResponseFactory.Error("Execution not found.", StatusCodes.Status404NotFound);

                    // Get all disputes for this execution
                    var disputes = await db.RideDriverExecutionDisputes
                        .Where(d => d.RideDriverExecutionId == execution.Id)
                        .Include(d => d.Driver)
                            .ThenInclude(dr => dr.User)
                        .Include(d => d.ResolvedBy)
                        .Include(d => d.Comments)
                            .ThenInclude(c => c.Author)
                        .OrderByDescending(d => d.CreatedAtUtc)
                        .ToListAsync();

                    var disputeDtos = disputes.Select(d => new RideExecutionDisputeDto
                    {
                        Id = d.Id,
                        RideDriverExecutionId = d.RideDriverExecutionId,
                        DriverId = d.DriverId,
                        DriverFirstName = d.Driver.User?.FirstName ?? "",
                        DriverLastName = d.Driver.User?.LastName ?? "",
                        Reason = d.Reason,
                        Status = d.Status.ToString(),
                        CreatedAtUtc = d.CreatedAtUtc,
                        ResolvedAtUtc = d.ResolvedAtUtc,
                        ClosedAtUtc = d.ClosedAtUtc,
                        ResolvedById = d.ResolvedById,
                        ResolvedByName = d.ResolvedBy != null ? $"{d.ResolvedBy.FirstName} {d.ResolvedBy.LastName}" : null,
                        ResolutionNotes = d.ResolutionNotes,
                        ResolutionType = d.ResolutionType,
                        Comments = d.Comments.Select(c => new RideExecutionDisputeCommentDto
                        {
                            Id = c.Id,
                            DisputeId = c.DisputeId,
                            AuthorUserId = c.AuthorUserId,
                            AuthorFirstName = c.Author.FirstName ?? "",
                            AuthorLastName = c.Author.LastName ?? "",
                            Body = c.Body,
                            CreatedAtUtc = c.CreatedAtUtc
                        }).OrderBy(c => c.CreatedAtUtc).ToList()
                    }).ToList();

                    return ApiResponseFactory.Success(disputeDtos);
                }
                catch (Exception ex)
                {
                    return ApiResponseFactory.Error($"Error retrieving disputes: {ex.Message}", StatusCodes.Status500InternalServerError);
                }
            });

        // POST /execution-disputes/{id}/comments - Add comment to dispute
        app.MapPost("/execution-disputes/{id}/comments",
            [Authorize(Roles = "globalAdmin, customerAdmin, employer, driver")]
            async (
                Guid id,
                [FromBody] AddDisputeCommentRequest request,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var user = await userManager.FindByIdAsync(userId!);
                    if (user == null)
                        return ApiResponseFactory.Error("User not found.", StatusCodes.Status401Unauthorized);

                    var userRoles = await userManager.GetRolesAsync(user);
                    var isDriver = userRoles.Contains("driver");

                    // Find the dispute
                    var dispute = await db.RideDriverExecutionDisputes
                        .Include(d => d.RideDriverExecution)
                            .ThenInclude(e => e.Ride)
                        .Include(d => d.Driver)
                        .FirstOrDefaultAsync(d => d.Id == id);
                    
                    if (dispute == null)
                        return ApiResponseFactory.Error("Dispute not found.", StatusCodes.Status404NotFound);

                    // Verify access
                    if (isDriver)
                    {
                        var driver = await db.Drivers.FirstOrDefaultAsync(d => d.AspNetUserId == userId);
                        if (driver == null || driver.Id != dispute.DriverId)
                            return ApiResponseFactory.Error("Access denied to this dispute.", StatusCodes.Status403Forbidden);
                    }
                    else
                    {
                        // Check company access for admins
                        var isGlobalAdmin = userRoles.Contains("globalAdmin");
                        if (!isGlobalAdmin)
                        {
                            var hasAccess = await db.ContactPersonClientCompanies
                                .AnyAsync(cp => cp.ContactPerson.AspNetUserId == userId && 
                                               cp.CompanyId == dispute.RideDriverExecution.Ride.CompanyId);
                            
                            if (!hasAccess)
                                return ApiResponseFactory.Error("Access denied to this dispute.", StatusCodes.Status403Forbidden);
                        }
                    }

                    // Dispute must be open
                    if (dispute.Status != RideExecutionDisputeStatus.Open)
                        return ApiResponseFactory.Error("Cannot add comments to a closed dispute.", StatusCodes.Status400BadRequest);

                    // Create the comment
                    var comment = new RideDriverExecutionDisputeComment
                    {
                        Id = Guid.NewGuid(),
                        DisputeId = dispute.Id,
                        AuthorUserId = userId!,
                        Body = request.Body,
                        CreatedAtUtc = DateTime.UtcNow
                    };

                    db.RideDriverExecutionDisputeComments.Add(comment);
                    await db.SaveChangesAsync();

                    // Reload with author info
                    await db.Entry(comment).Reference(c => c.Author).LoadAsync();

                    return ApiResponseFactory.Success(new RideExecutionDisputeCommentDto
                    {
                        Id = comment.Id,
                        DisputeId = comment.DisputeId,
                        AuthorUserId = comment.AuthorUserId,
                        AuthorFirstName = comment.Author.FirstName ?? "",
                        AuthorLastName = comment.Author.LastName ?? "",
                        Body = comment.Body,
                        CreatedAtUtc = comment.CreatedAtUtc
                    }, StatusCodes.Status201Created);
                }
                catch (Exception ex)
                {
                    return ApiResponseFactory.Error($"Error adding comment: {ex.Message}", StatusCodes.Status500InternalServerError);
                }
            });

        // PUT /execution-disputes/{id}/close - Close dispute (admin only)
        app.MapPut("/execution-disputes/{id}/close",
            [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
            async (
                Guid id,
                [FromBody] CloseRideExecutionDisputeRequest request,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var user = await userManager.FindByIdAsync(userId!);
                    if (user == null)
                        return ApiResponseFactory.Error("User not found.", StatusCodes.Status401Unauthorized);

                    var userRoles = await userManager.GetRolesAsync(user);
                    var isGlobalAdmin = userRoles.Contains("globalAdmin");

                    // Find the dispute
                    var dispute = await db.RideDriverExecutionDisputes
                        .Include(d => d.RideDriverExecution)
                            .ThenInclude(e => e.Ride)
                        .FirstOrDefaultAsync(d => d.Id == id);
                    
                    if (dispute == null)
                        return ApiResponseFactory.Error("Dispute not found.", StatusCodes.Status404NotFound);

                    // Check company access
                    if (!isGlobalAdmin)
                    {
                        var hasAccess = await db.ContactPersonClientCompanies
                            .AnyAsync(cp => cp.ContactPerson.AspNetUserId == userId && 
                                           cp.CompanyId == dispute.RideDriverExecution.Ride.CompanyId);
                        
                        if (!hasAccess)
                            return ApiResponseFactory.Error("Access denied to this dispute.", StatusCodes.Status403Forbidden);
                    }

                    // Dispute must be open
                    if (dispute.Status != RideExecutionDisputeStatus.Open)
                        return ApiResponseFactory.Error("Dispute is already closed.", StatusCodes.Status400BadRequest);

                    // Validate resolution type
                    var resolutionType = request.ResolutionType?.ToLower();
                    if (resolutionType != "accept" && resolutionType != "reject")
                        return ApiResponseFactory.Error("ResolutionType must be 'accept' or 'reject'.", StatusCodes.Status400BadRequest);

                    // Close the dispute
                    dispute.Status = RideExecutionDisputeStatus.Closed;
                    dispute.ClosedAtUtc = DateTime.UtcNow;
                    dispute.ResolvedById = userId;
                    dispute.ResolvedAtUtc = DateTime.UtcNow;
                    dispute.ResolutionNotes = request.ResolutionNotes;
                    dispute.ResolutionType = resolutionType == "accept" ? "Accept" : "Reject";

                    // Update execution status based on resolution type
                    if (resolutionType == "accept")
                    {
                        // Driver was right - approve the execution
                        dispute.RideDriverExecution.Status = RideDriverExecutionStatus.Approved;
                        dispute.RideDriverExecution.ApprovedAt = DateTime.UtcNow;
                        dispute.RideDriverExecution.ApprovedBy = userId;
                    }
                    else
                    {
                        // Admin was right - keep it rejected
                        dispute.RideDriverExecution.Status = RideDriverExecutionStatus.Rejected;
                    }
                    
                    dispute.RideDriverExecution.LastModifiedAt = DateTime.UtcNow;
                    dispute.RideDriverExecution.LastModifiedBy = userId;

                    await db.SaveChangesAsync();

                    return ApiResponseFactory.Success(new
                    {
                        Dispute = new
                        {
                            Id = dispute.Id,
                            Status = dispute.Status.ToString(),
                            ResolutionType = dispute.ResolutionType,
                            ResolutionNotes = dispute.ResolutionNotes,
                            ClosedAtUtc = dispute.ClosedAtUtc,
                            ResolvedById = dispute.ResolvedById,
                            ResolvedByName = user.FirstName + " " + user.LastName
                        },
                        ExecutionStatus = dispute.RideDriverExecution.Status.ToString()
                    });
                }
                catch (Exception ex)
                {
                    return ApiResponseFactory.Error($"Error closing dispute: {ex.Message}", StatusCodes.Status500InternalServerError);
                }
            });
    }
}

