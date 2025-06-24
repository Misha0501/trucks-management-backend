using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Helpers;

namespace TruckManagement.Endpoints
{
    public static class DisputeEndpoints
    {
        public static void MapDisputeEndpoints(this WebApplication app)
        {
           app.MapGet("/disputes/{id}",
            [Authorize(Roles =
                "driver,customerAdmin,customerAccountant,employer,customer,globalAdmin")]
            async (
                string id,
                ApplicationDbContext            db,
                UserManager<ApplicationUser>    userManager,
                ClaimsPrincipal                 currentUser) =>
            {
                try
                {
                    /* ---------- 1. validate route id --------------------------- */
                    if (!Guid.TryParse(id, out var disputeGuid))
                        return ApiResponseFactory.Error(
                            "Invalid dispute ID.", StatusCodes.Status400BadRequest);

                    /* ---------- 2. load dispute + related data ---------------- */
                    var dispute = await db.PartRideDisputes
                        .Include(d => d.PartRide)
                            .ThenInclude(pr => pr.Company)
                        .Include(d => d.PartRide)
                            .ThenInclude(pr => pr.Driver)
                        .Include(d => d.Comments)
                            .ThenInclude(c => c.Author)
                        .FirstOrDefaultAsync(d => d.Id == disputeGuid);

                    if (dispute is null)
                        return ApiResponseFactory.Error(
                            "Dispute not found.", StatusCodes.Status404NotFound);

                    /* ---------- 3. authorization ------------------------------ */
                    var userId   = userManager.GetUserId(currentUser);
                    var isGlobal = currentUser.IsInRole("globalAdmin");
                    var isDriver = currentUser.IsInRole("driver");

                    if (!isGlobal)
                    {
                        if (isDriver)
                        {
                            var driver = await db.Drivers
                                .AsNoTracking()
                                .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);

                            if (driver == null || dispute.PartRide.DriverId != driver.Id)
                                return ApiResponseFactory.Error(
                                    "You are not authorized to view this dispute.",
                                    StatusCodes.Status403Forbidden);
                        }
                        else
                        {
                            var contact = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                            if (contact == null)
                                return ApiResponseFactory.Error(
                                    "No contact-person profile found.",
                                    StatusCodes.Status403Forbidden);

                            var companyIds = contact.ContactPersonClientCompanies
                                .Select(cpc => cpc.CompanyId)
                                .Distinct();

                            if (dispute.PartRide.CompanyId.HasValue &&
                                !companyIds.Contains(dispute.PartRide.CompanyId.Value))
                                return ApiResponseFactory.Error(
                                    "You are not authorized to view this dispute.",
                                    StatusCodes.Status403Forbidden);
                        }
                    }

                    /* ---------- 4. projection --------------------------------- */
                    var response = new
                    {
                        dispute.Id,
                        dispute.PartRideId,
                        dispute.CorrectionHours,
                        dispute.Status,
                        dispute.CreatedAtUtc,
                        dispute.ClosedAtUtc,
                        Comments = dispute.Comments
                            .OrderBy(c => c.CreatedAt)
                            .Select(c => new
                            {
                                c.Id,
                                c.Body,
                                c.CreatedAt,
                                Author = new
                                {
                                    c.Author?.Id,
                                    c.Author?.FirstName,
                                    c.Author?.LastName,
                                    c.Author?.Email
                                }
                            })
                    };

                    return ApiResponseFactory.Success(response, StatusCodes.Status200OK);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error fetching dispute: {ex}");
                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while retrieving dispute.",
                        StatusCodes.Status500InternalServerError);
                }
            });
        }
    }
}