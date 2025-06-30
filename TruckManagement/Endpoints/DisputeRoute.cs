using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;
using TruckManagement.Extensions;
using TruckManagement.Helpers;
using TruckManagement.Services;

namespace TruckManagement.Endpoints
{
    public static class DisputeEndpoints
    {
        public static void MapDisputeEndpoints(this WebApplication app)
        {
            app.MapGet("/disputes",
                [Authorize(Roles =
                    "driver,customerAdmin,customerAccountant,employer,customer,globalAdmin")]
                async (
                    [FromQuery] DateTime? date, // exact date
                    [FromQuery] DateTime? dateFrom, // range start (UTC)
                    [FromQuery] DateTime? dateTo, // range end   (UTC)
                    HttpContext http, // for companyIds[] and driverIds[]
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    [FromQuery] int pageNumber = 1,
                    [FromQuery] int pageSize = 10
                ) =>
                {
                    try
                    {
                        /* ---------- 1. parse & normalise query -------------------- */
                        if (pageNumber < 1) pageNumber = 1;
                        if (pageSize < 1) pageSize = 10;

                        var companyIdsRaw = http.Request.Query["companyIds"];
                        var companyGuids = GuidHelper.ParseGuids(companyIdsRaw, "companyIds");

                        var driverIdsRaw = http.Request.Query["driverIds"];
                        var driverGuids = GuidHelper.ParseGuids(driverIdsRaw, "driverIds");

                        var clientIdsRaw = http.Request.Query["clientIds"];
                        var clientGuids = GuidHelper.ParseGuids(clientIdsRaw, "clientIds");

                        var carIdsRaw = http.Request.Query["carIds"];
                        var carGuids = GuidHelper.ParseGuids(carIdsRaw, "carIds");

                        // Filter by dispute statuses if provided
                        var statusRaw = http.Request.Query["statuses"];
                        var statusList = statusRaw
                            .Select(s =>
                                Enum.TryParse<DisputeStatus>(s, true, out var parsed) ? parsed : (DisputeStatus?)null)
                            .Where(s => s.HasValue)
                            .Select(s => s.Value)
                            .ToList();

                        /* ---------- 2. establish caller’s scope ------------------- */
                        var aspUserId = userManager.GetUserId(currentUser);
                        var isGlobal = currentUser.IsInRole("globalAdmin");
                        var isDriver = currentUser.IsInRole("driver");

                        Guid? callerDriverId = null;
                        List<Guid> callerCompanies = new(); // for contact-person roles

                        if (isDriver && !isGlobal)
                        {
                            var driver = await db.Drivers
                                .AsNoTracking()
                                .FirstOrDefaultAsync(d => d.AspNetUserId == aspUserId && !d.IsDeleted);

                            if (driver is null)
                                return ApiResponseFactory.Error(
                                    "You are not registered as a driver.", StatusCodes.Status403Forbidden);

                            callerDriverId = driver.Id;
                        }
                        else if (!isGlobal) // contact-person roles
                        {
                            var contact = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == aspUserId);

                            if (contact is null)
                                return ApiResponseFactory.Error(
                                    "No contact-person profile found.", StatusCodes.Status403Forbidden);

                            callerCompanies = contact.ContactPersonClientCompanies
                                .Select(c => c.CompanyId)
                                .Where(id => id.HasValue)
                                .Select(id => id.Value)
                                .ToList();
                        }

                        /* ---------- 3. base query ---------------------------------- */
                        IQueryable<PartRideDispute> q = db.PartRideDisputes
                            .AsNoTracking()
                            .Include(d => d.PartRide)
                            .ThenInclude(pr => pr.Driver!.User)
                            .Include(d => d.PartRide)
                            .ThenInclude(pr => pr.Company);

                        /* ---------- 4. caller-scope filtering ---------------------- */
                        if (isDriver && !isGlobal)
                        {
                            q = q.Where(d => d.PartRide.DriverId == callerDriverId);
                        }
                        else if (!isGlobal) // contact-person
                        {
                            q = q.Where(d =>
                                d.PartRide.CompanyId.HasValue &&
                                callerCompanies.Contains(d.PartRide.CompanyId.Value));
                        }

                        /* ---------- 5. additional filters ------------------------- */
                        if (driverGuids.Any())
                            q = q.Where(d =>
                                d.PartRide.DriverId.HasValue && driverGuids.Contains(d.PartRide.DriverId.Value));

                        if (companyGuids.Any())
                            q = q.Where(d =>
                                d.PartRide.CompanyId.HasValue &&
                                companyGuids.Contains(d.PartRide.CompanyId.Value));

                        if (clientGuids.Any())
                            q = q.Where(d =>
                                d.PartRide.ClientId.HasValue &&
                                clientGuids.Contains(d.PartRide.ClientId.Value));

                        if (carGuids.Any())
                            q = q.Where(d =>
                                d.PartRide.CarId.HasValue &&
                                carGuids.Contains(d.PartRide.CarId.Value));

                        if (date.HasValue) // exact day (UTC)
                        {
                            var day = date.Value.Date;
                            q = q.Where(d => d.PartRide.Date.Date == DateTime.SpecifyKind(day, DateTimeKind.Utc));
                        }
                        else
                        {
                            if (dateFrom.HasValue)
                                q = q.Where(d =>
                                    d.PartRide.Date >= DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Utc));

                            if (dateTo.HasValue)
                                q = q.Where(d =>
                                    d.PartRide.Date <= DateTime.SpecifyKind(dateTo.Value.Date, DateTimeKind.Utc));
                        }

                        if (statusList.Any())
                            q = q.Where(d => statusList.Contains(d.Status));

                        /* ---------- 6. paging ------------------------------------- */
                        var totalCount = await q.CountAsync();
                        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                        var list = await q
                            .OrderByDescending(d => d.CreatedAtUtc)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .Select(d => new
                            {
                                d.Id,
                                d.Status,
                                d.CorrectionHours,
                                d.CreatedAtUtc,
                                Driver = d.PartRide.Driver != null
                                    ? new
                                    {
                                        d.PartRide.Driver.Id,
                                        d.PartRide.Driver.User.FirstName,
                                        d.PartRide.Driver.User.LastName
                                    }
                                    : null,
                                Company = d.PartRide.Company != null
                                    ? new { d.PartRide.Company.Id, d.PartRide.Company.Name }
                                    : null,
                                Client = d.PartRide.Client != null
                                    ? new { d.PartRide.Client.Id, d.PartRide.Client.Name }
                                    : null,
                                Car = d.PartRide.Car != null
                                    ? new { d.PartRide.Car.Id, d.PartRide.Car.LicensePlate }
                                    : null,
                                PartRide = new
                                {
                                    d.PartRide.Id,
                                    d.PartRide.Date,
                                    d.PartRide.DecimalHours
                                }
                            })
                            .ToListAsync();

                        /* ---------- 7. result -------------------------------------- */
                        return ApiResponseFactory.Success(new
                        {
                            pageNumber,
                            pageSize,
                            totalCount,
                            totalPages,
                            data = list
                        });
                    }
                    catch (ArgumentException ex)
                    {
                        return ApiResponseFactory.Error(ex.Message, StatusCodes.Status400BadRequest);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error fetching disputes: {ex}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while retrieving disputes.",
                            StatusCodes.Status500InternalServerError);
                    }
                });

            app.MapGet("/disputes/{id}",
                [Authorize(Roles =
                    "driver,customerAdmin,customerAccountant,employer,customer,globalAdmin")]
                async (
                    string id,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser) =>
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
                        var userId = userManager.GetUserId(currentUser);
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
                        var d = dispute; // for brevity in projection below
                        var response = new
                        {
                            d.Id,
                            d.CorrectionHours,
                            d.Status,
                            d.CreatedAtUtc,
                            d.ClosedAtUtc,
                            PartRide = new
                            {
                                d.PartRide.Id,
                                d.PartRide.Date,
                                d.PartRide.DecimalHours,
                                NewDecimalHours = d.PartRide.DecimalHours + d.CorrectionHours
                            },
                            Comments = d.Comments
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

            app.MapPut("/disputes/{id}",
                [Authorize(Roles =
                    "customerAdmin,customerAccountant,employer,customer,globalAdmin")]
                async (
                    string id,
                    [FromBody] UpdateDisputeRequest body,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser) =>
                {
                    try
                    {
                        /* --- 0. validate -------------------------------------------------- */
                        if (!Guid.TryParse(id, out var disputeGuid))
                            return ApiResponseFactory.Error("Invalid dispute ID.",
                                StatusCodes.Status400BadRequest);

                        if (body?.CorrectionHours is null)
                            return ApiResponseFactory.Error("correctionHours is required.",
                                StatusCodes.Status400BadRequest);

                        /* --- 1. load dispute  -------------------------------------------- */
                        var dispute = await db.PartRideDisputes
                            .Include(d => d.PartRide)
                            .FirstOrDefaultAsync(d => d.Id == disputeGuid);

                        if (dispute is null)
                            return ApiResponseFactory.Error("Dispute not found.",
                                StatusCodes.Status404NotFound);

                        /* --- 2. check status --------------------------------------------- */
                        if (dispute.Status is DisputeStatus.AcceptedByDriver
                            or DisputeStatus.AcceptedByAdmin
                            or DisputeStatus.Closed)
                            return ApiResponseFactory.Error(
                                "This dispute has already been resolved – it can’t be edited.",
                                StatusCodes.Status409Conflict);

                        /* --- 3. authorisation (contact-person vs. global) ----------------- */
                        var aspUserId = userManager.GetUserId(currentUser);
                        var isGlobal = currentUser.IsInRole("globalAdmin");

                        if (!isGlobal)
                        {
                            var contact = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == aspUserId);

                            if (contact is null)
                                return ApiResponseFactory.Error(
                                    "No contact-person profile found.", StatusCodes.Status403Forbidden);

                            var allowedCompanies = contact.ContactPersonClientCompanies
                                .Select(c => c.CompanyId)
                                .Distinct();

                            if (dispute.PartRide.CompanyId.HasValue &&
                                !allowedCompanies.Contains(dispute.PartRide.CompanyId.Value))
                                return ApiResponseFactory.Error(
                                    "You are not authorised to edit this dispute.",
                                    StatusCodes.Status403Forbidden);
                        }

                        /* --- 4. update + state flip -------------------------------------- */
                        dispute.CorrectionHours = body.CorrectionHours.Value;
                        db.Entry(dispute).State = EntityState.Modified;

                        await db.SaveChangesAsync();

                        /* --- 5. response -------------------------------------------------- */
                        return ApiResponseFactory.Success(new
                        {
                            dispute.Id,
                            dispute.Status,
                            dispute.CorrectionHours
                        }, StatusCodes.Status200OK);
                    }
                    catch (ArgumentException ex)
                    {
                        return ApiResponseFactory.Error(ex.Message, StatusCodes.Status400BadRequest);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        return ApiResponseFactory.Error(
                            "Someone else modified the dispute – reload and try again.",
                            StatusCodes.Status409Conflict);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error updating dispute: {ex}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the dispute.",
                            StatusCodes.Status500InternalServerError);
                    }
                });

            app.MapPost("/disputes/{id}/comments",
                [Authorize(Roles =
                    "driver,customerAdmin,customerAccountant,employer,customer,globalAdmin")]
                async (
                    string id,
                    [FromBody] CreateDisputeCommentRequest body,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser) =>
                {
                    try
                    {
                        /* ---------- 1. validate inputs -------------------------------- */
                        if (string.IsNullOrWhiteSpace(body?.Comment))
                            return ApiResponseFactory.Error("Comment cannot be empty.",
                                StatusCodes.Status400BadRequest);

                        if (!Guid.TryParse(id, out var disputeGuid))
                            return ApiResponseFactory.Error("Invalid dispute ID.",
                                StatusCodes.Status400BadRequest);

                        /* ---------- 2. load dispute (+ last comment) ------------------ */
                        var dispute = await db.PartRideDisputes
                            .Include(d => d.PartRide)
                            .ThenInclude(pr => pr.Company)
                            .Include(d => d.PartRide)
                            .ThenInclude(pr => pr.Driver)
                            .Include(d => d.Comments.OrderBy(c => c.CreatedAt))
                            .FirstOrDefaultAsync(d => d.Id == disputeGuid);

                        if (dispute is null)
                            return ApiResponseFactory.Error("Dispute not found.",
                                StatusCodes.Status404NotFound);

                        if (dispute.Status is DisputeStatus.Closed
                            or DisputeStatus.AcceptedByDriver
                            or DisputeStatus.AcceptedByAdmin)
                        {
                            return ApiResponseFactory.Error(
                                "No further comments are allowed – the dispute is closed or already accepted.",
                                StatusCodes.Status409Conflict);
                        }

                        /* ---------- 3. authorization ---------------------------------- */
                        var userId = userManager.GetUserId(currentUser);
                        var isDriver = currentUser.IsInRole("driver");
                        var isGlobal = currentUser.IsInRole("globalAdmin"); // bypass checks

                        if (!isGlobal)
                        {
                            if (isDriver)
                            {
                                var driver = await db.Drivers
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);

                                if (driver == null || dispute.PartRide.DriverId != driver.Id)
                                    return ApiResponseFactory.Error(
                                        "You are not authorized to comment on this dispute.",
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

                                var allowed = contact.ContactPersonClientCompanies
                                    .Select(c => c.CompanyId)
                                    .Distinct();

                                if (dispute.PartRide.CompanyId.HasValue &&
                                    !allowed.Contains(dispute.PartRide.CompanyId.Value))
                                    return ApiResponseFactory.Error(
                                        "You are not authorized to comment on this dispute.",
                                        StatusCodes.Status403Forbidden);
                            }
                        }

                        /* ---------- 4. “no double-post” rule -------------------------- */
                        var lastComment = dispute.Comments.LastOrDefault();
                        bool lastByDriver = lastComment?.AuthorUserId == dispute.PartRide.Driver?.AspNetUserId;
                        bool tryingDriverDouble = isDriver && lastByDriver;
                        bool tryingAdminDouble = !isDriver && !lastByDriver && lastComment != null;

                        if (tryingDriverDouble || tryingAdminDouble)
                            return ApiResponseFactory.Error(
                                "You must wait for the other party to reply before adding another comment.",
                                StatusCodes.Status409Conflict);

                        /* ---------- 5. add comment & flip status ---------------------- */
                        var newComment = new PartRideDisputeComment
                        {
                            Id = Guid.NewGuid(),
                            DisputeId = dispute.Id,
                            AuthorUserId = userId,
                            Body = body.Comment,
                            CreatedAt = DateTime.UtcNow
                        };
                        dispute.Comments.Add(newComment);
                        db.Entry(dispute).State = EntityState.Modified;
                        db.Entry(newComment).State = EntityState.Added;


                        if (isDriver)
                            dispute.Status = DisputeStatus.PendingAdmin;
                        else
                            dispute.Status = DisputeStatus.PendingDriver;

                        await db.SaveChangesAsync();

                        /* ---------- 6. result ----------------------------------------- */
                        var response = new
                        {
                            dispute.Id,
                            dispute.Status,
                            LatestComment = new
                            {
                                Body = body.Comment,
                                CreatedAt = DateTime.UtcNow,
                                AuthorUser = userId
                            }
                        };

                        return ApiResponseFactory.Success(response, StatusCodes.Status201Created);
                    }
                    catch (ArgumentException ex)
                    {
                        return ApiResponseFactory.Error(ex.Message, StatusCodes.Status400BadRequest);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error adding comment: {ex}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while adding a comment.",
                            StatusCodes.Status500InternalServerError);
                    }
                });

            app.MapPost("/disputes/{id}/accept",
                [Authorize(Roles =
                    "driver,customerAdmin,customerAccountant,employer,customer,globalAdmin")]
                async (
                    string id,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser) =>
                {
                    try
                    {
                        /* ---------- 0. basic validation ------------------------------ */
                        if (!Guid.TryParse(id, out var disputeGuid))
                            return ApiResponseFactory.Error("Invalid dispute ID.",
                                StatusCodes.Status400BadRequest);

                        /* ---------- 1. fetch dispute + ride -------------------------- */
                        var dispute = await db.PartRideDisputes
                            .Include(d => d.PartRide!)
                            .FirstOrDefaultAsync(d => d.Id == disputeGuid);

                        if (dispute is null)
                            return ApiResponseFactory.Error("Dispute not found.",
                                StatusCodes.Status404NotFound);

                        if (dispute.Status is DisputeStatus.AcceptedByDriver or DisputeStatus.AcceptedByAdmin
                            or DisputeStatus.Closed)
                            return ApiResponseFactory.Error("This dispute is already resolved.",
                                StatusCodes.Status409Conflict);

                        /* ---------- 2. figure out caller identity -------------------- */
                        var aspUserId = userManager.GetUserId(currentUser);
                        var isDriver = currentUser.IsInRole("driver");
                        var isGlobal = currentUser.IsInRole("globalAdmin");

                        /* ---------- 3-a. DRIVER path --------------------------------- */
                        if (isDriver && !isGlobal)
                        {
                            var driver = await db.Drivers
                                .AsNoTracking()
                                .FirstOrDefaultAsync(d => d.AspNetUserId == aspUserId && !d.IsDeleted);

                            if (driver == null || dispute.PartRide.DriverId != driver.Id)
                                return ApiResponseFactory.Error("You are not authorized.",
                                    StatusCodes.Status403Forbidden);

                            if (dispute.Status != DisputeStatus.PendingDriver)
                                return ApiResponseFactory.Error("Dispute isn’t waiting for driver signature.",
                                    StatusCodes.Status409Conflict);

                            /* ---- apply correction ---- */
                            dispute.PartRide.CorrectionTotalHours += dispute.CorrectionHours;

                            // ── recompute calculated fields ──
                            var calculator = new PartRideCalculator(db);
                            var calcContext = new PartRideCalculationContext(
                                Date: dispute.PartRide.Date,
                                Start: dispute.PartRide.Start,
                                End: dispute.PartRide.End,
                                DriverId: dispute.PartRide.DriverId,
                                HoursCodeId: dispute.PartRide.HoursCodeId ?? Guid.Empty,
                                HoursOptionId: dispute.PartRide.HoursOptionId,
                                Kilometers: dispute.PartRide.Kilometers ?? 0,
                                CorrectionTotalHours: dispute.PartRide.CorrectionTotalHours
                            );
                            var calcResult = await calculator.CalculateAsync(calcContext);
                            dispute.PartRide.ApplyCalculated(calcResult);

                            dispute.Status = DisputeStatus.AcceptedByDriver;
                            dispute.ClosedAtUtc = DateTime.UtcNow;
                        }
                        /* ---------- 3-b. ADMIN / contact-person path ----------------- */
                        else
                        {
                            //  ❱  contact-person roles share same check logic as earlier routes
                            if (!isGlobal)
                            {
                                var contact = await db.ContactPersons
                                    .Include(cp => cp.ContactPersonClientCompanies)
                                    .FirstOrDefaultAsync(cp => cp.AspNetUserId == aspUserId);

                                if (contact == null)
                                    return ApiResponseFactory.Error("No contact-person profile found.",
                                        StatusCodes.Status403Forbidden);

                                var companies = contact.ContactPersonClientCompanies
                                    .Select(c => c.CompanyId)
                                    .Distinct();

                                if (dispute.PartRide.CompanyId.HasValue &&
                                    !companies.Contains(dispute.PartRide.CompanyId.Value))
                                    return ApiResponseFactory.Error("You are not authorized.",
                                        StatusCodes.Status403Forbidden);
                            }

                            if (dispute.Status != DisputeStatus.PendingAdmin)
                                return ApiResponseFactory.Error("Dispute isn’t waiting for admin.",
                                    StatusCodes.Status409Conflict);

                            dispute.Status = DisputeStatus.AcceptedByAdmin;
                            dispute.ClosedAtUtc = DateTime.UtcNow;
                        }

                        await db.SaveChangesAsync();

                        /* ---------- 4. response -------------------------------------- */
                        var response = new
                        {
                            dispute.Id,
                            dispute.Status,
                            dispute.CorrectionHours,
                            dispute.ClosedAtUtc
                        };

                        return ApiResponseFactory.Success(response, StatusCodes.Status200OK);
                    }
                    catch (ArgumentException ex)
                    {
                        return ApiResponseFactory.Error(ex.Message, StatusCodes.Status400BadRequest);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        return ApiResponseFactory.Error(
                            "The dispute was modified by someone else. Please reload and try again.",
                            StatusCodes.Status409Conflict);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error accepting dispute: {ex}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while accepting the dispute.",
                            StatusCodes.Status500InternalServerError);
                    }
                });

            app.MapPost("/disputes/{id}/close",
                [Authorize(Roles =
                    "customerAdmin,customerAccountant,employer,customer,globalAdmin")]
                async (
                    string id,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser) =>
                {
                    try
                    {
                        /* ---------- 0. validate ID ------------------------------------ */
                        if (!Guid.TryParse(id, out var disputeGuid))
                            return ApiResponseFactory.Error("Invalid dispute ID.",
                                StatusCodes.Status400BadRequest);

                        /* ---------- 1. load dispute (+ ride) -------------------------- */
                        var dispute = await db.PartRideDisputes
                            .Include(d => d.PartRide)
                            .FirstOrDefaultAsync(d => d.Id == disputeGuid);

                        if (dispute is null)
                            return ApiResponseFactory.Error("Dispute not found.",
                                StatusCodes.Status404NotFound);

                        /* ---------- 2. only unresolved can be closed ------------------ */
                        if (dispute.Status is DisputeStatus.AcceptedByDriver
                            or DisputeStatus.AcceptedByAdmin
                            or DisputeStatus.Closed)
                        {
                            return ApiResponseFactory.Error(
                                "This dispute is already resolved or closed.",
                                StatusCodes.Status409Conflict);
                        }

                        /* ---------- 3. authorization (contact-person roles) ----------- */
                        var aspUserId = userManager.GetUserId(currentUser);
                        var isGlobal = currentUser.IsInRole("globalAdmin");

                        if (!isGlobal)
                        {
                            var contact = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == aspUserId);

                            if (contact == null)
                                return ApiResponseFactory.Error("No contact-person profile found.",
                                    StatusCodes.Status403Forbidden);

                            var allowedCompanies = contact.ContactPersonClientCompanies
                                .Select(c => c.CompanyId)
                                .Distinct();

                            if (dispute.PartRide.CompanyId.HasValue &&
                                !allowedCompanies.Contains(dispute.PartRide.CompanyId.Value))
                                return ApiResponseFactory.Error("You are not authorized.",
                                    StatusCodes.Status403Forbidden);
                        }

                        /* ---------- 4. close the dispute ------------------------------ */
                        dispute.Status = DisputeStatus.Closed;
                        dispute.ClosedAtUtc = DateTime.UtcNow;

                        await db.SaveChangesAsync();

                        /* ---------- 5. response -------------------------------------- */
                        return ApiResponseFactory.Success(new
                        {
                            dispute.Id,
                            dispute.Status,
                            dispute.ClosedAtUtc
                        });
                    }
                    catch (ArgumentException ex)
                    {
                        return ApiResponseFactory.Error(ex.Message, StatusCodes.Status400BadRequest);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        return ApiResponseFactory.Error(
                            "The dispute was modified by someone else. Please reload and try again.",
                            StatusCodes.Status409Conflict);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error closing dispute: {ex}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while closing the dispute.",
                            StatusCodes.Status500InternalServerError);
                    }
                });
        }
    }
}