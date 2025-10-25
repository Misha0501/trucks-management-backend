using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;
using TruckManagement.Helpers;

namespace TruckManagement.Endpoints
{
    public static class CapacityTemplateEndpoints
    {
        public static void MapCapacityTemplateEndpoints(this WebApplication app)
        {
            // GET /capacity-templates?companyId={guid}
            app.MapGet("/capacity-templates",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
                async (
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    [FromQuery] string? companyId = null
                ) =>
                {
                    try
                    {
                        // Get current user
                        var currentUserId = userManager.GetUserId(currentUser);
                        var isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                        // Determine company filter
                        Guid? filterCompanyId = null;
                        if (!string.IsNullOrEmpty(companyId))
                        {
                            if (!Guid.TryParse(companyId, out var parsedCompanyId))
                            {
                                return ApiResponseFactory.Error("Invalid company ID format.", StatusCodes.Status400BadRequest);
                            }
                            filterCompanyId = parsedCompanyId;
                        }

                        // Build query
                        var query = db.ClientCapacityTemplates
                            .Include(t => t.Client)
                            .Include(t => t.Company)
                            .AsQueryable();

                        // Company scoping for non-global admins
                        if (!isGlobalAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("Contact person not found.", StatusCodes.Status404NotFound);
                            }

                            var allowedCompanyIds = await db.ContactPersonClientCompanies
                                .Where(cpc => cpc.ContactPersonId == contactPerson.Id && cpc.CompanyId.HasValue)
                                .Select(cpc => cpc.CompanyId!.Value)
                                .Distinct()
                                .ToListAsync();

                            query = query.Where(t => allowedCompanyIds.Contains(t.CompanyId));
                        }

                        // Apply company filter if specified
                        if (filterCompanyId.HasValue)
                        {
                            query = query.Where(t => t.CompanyId == filterCompanyId.Value);
                        }

                        var templates = await query
                            .OrderByDescending(t => t.CreatedAt)
                            .ToListAsync();

                        var templateDtos = templates.Select(t => new CapacityTemplateDto
                        {
                            Id = t.Id,
                            CompanyId = t.CompanyId,
                            ClientId = t.ClientId,
                            Client = t.Client != null ? new ClientDto
                            {
                                Id = t.Client.Id,
                                Name = t.Client.Name,
                                Address = t.Client.Address,
                                City = t.Client.City,
                                Country = t.Client.Country,
                                Email = t.Client.Email,
                                PhoneNumber = t.Client.PhoneNumber
                            } : null,
                            StartDate = t.StartDate,
                            EndDate = t.EndDate,
                            MondayTrucks = t.MondayTrucks,
                            TuesdayTrucks = t.TuesdayTrucks,
                            WednesdayTrucks = t.WednesdayTrucks,
                            ThursdayTrucks = t.ThursdayTrucks,
                            FridayTrucks = t.FridayTrucks,
                            SaturdayTrucks = t.SaturdayTrucks,
                            SundayTrucks = t.SundayTrucks,
                            Notes = t.Notes,
                            IsActive = t.IsActive,
                            CreatedAt = t.CreatedAt
                        }).ToList();

                        return ApiResponseFactory.Success(templateDtos);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error retrieving capacity templates: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // POST /capacity-templates
            app.MapPost("/capacity-templates",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    [FromBody] CreateCapacityTemplateRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        // Validate request
                        if (!Guid.TryParse(request.CompanyId, out var companyGuid))
                        {
                            return ApiResponseFactory.Error("Invalid company ID format.", StatusCodes.Status400BadRequest);
                        }

                        if (!Guid.TryParse(request.ClientId, out var clientGuid))
                        {
                            return ApiResponseFactory.Error("Invalid client ID format.", StatusCodes.Status400BadRequest);
                        }

                        if (request.StartDate >= request.EndDate)
                        {
                            return ApiResponseFactory.Error("Start date must be before end date.", StatusCodes.Status400BadRequest);
                        }

                        // Validate at least one day has trucks
                        var totalTrucks = request.MondayTrucks + request.TuesdayTrucks + request.WednesdayTrucks +
                                         request.ThursdayTrucks + request.FridayTrucks + request.SaturdayTrucks +
                                         request.SundayTrucks;
                        if (totalTrucks <= 0)
                        {
                            return ApiResponseFactory.Error("At least one day must have trucks assigned.", StatusCodes.Status400BadRequest);
                        }

                        // Check authorization
                        var currentUserId = userManager.GetUserId(currentUser);
                        var isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                        if (!isGlobalAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("Contact person not found.", StatusCodes.Status404NotFound);
                            }

                            var hasAccess = await db.ContactPersonClientCompanies
                                .AnyAsync(cpc => cpc.ContactPersonId == contactPerson.Id &&
                                               cpc.CompanyId == companyGuid);

                            if (!hasAccess)
                            {
                                return ApiResponseFactory.Error("You don't have access to this company.", StatusCodes.Status403Forbidden);
                            }
                        }

                        // Verify company exists
                        var companyExists = await db.Companies.AnyAsync(c => c.Id == companyGuid);
                        if (!companyExists)
                        {
                            return ApiResponseFactory.Error("Company not found.", StatusCodes.Status404NotFound);
                        }

                        // Verify client exists and belongs to company
                        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == clientGuid);
                        if (client == null)
                        {
                            return ApiResponseFactory.Error("Client not found.", StatusCodes.Status404NotFound);
                        }

                        if (client.CompanyId != companyGuid)
                        {
                            return ApiResponseFactory.Error("Client does not belong to the specified company.", StatusCodes.Status400BadRequest);
                        }

                        // Create template
                        var template = new ClientCapacityTemplate
                        {
                            Id = Guid.NewGuid(),
                            CompanyId = companyGuid,
                            ClientId = clientGuid,
                            StartDate = DateTime.SpecifyKind(request.StartDate.Date, DateTimeKind.Utc),
                            EndDate = DateTime.SpecifyKind(request.EndDate.Date, DateTimeKind.Utc),
                            MondayTrucks = request.MondayTrucks,
                            TuesdayTrucks = request.TuesdayTrucks,
                            WednesdayTrucks = request.WednesdayTrucks,
                            ThursdayTrucks = request.ThursdayTrucks,
                            FridayTrucks = request.FridayTrucks,
                            SaturdayTrucks = request.SaturdayTrucks,
                            SundayTrucks = request.SundayTrucks,
                            Notes = request.Notes,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        db.ClientCapacityTemplates.Add(template);
                        await db.SaveChangesAsync();

                        // Load client details for response
                        var createdTemplate = await db.ClientCapacityTemplates
                            .Include(t => t.Client)
                            .FirstOrDefaultAsync(t => t.Id == template.Id);

                        var responseDto = new CapacityTemplateDto
                        {
                            Id = createdTemplate!.Id,
                            CompanyId = createdTemplate.CompanyId,
                            ClientId = createdTemplate.ClientId,
                            Client = new ClientDto
                            {
                                Id = createdTemplate.Client.Id,
                                Name = createdTemplate.Client.Name,
                                Address = createdTemplate.Client.Address,
                                City = createdTemplate.Client.City,
                                Country = createdTemplate.Client.Country,
                                Email = createdTemplate.Client.Email,
                                PhoneNumber = createdTemplate.Client.PhoneNumber
                            },
                            StartDate = createdTemplate.StartDate,
                            EndDate = createdTemplate.EndDate,
                            MondayTrucks = createdTemplate.MondayTrucks,
                            TuesdayTrucks = createdTemplate.TuesdayTrucks,
                            WednesdayTrucks = createdTemplate.WednesdayTrucks,
                            ThursdayTrucks = createdTemplate.ThursdayTrucks,
                            FridayTrucks = createdTemplate.FridayTrucks,
                            SaturdayTrucks = createdTemplate.SaturdayTrucks,
                            SundayTrucks = createdTemplate.SundayTrucks,
                            Notes = createdTemplate.Notes,
                            IsActive = createdTemplate.IsActive,
                            CreatedAt = createdTemplate.CreatedAt
                        };

                        return ApiResponseFactory.Success(responseDto, StatusCodes.Status201Created);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error creating capacity template: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // PUT /capacity-templates/{id}
            app.MapPut("/capacity-templates/{id}",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    Guid id,
                    [FromBody] UpdateCapacityTemplateRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        // Find template
                        var template = await db.ClientCapacityTemplates
                            .Include(t => t.Client)
                            .FirstOrDefaultAsync(t => t.Id == id);

                        if (template == null)
                        {
                            return ApiResponseFactory.Error("Template not found.", StatusCodes.Status404NotFound);
                        }

                        // Check authorization
                        var currentUserId = userManager.GetUserId(currentUser);
                        var isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                        if (!isGlobalAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("Contact person not found.", StatusCodes.Status404NotFound);
                            }

                            var hasAccess = await db.ContactPersonClientCompanies
                                .AnyAsync(cpc => cpc.ContactPersonId == contactPerson.Id &&
                                               cpc.CompanyId == template.CompanyId);

                            if (!hasAccess)
                            {
                                return ApiResponseFactory.Error("You don't have access to this template.", StatusCodes.Status403Forbidden);
                            }
                        }

                        // Validate dates
                        if (request.StartDate >= request.EndDate)
                        {
                            return ApiResponseFactory.Error("Start date must be before end date.", StatusCodes.Status400BadRequest);
                        }

                        // Validate at least one day has trucks
                        var totalTrucks = request.MondayTrucks + request.TuesdayTrucks + request.WednesdayTrucks +
                                         request.ThursdayTrucks + request.FridayTrucks + request.SaturdayTrucks +
                                         request.SundayTrucks;
                        if (totalTrucks <= 0)
                        {
                            return ApiResponseFactory.Error("At least one day must have trucks assigned.", StatusCodes.Status400BadRequest);
                        }

                        // Update template
                        template.StartDate = DateTime.SpecifyKind(request.StartDate.Date, DateTimeKind.Utc);
                        template.EndDate = DateTime.SpecifyKind(request.EndDate.Date, DateTimeKind.Utc);
                        template.MondayTrucks = request.MondayTrucks;
                        template.TuesdayTrucks = request.TuesdayTrucks;
                        template.WednesdayTrucks = request.WednesdayTrucks;
                        template.ThursdayTrucks = request.ThursdayTrucks;
                        template.FridayTrucks = request.FridayTrucks;
                        template.SaturdayTrucks = request.SaturdayTrucks;
                        template.SundayTrucks = request.SundayTrucks;
                        template.Notes = request.Notes;
                        template.IsActive = request.IsActive;

                        await db.SaveChangesAsync();

                        var responseDto = new CapacityTemplateDto
                        {
                            Id = template.Id,
                            CompanyId = template.CompanyId,
                            ClientId = template.ClientId,
                            Client = new ClientDto
                            {
                                Id = template.Client.Id,
                                Name = template.Client.Name,
                                Address = template.Client.Address,
                                City = template.Client.City,
                                Country = template.Client.Country,
                                Email = template.Client.Email,
                                PhoneNumber = template.Client.PhoneNumber
                            },
                            StartDate = template.StartDate,
                            EndDate = template.EndDate,
                            MondayTrucks = template.MondayTrucks,
                            TuesdayTrucks = template.TuesdayTrucks,
                            WednesdayTrucks = template.WednesdayTrucks,
                            ThursdayTrucks = template.ThursdayTrucks,
                            FridayTrucks = template.FridayTrucks,
                            SaturdayTrucks = template.SaturdayTrucks,
                            SundayTrucks = template.SundayTrucks,
                            Notes = template.Notes,
                            IsActive = template.IsActive,
                            CreatedAt = template.CreatedAt
                        };

                        return ApiResponseFactory.Success(responseDto);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error updating capacity template: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // DELETE /capacity-templates/{id}
            app.MapDelete("/capacity-templates/{id}",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    Guid id,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        var template = await db.ClientCapacityTemplates.FindAsync(id);

                        if (template == null)
                        {
                            return ApiResponseFactory.Error("Template not found.", StatusCodes.Status404NotFound);
                        }

                        // Check authorization
                        var currentUserId = userManager.GetUserId(currentUser);
                        var isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                        if (!isGlobalAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("Contact person not found.", StatusCodes.Status404NotFound);
                            }

                            var hasAccess = await db.ContactPersonClientCompanies
                                .AnyAsync(cpc => cpc.ContactPersonId == contactPerson.Id &&
                                               cpc.CompanyId == template.CompanyId);

                            if (!hasAccess)
                            {
                                return ApiResponseFactory.Error("You don't have access to this template.", StatusCodes.Status403Forbidden);
                            }
                        }

                        db.ClientCapacityTemplates.Remove(template);
                        await db.SaveChangesAsync();

                        return ApiResponseFactory.Success("Template deleted successfully.");
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error deleting capacity template: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });
        }
    }
}

