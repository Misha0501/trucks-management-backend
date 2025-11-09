using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Helpers;

namespace TruckManagement.Endpoints
{
    public static class RideExecutionCommentEndpoints
    {
        public static void MapRideExecutionCommentEndpoints(this WebApplication app)
        {
            // POST /rides/{id}/executions/{driverId}/comments - Add comment to driver's execution
            app.MapPost("/rides/{id}/executions/{driverId}/comments",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer, driver")]
                async (
                    Guid id,
                    Guid driverId,
                    [FromBody] CreateCommentRequest request,
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

                        // Get the execution
                        var execution = await db.RideDriverExecutions
                            .Include(e => e.Ride)
                            .FirstOrDefaultAsync(e => e.RideId == id && e.DriverId == driverId);

                        if (execution == null)
                        {
                            return ApiResponseFactory.Error("Execution not found.", StatusCodes.Status404NotFound);
                        }

                        // Authorization check
                        var userRoles = await userManager.GetRolesAsync(user);
                        var isDriver = userRoles.Contains("driver");
                        var isAdmin = userRoles.Contains("globalAdmin") || userRoles.Contains("customerAdmin") || userRoles.Contains("employer");

                        // Drivers can only comment on their own execution
                        if (isDriver)
                        {
                            var driver = await db.Drivers
                                .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);

                            if (driver == null || driver.Id != driverId)
                            {
                                return ApiResponseFactory.Error("You can only comment on your own execution.", StatusCodes.Status403Forbidden);
                            }
                        }

                        // Admin company scope check
                        if (isAdmin && !userRoles.Contains("globalAdmin"))
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

                            if (!allowedCompanyIds.Contains(execution.Ride.CompanyId))
                            {
                                return ApiResponseFactory.Error("Access denied to this execution.", StatusCodes.Status403Forbidden);
                            }
                        }

                        // Create comment
                        var comment = new RideDriverExecutionComment
                        {
                            RideDriverExecutionId = execution.Id,
                            UserId = userId!,
                            Comment = request.Comment!
                        };

                        db.RideDriverExecutionComments.Add(comment);
                        await db.SaveChangesAsync();

                        // Return response
                        var response = new CommentDto
                        {
                            Id = comment.Id,
                            RideDriverExecutionId = comment.RideDriverExecutionId,
                            UserId = comment.UserId,
                            UserFirstName = user.FirstName,
                            UserLastName = user.LastName,
                            Comment = comment.Comment,
                            CreatedAt = comment.CreatedAt
                        };

                        return ApiResponseFactory.Success(response);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error creating comment: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // GET /rides/{id}/executions/{driverId}/comments - Get comments for driver's execution
            app.MapGet("/rides/{id}/executions/{driverId}/comments",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer, driver")]
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

                        // Get the execution
                        var execution = await db.RideDriverExecutions
                            .Include(e => e.Ride)
                            .Include(e => e.Comments)
                                .ThenInclude(c => c.User)
                            .FirstOrDefaultAsync(e => e.RideId == id && e.DriverId == driverId);

                        if (execution == null)
                        {
                            return ApiResponseFactory.Error("Execution not found.", StatusCodes.Status404NotFound);
                        }

                        // Authorization check
                        var userRoles = await userManager.GetRolesAsync(user);
                        var isDriver = userRoles.Contains("driver");
                        var isAdmin = userRoles.Contains("globalAdmin") || userRoles.Contains("customerAdmin") || userRoles.Contains("employer");

                        // Drivers can only view comments on their own execution
                        if (isDriver)
                        {
                            var driver = await db.Drivers
                                .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);

                            if (driver == null || driver.Id != driverId)
                            {
                                return ApiResponseFactory.Error("You can only view comments on your own execution.", StatusCodes.Status403Forbidden);
                            }
                        }

                        // Admin company scope check
                        if (isAdmin && !userRoles.Contains("globalAdmin"))
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

                            if (!allowedCompanyIds.Contains(execution.Ride.CompanyId))
                            {
                                return ApiResponseFactory.Error("Access denied to this execution.", StatusCodes.Status403Forbidden);
                            }
                        }

                        // Return comments
                        var comments = execution.Comments
                            .OrderBy(c => c.CreatedAt)
                            .Select(c => new CommentDto
                            {
                                Id = c.Id,
                                RideDriverExecutionId = c.RideDriverExecutionId,
                                UserId = c.UserId,
                                UserFirstName = c.User?.FirstName,
                                UserLastName = c.User?.LastName,
                                Comment = c.Comment,
                                CreatedAt = c.CreatedAt
                            })
                            .ToList();

                        return ApiResponseFactory.Success(comments);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error retrieving comments: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });
        }
    }

    // DTOs for comments
    public class CreateCommentRequest
    {
        public string? Comment { get; set; }
    }

    public class CommentDto
    {
        public Guid Id { get; set; }
        public Guid RideDriverExecutionId { get; set; }
        public string UserId { get; set; } = default!;
        public string? UserFirstName { get; set; }
        public string? UserLastName { get; set; }
        public string Comment { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
    }
}

