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
                HttpContext http,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser,
                [FromQuery] Guid? driverId,
                [FromQuery] int? weekNr,
                [FromQuery] string? status, // hasDisputes | allApproved | hasPending
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 10) =>
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;

                var userId = userManager.GetUserId(currentUser)!;
                bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

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
                    query = query.Where(w =>
                        w.PartRides.Any(pr => pr.CompanyId.HasValue && allowedCompanyIds.Contains(pr.CompanyId.Value)));

                // Only include weeks that are in PendingAdmin status
                query = query.Where(w => w.Status == WeekApprovalStatus.PendingAdmin);

                if (!string.IsNullOrWhiteSpace(status))
                {
                    status = status.ToLowerInvariant();
                    query = status switch
                    {
                        "hasdisputes" => query.Where(w => w.PartRides.Any(pr => pr.Status == PartRideStatus.Dispute)),
                        "allapprovedorrejected" => query.Where(w => w.PartRides.All(pr =>
                            pr.Status == PartRideStatus.Accepted || pr.Status == PartRideStatus.Rejected)),
                        "haspending" => query.Where(
                            w => w.PartRides.Any(pr => pr.Status == PartRideStatus.PendingAdmin)),
                        _ => query
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
                        SummaryStatus = w.PartRides.Any(pr => pr.Status == PartRideStatus.Dispute)
                            ? "Has Disputes"
                            : w.PartRides.All(pr =>
                                pr.Status == PartRideStatus.Accepted || pr.Status == PartRideStatus.Rejected)
                                ? "All Approved"
                                : w.PartRides.Any(pr => pr.Status == PartRideStatus.PendingAdmin)
                                    ? "Has Pending"
                                    : "Unknown",
                        PartRideCount = w.PartRides.Count,
                        TotalHours = Math.Round(w.PartRides
                            .Where(pr => pr.Status != PartRideStatus.Rejected)
                            .Sum(pr => pr.DecimalHours ?? 0), 2),
                        PendingAdminCount = w.PartRides.Count(pr => pr.Status == PartRideStatus.PendingAdmin),
                        DisputeCount = w.PartRides.Count(pr => pr.Status == PartRideStatus.Dispute),
                        ForecastedEarning = Math.Round(
                            w.PartRides
                                .Where(pr => pr.Status != PartRideStatus.Rejected)
                                .Sum(pr =>
                                    pr.NightAllowance +
                                    pr.KilometerReimbursement +
                                    pr.ConsignmentFee +
                                    pr.VariousCompensation +
                                    pr.TaxFreeCompensation
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

        app.MapGet(
            "/weeks-to-submit/{id}",
            [Authorize(Roles = "globalAdmin, customerAdmin")]
            async (
                string id,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser) =>
            {
                /* ---------- validate id ---------- */
                if (!Guid.TryParse(id, out var weekApprovalId))
                    return ApiResponseFactory.Error("Invalid weekApproval ID format.", StatusCodes.Status400BadRequest);

                /* ---------- fetch object ---------- */
                var week = await db.WeekApprovals
                    .Include(w => w.PartRides!)
                    .ThenInclude(pr => pr.HoursCode)
                    .Include(w => w.PartRides!)
                    .ThenInclude(pr => pr.Driver!)
                    .ThenInclude(d => d.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == weekApprovalId);

                if (week is null)
                    return ApiResponseFactory.Error("WeekApproval not found.", StatusCodes.Status404NotFound);

                /* ---------- authorisation ---------- */
                var userId = userManager.GetUserId(currentUser)!;
                bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                if (!isGlobalAdmin)
                {
                    var contact = await db.ContactPersons
                        .Include(c => c.ContactPersonClientCompanies)
                        .SingleOrDefaultAsync(c => c.AspNetUserId == userId && !c.IsDeleted);

                    if (contact is null)
                        return ApiResponseFactory.Error("Not authorised.", StatusCodes.Status403Forbidden);

                    var allowedCompanyIds = contact.ContactPersonClientCompanies
                        .Select(cc => cc.CompanyId)
                        .Where(id => id.HasValue)
                        .Select(id => id!.Value)
                        .ToHashSet();

                    // the WeekApproval is considered “owned” if at least one PartRide
                    // belongs to a company the contact-person is linked with
                    bool owned = week.PartRides.Any(pr =>
                        pr.CompanyId.HasValue && allowedCompanyIds.Contains(pr.CompanyId.Value));

                    if (!owned)
                        return ApiResponseFactory.Error("You are not authorised for this week.",
                            StatusCodes.Status403Forbidden);
                }

                /* ---------- build response ---------- */
                var partRideRows = week.PartRides
                    .OrderByDescending(pr => pr.Date)
                    .ThenBy(pr => pr.Start)
                    .Select(pr => new
                    {
                        pr.Id,
                        pr.Date,
                        Hours = pr.Status == PartRideStatus.Rejected
                            ? 0
                            : Math.Round(pr.DecimalHours ?? 0, 2),
                        HoursCode = pr.HoursCode != null
                            ? new { pr.HoursCode.Id, pr.HoursCode.Name }
                            : null,
                        ForecastedEarnings = pr.Status == PartRideStatus.Rejected
                            ? 0
                            : Math.Round(
                                pr.NightAllowance +
                                pr.KilometerReimbursement +
                                pr.ConsignmentFee +
                                pr.VariousCompensation +
                                pr.TaxFreeCompensation,
                                2)
                    })
                    .ToList();

                var totalHours = Math.Round(partRideRows.Sum(r => r.Hours), 2);
                var totalForecast = Math.Round(partRideRows.Sum(r => r.ForecastedEarnings), 2);
                var vacationHoursUsed = week.PartRides
                    .Where(pr => pr.VacationHours < 0) // negative ⇒ used
                    .Sum(pr => -pr.VacationHours);
                
                var vacationHoursLeft = await db.PartRides
                    .Where(pr =>
                        pr.DriverId == week.DriverId &&
                        pr.Date.Year == week.Year)        
                    .SumAsync(pr => pr.VacationHours ?? 0); 
                
                var result = new
                {
                    week.Id,
                    week.Year,
                    week.WeekNr,
                    week.PeriodNr,
                    week.Status,
                    Driver = new
                    {
                        week.DriverId,
                        week.PartRides.FirstOrDefault()?.Driver?.User.FirstName,
                        week.PartRides.FirstOrDefault()?.Driver?.User.LastName
                    },
                    TotalHours = totalHours,
                    TotalForecasted = totalForecast,
                    PartRides = partRideRows,
                    VacationHoursUsed = vacationHoursUsed,
                    VacationHoursLeft = vacationHoursLeft
                };

                return ApiResponseFactory.Success(result);
            });

        app.MapPut(
            "/weeks-to-submit/{id}/allow-driver",
            [Authorize(Roles = "globalAdmin, customerAdmin")]
            async (
                string id,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser) =>
            {
                /* ---------- validate id ---------- */
                if (!Guid.TryParse(id, out var weekId))
                    return ApiResponseFactory.Error("Invalid weekApproval ID format.", StatusCodes.Status400BadRequest);

                /* ---------- fetch object ---------- */
                var week = await db.WeekApprovals
                    .Include(w => w.PartRides)
                    .FirstOrDefaultAsync(w => w.Id == weekId);

                if (week is null)
                    return ApiResponseFactory.Error("WeekApproval not found.", StatusCodes.Status404NotFound);

                /* ---------- authorisation ---------- */
                var userId = userManager.GetUserId(currentUser)!;
                bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                if (!isGlobalAdmin)
                {
                    // contact-person must own at least one company in the week
                    var contact = await db.ContactPersons
                        .Include(c => c.ContactPersonClientCompanies)
                        .SingleOrDefaultAsync(c => c.AspNetUserId == userId && !c.IsDeleted);

                    if (contact is null)
                        return ApiResponseFactory.Error("Not authorised.", StatusCodes.Status403Forbidden);

                    var allowedCompanyIds = contact.ContactPersonClientCompanies
                        .Select(cc => cc.CompanyId)
                        .Where(id => id.HasValue)
                        .Select(id => id!.Value)
                        .ToHashSet();

                    bool owned = week.PartRides.Any(pr =>
                        pr.CompanyId.HasValue && allowedCompanyIds.Contains(pr.CompanyId.Value));

                    if (!owned)
                        return ApiResponseFactory.Error("You are not authorised for this week.",
                            StatusCodes.Status403Forbidden);
                }

                /* ---------- business checks ---------- */
                if (week.Status != WeekApprovalStatus.PendingAdmin)
                    return ApiResponseFactory.Error("Week is not in PendingAdmin status.",
                        StatusCodes.Status409Conflict);

                if (week.PartRides.Any(pr => pr.Status == PartRideStatus.Dispute))
                    return ApiResponseFactory.Error("Week has rides in Dispute status.", StatusCodes.Status409Conflict);

                bool allApprovedOrRejected = week.PartRides.All(pr =>
                    pr.Status == PartRideStatus.Accepted || pr.Status == PartRideStatus.Rejected);

                if (!allApprovedOrRejected)
                    return ApiResponseFactory.Error(
                        "All rides must be Accepted or Rejected before allowing driver signature.",
                        StatusCodes.Status409Conflict);

                /* ---------- state change ---------- */
                week.Status = WeekApprovalStatus.PendingDriver;
                week.AdminUserId = userId is null ? null : Guid.Parse(userId);
                week.AdminAllowedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();

                return ApiResponseFactory.Success(new
                {
                    week.Id,
                    week.Year,
                    week.WeekNr,
                    NewStatus = week.Status
                }, StatusCodes.Status200OK);
            });

        return app;
    }
}