using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckManagement.Data;
using TruckManagement.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Entities;
using TruckManagement.Helpers;

namespace TruckManagement.Endpoints;

public static class WeeksToSubmitEndpoints
{
    public static WebApplication MapWeekToSubmitEndpoints(this WebApplication app)
    {
        app.MapGet(
            "/weeks-to-submit",
            [Authorize(Roles = "globalAdmin, customerAdmin")]
            async (
                HttpContext              http,
                ApplicationDbContext     db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal          currentUser,
                [FromQuery] Guid?        driverId,
                [FromQuery] int?         weekNr,
                [FromQuery] string?      status,             // hasDisputes | allApproved | hasPending
                [FromQuery] int          pageNumber = 1,
                [FromQuery] int          pageSize   = 10) =>
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize   < 1) pageSize   = 10;

                var userId = userManager.GetUserId(currentUser)!;
                bool isGlobalAdmin   = currentUser.IsInRole("globalAdmin");

                /* ---------------- authorization scope ---------------- */
                List<Guid>? allowedCompanyIds = null;
                if (!isGlobalAdmin)
                {
                    var contact = await db.ContactPersons
                                           .Include(c => c.ContactPersonClientCompanies)
                                           .SingleOrDefaultAsync(c => c.AspNetUserId == userId && !c.IsDeleted);
                    if (contact is null)
                        return ApiResponseFactory.Error("Not authorized.", StatusCodes.Status403Forbidden);

                    allowedCompanyIds = contact.ContactPersonClientCompanies
                                               .Select(cc => cc.CompanyId)
                                               .Where(id => id.HasValue)
                                               .Select(id => id!.Value)
                                               .Distinct()
                                               .ToList();
                }

                /* ---------------- base query ---------------- */
                var query = db.WeekApprovals
                              .Include(w => w.PartRides!)
                                  .ThenInclude(pr => pr.Driver!)
                              .AsQueryable();

                if (driverId.HasValue)
                    query = query.Where(w => w.DriverId == driverId.Value);

                if (weekNr.HasValue)
                    query = query.Where(w => w.WeekNr == weekNr.Value);

                if (allowedCompanyIds is not null)
                    query = query.Where(w => w.PartRides.Any(pr => pr.CompanyId.HasValue && allowedCompanyIds.Contains(pr.CompanyId.Value)));

                if (!string.IsNullOrWhiteSpace(status))
                {
                    status = status.ToLowerInvariant();
                    query = status switch
                    {
                        "hasdisputes" => query.Where(w => w.PartRides.Any(pr => pr.Status == PartRideStatus.Dispute)),
                        "allapprovedorrejected" => query.Where(w => w.PartRides.All(pr => pr.Status == PartRideStatus.Accepted || pr.Status == PartRideStatus.Rejected)),
                        "haspending"  => query.Where(w => w.PartRides.Any(pr => pr.Status == PartRideStatus.PendingAdmin)),
                        _             => query
                    };
                }

                /* ---------------- pagination ---------------- */
                int totalCount = await query.CountAsync();
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var weeks = await query
                    .OrderByDescending(w => w.Year)
                    .ThenByDescending(w => w.WeekNr)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(w => new
                    {
                        w.Id,
                        w.Year,
                        w.WeekNr,
                        w.PeriodNr,
                        w.Status,
                        Driver = new
                        {
                            w.DriverId,
                            w.PartRides.FirstOrDefault()!.Driver!.User.FirstName,
                            w.PartRides.FirstOrDefault()!.Driver!.User.LastName
                        },
                        SummaryStatus = w.PartRides.Any(pr => pr.Status == PartRideStatus.Dispute) ? "HasDisputes" :
                                        w.PartRides.All(pr => pr.Status == PartRideStatus.Accepted || pr.Status == PartRideStatus.Rejected) ? "AllApprovedOrRejected" :
                                        w.PartRides.Any(pr => pr.Status == PartRideStatus.PendingAdmin) ? "HasPending" : "Unknown",
                        PartRideCount = w.PartRides.Count,
                        TotalHours        = Math.Round(w.PartRides.Sum(pr => pr.DecimalHours ?? 0), 2),
                        PendingAdminCount = w.PartRides.Count(pr => pr.Status == PartRideStatus.PendingAdmin),
                        DisputeCount = w.PartRides.Count(pr => pr.Status == PartRideStatus.Dispute),
                        Forecasted = Math.Round(
                            w.PartRides.Sum(pr =>
                                pr.NightAllowance +
                                pr.KilometerReimbursement +
                                pr.ConsignmentFee +
                                pr.VariousCompensation
                            ), 2)
                    })
                    .ToListAsync();

                return ApiResponseFactory.Success(new
                {
                    pageNumber,
                    pageSize,
                    totalCount,
                    totalPages,
                    data = weeks
                });
            });

        return app;
    }
}