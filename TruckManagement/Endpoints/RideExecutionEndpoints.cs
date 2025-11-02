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
using TruckManagement.Services;

namespace TruckManagement.Endpoints
{
    public static class RideExecutionEndpoints
    {
        public static void MapRideExecutionEndpoints(this WebApplication app)
        {
            // PUT /rides/{id}/my-execution - Driver submits their own execution
            app.MapPut("/rides/{id}/my-execution",
                [Authorize(Roles = "driver")]
                async (
                    Guid id,
                    [FromBody] SubmitExecutionRequest request,
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

                        // Get driver
                        var driver = await db.Drivers
                            .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);

                        if (driver == null)
                        {
                            return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status404NotFound);
                        }

                        // Get ride with assignments
                        var ride = await db.Rides
                            .Include(r => r.DriverAssignments)
                            .FirstOrDefaultAsync(r => r.Id == id);

                        if (ride == null)
                        {
                            return ApiResponseFactory.Error("Ride not found.", StatusCodes.Status404NotFound);
                        }

                        // Verify driver is assigned to this ride
                        var assignment = ride.DriverAssignments.FirstOrDefault(da => da.DriverId == driver.Id);
                        if (assignment == null)
                        {
                            return ApiResponseFactory.Error("You are not assigned to this ride.", StatusCodes.Status403Forbidden);
                        }

                        // Check if execution already exists
                        var existingExecution = await db.RideDriverExecutions
                            .FirstOrDefaultAsync(e => e.RideId == id && e.DriverId == driver.Id);

                        RideDriverExecution execution;
                        bool isNew = false;

                        if (existingExecution != null)
                        {
                            // Update existing
                            if (existingExecution.Status == RideDriverExecutionStatus.Approved)
                            {
                                return ApiResponseFactory.Error("Cannot edit approved execution.", StatusCodes.Status409Conflict);
                            }
                            execution = existingExecution;
                        }
                        else
                        {
                            // Create new
                            execution = new RideDriverExecution
                            {
                                RideId = id,
                                DriverId = driver.Id,
                                IsPrimary = assignment.IsPrimary
                            };
                            isNew = true;
                        }

                        // Update fields from request
                        execution.ActualStartTime = request.ActualStartTime;
                        execution.ActualEndTime = request.ActualEndTime;
                        execution.ActualRestTime = request.ActualRestTime;
                        execution.ActualKilometers = request.ActualKilometers;
                        execution.ExtraKilometers = request.ExtraKilometers;
                        execution.ActualCosts = request.ActualCosts;
                        execution.CostsDescription = request.CostsDescription;
                        execution.Turnover = request.Turnover;
                        execution.Remark = request.Remark;
                        execution.CorrectionTotalHours = request.CorrectionTotalHours;
                        execution.VariousCompensation = request.VariousCompensation;

                        // Parse and set optional GUIDs
                        if (!string.IsNullOrEmpty(request.HoursCodeId) && Guid.TryParse(request.HoursCodeId, out var hoursCodeId))
                        {
                            execution.HoursCodeId = hoursCodeId;
                        }
                        if (!string.IsNullOrEmpty(request.HoursOptionId) && Guid.TryParse(request.HoursOptionId, out var hoursOptionId))
                        {
                            execution.HoursOptionId = hoursOptionId;
                        }
                        if (!string.IsNullOrEmpty(request.CharterId) && Guid.TryParse(request.CharterId, out var charterId))
                        {
                            execution.CharterId = charterId;
                        }

                        // Run calculations if we have the required fields
                        if (execution.ActualStartTime.HasValue && execution.ActualEndTime.HasValue && ride.PlannedDate.HasValue)
                        {
                            var calculator = new RideExecutionCalculationService(db);
                            execution = await calculator.CalculateAndApplyAsync(execution, ride.PlannedDate.Value);
                        }

                        // Update audit fields
                        execution.LastModifiedAt = DateTime.UtcNow;
                        execution.LastModifiedBy = userId;
                        if (isNew)
                        {
                            execution.SubmittedAt = DateTime.UtcNow;
                            execution.SubmittedBy = userId;
                        }

                        if (isNew)
                        {
                            db.RideDriverExecutions.Add(execution);
                        }

                        await db.SaveChangesAsync();

                        // Handle file uploads if provided
                        var uploadedFiles = new List<ExecutionFileDto>();
                        if (request.Files != null && request.Files.Any())
                        {
                            foreach (var fileUpload in request.Files)
                            {
                                try
                                {
                                    // Decode base64 file data
                                    byte[] fileData = Convert.FromBase64String(fileUpload.FileDataBase64);

                                    // Create file entity
                                    var file = new RideDriverExecutionFile
                                    {
                                        RideDriverExecutionId = execution.Id,
                                        FileName = fileUpload.FileName,
                                        FileSize = fileData.Length,
                                        ContentType = fileUpload.ContentType,
                                        FileData = fileData,
                                        UploadedBy = userId
                                    };

                                    db.RideDriverExecutionFiles.Add(file);
                                    await db.SaveChangesAsync();

                                    uploadedFiles.Add(new ExecutionFileDto
                                    {
                                        Id = file.Id,
                                        RideDriverExecutionId = file.RideDriverExecutionId,
                                        FileName = file.FileName,
                                        FileSize = file.FileSize,
                                        ContentType = file.ContentType,
                                        UploadedAt = file.UploadedAt,
                                        UploadedBy = file.UploadedBy
                                    });
                                }
                                catch (FormatException)
                                {
                                    // Skip invalid base64 files, continue with others
                                    continue;
                                }
                            }
                        }

                        // Update ride completion status
                        await UpdateRideCompletionStatus(db, id);

                        // Return response
                        var response = new RideDriverExecutionDto
                        {
                            Id = execution.Id,
                            RideId = execution.RideId,
                            DriverId = execution.DriverId,
                            IsPrimary = execution.IsPrimary,
                            DriverFirstName = user.FirstName,
                            DriverLastName = user.LastName,
                            ActualStartTime = execution.ActualStartTime,
                            ActualEndTime = execution.ActualEndTime,
                            ActualRestTime = execution.ActualRestTime,
                            RestCalculated = execution.RestCalculated,
                            ActualKilometers = execution.ActualKilometers,
                            ExtraKilometers = execution.ExtraKilometers,
                            ActualCosts = execution.ActualCosts,
                            CostsDescription = execution.CostsDescription,
                            Turnover = execution.Turnover,
                            Remark = execution.Remark,
                            CorrectionTotalHours = execution.CorrectionTotalHours,
                            DecimalHours = execution.DecimalHours,
                            NumberOfHours = execution.NumberOfHours,
                            PeriodNumber = execution.PeriodNumber,
                            WeekNrInPeriod = execution.WeekNrInPeriod,
                            WeekNumber = execution.WeekNumber,
                            NightAllowance = execution.NightAllowance,
                            KilometerReimbursement = execution.KilometerReimbursement,
                            ConsignmentFee = execution.ConsignmentFee,
                            TaxFreeCompensation = execution.TaxFreeCompensation,
                            VariousCompensation = execution.VariousCompensation,
                            StandOver = execution.StandOver,
                            SaturdayHours = execution.SaturdayHours,
                            SundayHolidayHours = execution.SundayHolidayHours,
                            VacationHoursEarned = execution.VacationHoursEarned,
                            Status = execution.Status,
                            HoursCodeId = execution.HoursCodeId,
                            HoursOptionId = execution.HoursOptionId,
                            CharterId = execution.CharterId,
                            SubmittedAt = execution.SubmittedAt,
                            LastModifiedAt = execution.LastModifiedAt,
                            ApprovedAt = execution.ApprovedAt,
                            Files = uploadedFiles.Any() ? uploadedFiles : null
                        };

                        return ApiResponseFactory.Success(response);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error submitting execution: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // GET /rides/{id}/my-execution - Driver sees their own execution
            app.MapGet("/rides/{id}/my-execution",
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
                            .Include(e => e.Driver)
                                .ThenInclude(d => d.User)
                            .Include(e => e.HoursCode)
                            .Include(e => e.HoursOption)
                            .FirstOrDefaultAsync(e => e.RideId == id && e.DriverId == driver.Id);

                        if (execution == null)
                        {
                            return ApiResponseFactory.Error("Execution not found.", StatusCodes.Status404NotFound);
                        }

                        var response = new RideDriverExecutionDto
                        {
                            Id = execution.Id,
                            RideId = execution.RideId,
                            DriverId = execution.DriverId,
                            IsPrimary = execution.IsPrimary,
                            DriverFirstName = execution.Driver.User.FirstName,
                            DriverLastName = execution.Driver.User.LastName,
                            ActualStartTime = execution.ActualStartTime,
                            ActualEndTime = execution.ActualEndTime,
                            ActualRestTime = execution.ActualRestTime,
                            RestCalculated = execution.RestCalculated,
                            ActualKilometers = execution.ActualKilometers,
                            ExtraKilometers = execution.ExtraKilometers,
                            ActualCosts = execution.ActualCosts,
                            CostsDescription = execution.CostsDescription,
                            Turnover = execution.Turnover,
                            Remark = execution.Remark,
                            CorrectionTotalHours = execution.CorrectionTotalHours,
                            DecimalHours = execution.DecimalHours,
                            NumberOfHours = execution.NumberOfHours,
                            PeriodNumber = execution.PeriodNumber,
                            WeekNrInPeriod = execution.WeekNrInPeriod,
                            WeekNumber = execution.WeekNumber,
                            NightAllowance = execution.NightAllowance,
                            KilometerReimbursement = execution.KilometerReimbursement,
                            ConsignmentFee = execution.ConsignmentFee,
                            TaxFreeCompensation = execution.TaxFreeCompensation,
                            VariousCompensation = execution.VariousCompensation,
                            StandOver = execution.StandOver,
                            SaturdayHours = execution.SaturdayHours,
                            SundayHolidayHours = execution.SundayHolidayHours,
                            VacationHoursEarned = execution.VacationHoursEarned,
                            Status = execution.Status,
                            HoursCodeId = execution.HoursCodeId,
                            HoursCodeName = execution.HoursCode?.Name,
                            HoursOptionId = execution.HoursOptionId,
                            HoursOptionName = execution.HoursOption?.Name,
                            CharterId = execution.CharterId,
                            SubmittedAt = execution.SubmittedAt,
                            LastModifiedAt = execution.LastModifiedAt,
                            ApprovedAt = execution.ApprovedAt
                        };

                        return ApiResponseFactory.Success(response);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error retrieving execution: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // GET /rides/{id}/executions - Admin sees all driver executions for ride
            app.MapGet("/rides/{id}/executions",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
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
                        if (user == null) return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                        var ride = await db.Rides
                            .Include(r => r.DriverExecutions)
                                .ThenInclude(e => e.Driver)
                                    .ThenInclude(d => d.User)
                            .Include(r => r.DriverExecutions)
                                .ThenInclude(e => e.HoursCode)
                            .Include(r => r.DriverExecutions)
                                .ThenInclude(e => e.HoursOption)
                            .FirstOrDefaultAsync(r => r.Id == id);

                        if (ride == null)
                        {
                            return ApiResponseFactory.Error("Ride not found.", StatusCodes.Status404NotFound);
                        }

                        // Check access permissions (non-global admin)
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

                        var executionsDto = ride.DriverExecutions.Select(e => new RideDriverExecutionDto
                        {
                            Id = e.Id,
                            RideId = e.RideId,
                            DriverId = e.DriverId,
                            IsPrimary = e.IsPrimary,
                            DriverFirstName = e.Driver.User?.FirstName,
                            DriverLastName = e.Driver.User?.LastName,
                            ActualStartTime = e.ActualStartTime,
                            ActualEndTime = e.ActualEndTime,
                            ActualRestTime = e.ActualRestTime,
                            RestCalculated = e.RestCalculated,
                            ActualKilometers = e.ActualKilometers,
                            ExtraKilometers = e.ExtraKilometers,
                            ActualCosts = e.ActualCosts,
                            CostsDescription = e.CostsDescription,
                            Turnover = e.Turnover,
                            Remark = e.Remark,
                            CorrectionTotalHours = e.CorrectionTotalHours,
                            DecimalHours = e.DecimalHours,
                            NumberOfHours = e.NumberOfHours,
                            PeriodNumber = e.PeriodNumber,
                            WeekNrInPeriod = e.WeekNrInPeriod,
                            WeekNumber = e.WeekNumber,
                            NightAllowance = e.NightAllowance,
                            KilometerReimbursement = e.KilometerReimbursement,
                            ConsignmentFee = e.ConsignmentFee,
                            TaxFreeCompensation = e.TaxFreeCompensation,
                            VariousCompensation = e.VariousCompensation,
                            StandOver = e.StandOver,
                            SaturdayHours = e.SaturdayHours,
                            SundayHolidayHours = e.SundayHolidayHours,
                            VacationHoursEarned = e.VacationHoursEarned,
                            Status = e.Status,
                            HoursCodeId = e.HoursCodeId,
                            HoursCodeName = e.HoursCode?.Name,
                            HoursOptionId = e.HoursOptionId,
                            HoursOptionName = e.HoursOption?.Name,
                            CharterId = e.CharterId,
                            SubmittedAt = e.SubmittedAt,
                            LastModifiedAt = e.LastModifiedAt,
                            ApprovedAt = e.ApprovedAt,
                            ApprovedBy = e.ApprovedBy
                        }).ToList();

                        var response = new RideExecutionsDto
                        {
                            RideId = ride.Id,
                            ExecutionCompletionStatus = ride.ExecutionCompletionStatus,
                            Executions = executionsDto
                        };

                        return ApiResponseFactory.Success(response);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error retrieving executions: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // GET /rides/my-assigned - Driver gets rides assigned to them
            app.MapGet("/rides/my-assigned",
                [Authorize(Roles = "driver")]
                async (
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    [FromQuery] string? startDate = null,
                    [FromQuery] string? endDate = null
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

                        // Parse date filters
                        DateTime? filterStartDate = null;
                        DateTime? filterEndDate = null;

                        if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var parsedStart))
                        {
                            filterStartDate = parsedStart.Date.ToUniversalTime();
                        }
                        if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var parsedEnd))
                        {
                            filterEndDate = parsedEnd.Date.ToUniversalTime();
                        }

                        // Get rides where driver is assigned
                        var query = db.RideDriverAssignments
                            .Include(rda => rda.Ride)
                                .ThenInclude(r => r.Client)
                            .Include(rda => rda.Ride)
                                .ThenInclude(r => r.Truck)
                            .Include(rda => rda.Ride)
                                .ThenInclude(r => r.DriverAssignments)
                                    .ThenInclude(da => da.Driver)
                                        .ThenInclude(d => d.User)
                            .Include(rda => rda.Ride)
                                .ThenInclude(r => r.DriverExecutions)
                            .Where(rda => rda.DriverId == driver.Id);

                        // Apply date filters if provided
                        if (filterStartDate.HasValue)
                        {
                            query = query.Where(rda => rda.Ride.PlannedDate >= filterStartDate.Value);
                        }
                        if (filterEndDate.HasValue)
                        {
                            query = query.Where(rda => rda.Ride.PlannedDate <= filterEndDate.Value);
                        }

                        var assignments = await query
                            .OrderBy(rda => rda.Ride.PlannedDate)
                            .ToListAsync();

                        var ridesData = assignments.Select(rda =>
                        {
                            var ride = rda.Ride;
                            var myExecution = ride.DriverExecutions.FirstOrDefault(e => e.DriverId == driver.Id);
                            
                            // Get other drivers on this ride
                            var otherDrivers = ride.DriverAssignments
                                .Where(da => da.DriverId != driver.Id)
                                .Select(da => new
                                {
                                    DriverId = da.DriverId,
                                    FirstName = da.Driver?.User?.FirstName,
                                    LastName = da.Driver?.User?.LastName,
                                    IsPrimary = da.IsPrimary,
                                    HasSubmittedExecution = ride.DriverExecutions.Any(e => e.DriverId == da.DriverId)
                                })
                                .ToList();

                            return new
                            {
                                RideId = ride.Id,
                                PlannedDate = ride.PlannedDate?.ToString("yyyy-MM-dd"),
                                PlannedStartTime = ride.PlannedStartTime,
                                PlannedEndTime = ride.PlannedEndTime,
                                RouteFromName = ride.RouteFromName,
                                RouteToName = ride.RouteToName,
                                TripNumber = ride.TripNumber,
                                ClientName = ride.Client?.Name,
                                TruckLicensePlate = ride.Truck?.LicensePlate,
                                MyRole = rda.IsPrimary ? "Primary" : "Second",
                                MyPlannedHours = rda.PlannedHours,
                                ExecutionCompletionStatus = ride.ExecutionCompletionStatus,
                                MyExecutionStatus = myExecution != null ? new
                                {
                                    HasSubmitted = true,
                                    Status = myExecution.Status.ToString(),
                                    DecimalHours = myExecution.DecimalHours,
                                    SubmittedAt = myExecution.SubmittedAt
                                } : new
                                {
                                    HasSubmitted = false,
                                    Status = "NotSubmitted",
                                    DecimalHours = (decimal?)null,
                                    SubmittedAt = (DateTime?)null
                                },
                                OtherDrivers = otherDrivers
                            };
                        }).ToList();

                        return ApiResponseFactory.Success(ridesData);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error retrieving assigned rides: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // DELETE /rides/{id}/my-execution - Driver deletes their own execution
            app.MapDelete("/rides/{id}/my-execution",
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
                            .FirstOrDefaultAsync(e => e.RideId == id && e.DriverId == driver.Id);

                        if (execution == null)
                        {
                            return ApiResponseFactory.Error("Execution not found.", StatusCodes.Status404NotFound);
                        }

                        // Cannot delete if approved
                        if (execution.Status == RideDriverExecutionStatus.Approved)
                        {
                            return ApiResponseFactory.Error("Cannot delete approved execution.", StatusCodes.Status409Conflict);
                        }

                        db.RideDriverExecutions.Remove(execution);
                        await db.SaveChangesAsync();

                        // Update ride completion status
                        await UpdateRideCompletionStatus(db, id);

                        return ApiResponseFactory.Success(new { Message = "Execution deleted successfully." });
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error deleting execution: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });
        }

        // Helper method to update ride completion status
        private static async Task UpdateRideCompletionStatus(ApplicationDbContext db, Guid rideId)
        {
            var ride = await db.Rides
                .Include(r => r.DriverAssignments)
                .Include(r => r.DriverExecutions)
                .FirstOrDefaultAsync(r => r.Id == rideId);

            if (ride == null) return;

            var assignedDriverCount = ride.DriverAssignments.Count;
            var executionCount = ride.DriverExecutions.Count;
            var approvedCount = ride.DriverExecutions.Count(e => e.Status == RideDriverExecutionStatus.Approved);

            if (executionCount == 0)
            {
                ride.ExecutionCompletionStatus = "none";
            }
            else if (approvedCount == assignedDriverCount && assignedDriverCount > 0)
            {
                ride.ExecutionCompletionStatus = "approved";
            }
            else if (executionCount == assignedDriverCount)
            {
                ride.ExecutionCompletionStatus = "complete";
            }
            else
            {
                ride.ExecutionCompletionStatus = "partial";
            }

            await db.SaveChangesAsync();
        }
    }
}

