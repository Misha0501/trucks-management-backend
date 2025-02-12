using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;
using TruckManagement.Helpers;
using TruckManagement.Models; // Where ApiResponseFactory is defined

namespace TruckManagement.Endpoints
{
    public static class CharterEndpoints
    {
        public static void MapCharterEndpoints(this WebApplication app)
        {
            app.MapPost("/charters",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    [FromBody] CreateCharterRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        if (request == null 
                            || string.IsNullOrWhiteSpace(request.Name) 
                            || string.IsNullOrWhiteSpace(request.CompanyId)
                            || string.IsNullOrWhiteSpace(request.ClientId))
                        {
                            return ApiResponseFactory.Error(
                                "Name, CompanyId, and ClientId are required.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        if (!Guid.TryParse(request.CompanyId, out var companyGuid))
                        {
                            return ApiResponseFactory.Error(
                                "Invalid CompanyId format.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        if (!Guid.TryParse(request.ClientId, out var clientGuid))
                        {
                            return ApiResponseFactory.Error(
                                "Invalid ClientId format.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error(
                                "User not authenticated.",
                                StatusCodes.Status401Unauthorized
                            );
                        }

                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                        // Check if the provided company exists
                        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyGuid);
                        if (company == null)
                        {
                            return ApiResponseFactory.Error(
                                "The specified company does not exist.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        // Check if the provided client exists and belongs to the same company
                        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == clientGuid);
                        if (client == null)
                        {
                            return ApiResponseFactory.Error(
                                "The specified client does not exist.",
                                StatusCodes.Status400BadRequest
                            );
                        }
                        if (client.CompanyId != companyGuid)
                        {
                            return ApiResponseFactory.Error(
                                "This client does not belong to the provided company.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        // If not global admin, confirm user is associated with the provided company
                        if (!isGlobalAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error(
                                    "No contact person profile found. You are not authorized.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            // Gather all associated company IDs for the contact person
                            var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(cpc => cpc.CompanyId)
                                .Distinct()
                                .ToList();

                            // Ensure user is associated with the same company
                            if (!associatedCompanyIds.Contains(companyGuid))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to create a charter in this company.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }

                        var charter = new Charter
                        {
                            Id = Guid.NewGuid(),
                            Name = request.Name,
                            CompanyId = companyGuid,
                            ClientId = clientGuid,
                            Remark = request.Remark
                        };

                        db.Charters.Add(charter);
                        await db.SaveChangesAsync();

                        var responseData = new
                        {
                            charter.Id,
                            charter.Name,
                            charter.CompanyId,
                            charter.ClientId,
                            charter.Remark
                        };

                        return ApiResponseFactory.Success(responseData, StatusCodes.Status201Created);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error creating charter: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while creating the charter.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });
        }
    }
}
