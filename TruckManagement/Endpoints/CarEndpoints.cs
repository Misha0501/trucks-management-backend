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
    public static class CarEndpoints
    {
        public static void MapCarEndpoints(this WebApplication app)
        {
            app.MapPost("/cars",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    [FromBody] CreateCarRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        if (request == null
                            || string.IsNullOrWhiteSpace(request.LicensePlate)
                            || string.IsNullOrWhiteSpace(request.CompanyId))
                        {
                            return ApiResponseFactory.Error(
                                "LicensePlate and CompanyId are required.",
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

                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error(
                                "User not authenticated.",
                                StatusCodes.Status401Unauthorized
                            );
                        }

                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyGuid);
                        if (company == null)
                        {
                            return ApiResponseFactory.Error(
                                "Specified company does not exist.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        // If not global admin, check ownership
                        if (!isGlobalAdmin)
                        {
                            bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");
                            if (!isCustomerAdmin)
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to create a car.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

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

                            var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(cpc => cpc.CompanyId)
                                .Distinct()
                                .ToList();

                            if (!associatedCompanyIds.Contains(companyGuid))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to create a car in this company.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }

                        var car = new Car
                        {
                            Id = Guid.NewGuid(),
                            LicensePlate = request.LicensePlate,
                            Remark = request.Remark,
                            CompanyId = companyGuid
                        };

                        db.Cars.Add(car);
                        await db.SaveChangesAsync();

                        var responseData = new
                        {
                            car.Id,
                            car.LicensePlate,
                            car.Remark,
                            car.CompanyId
                        };

                        return ApiResponseFactory.Success(
                            responseData,
                            StatusCodes.Status201Created
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error creating car: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while creating the car.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapGet("/cars",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer, customer, customerAccountant")]
                async (
                    [FromQuery] string? companyId,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(companyId))
                        {
                            return ApiResponseFactory.Error(
                                "Query parameter 'companyId' is required.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        if (!Guid.TryParse(companyId, out Guid companyGuid))
                        {
                            return ApiResponseFactory.Error(
                                "Invalid 'companyId' format.",
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

                        // Only skip checks if user is global admin
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

                            var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(cpc => cpc.CompanyId)
                                .Distinct()
                                .ToList();

                            if (!associatedCompanyIds.Contains(companyGuid))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to view cars of this company.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }

                        var cars = await db.Cars
                            .Where(c => c.CompanyId == companyGuid)
                            .Select(c => new
                            {
                                c.Id,
                                c.LicensePlate,
                                c.Remark,
                                c.CompanyId
                            })
                            .ToListAsync();

                        return ApiResponseFactory.Success(cars, StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error listing cars: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while listing cars.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapPut("/cars/{id}",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    string id,
                    [FromBody] UpdateCarRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        if (!Guid.TryParse(id, out var carGuid))
                        {
                            return ApiResponseFactory.Error("Invalid car ID format.", StatusCodes.Status400BadRequest);
                        }

                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error("User not authenticated.",
                                StatusCodes.Status401Unauthorized);
                        }

                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                        bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");
                        if (!isGlobalAdmin && !isCustomerAdmin)
                        {
                            return ApiResponseFactory.Error("You are not authorized to edit cars.",
                                StatusCodes.Status403Forbidden);
                        }

                        var car = await db.Cars
                            .Include(c => c.Company)
                            .FirstOrDefaultAsync(c => c.Id == carGuid);
                        if (car == null)
                        {
                            return ApiResponseFactory.Error("Car not found.", StatusCodes.Status404NotFound);
                        }

                        if (!isGlobalAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);
                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error(
                                    "No contact person profile found. You are not authorized.",
                                    StatusCodes.Status403Forbidden);
                            }

                            var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(cpc => cpc.CompanyId)
                                .Distinct()
                                .ToList();

                            if (!associatedCompanyIds.Contains(car.CompanyId))
                            {
                                return ApiResponseFactory.Error("You are not authorized to edit this car.",
                                    StatusCodes.Status403Forbidden);
                            }

                            if (!string.IsNullOrWhiteSpace(request.CompanyId))
                            {
                                if (!Guid.TryParse(request.CompanyId, out var newCompanyGuid))
                                {
                                    return ApiResponseFactory.Error("Invalid new company ID format.",
                                        StatusCodes.Status400BadRequest);
                                }

                                if (!associatedCompanyIds.Contains(newCompanyGuid))
                                {
                                    return ApiResponseFactory.Error(
                                        "You are not authorized to assign this car to a company you're not associated with.",
                                        StatusCodes.Status403Forbidden);
                                }

                                car.CompanyId = newCompanyGuid;
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(request.CompanyId))
                            {
                                if (!Guid.TryParse(request.CompanyId, out var newCompanyGuid))
                                {
                                    return ApiResponseFactory.Error("Invalid new company ID format.",
                                        StatusCodes.Status400BadRequest);
                                }

                                car.CompanyId = newCompanyGuid;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(request.LicensePlate))
                        {
                            car.LicensePlate = request.LicensePlate;
                        }

                        if (!string.IsNullOrWhiteSpace(request.Remark))
                        {
                            car.Remark = request.Remark;
                        }

                        await db.SaveChangesAsync();

                        var responseData = new
                        {
                            car.Id,
                            car.LicensePlate,
                            car.Remark,
                            car.CompanyId
                        };

                        return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error editing car: {ex.Message}");
                        return ApiResponseFactory.Error("An unexpected error occurred while editing the car.",
                            StatusCodes.Status500InternalServerError);
                    }
                });

            app.MapDelete("/cars/{id}",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    string id,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        if (!Guid.TryParse(id, out var carGuid))
                        {
                            return ApiResponseFactory.Error("Invalid car ID format.", StatusCodes.Status400BadRequest);
                        }

                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error("User not authenticated.",
                                StatusCodes.Status401Unauthorized);
                        }

                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                        bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");
                        if (!isGlobalAdmin && !isCustomerAdmin)
                        {
                            return ApiResponseFactory.Error("You are not authorized to delete cars.",
                                StatusCodes.Status403Forbidden);
                        }

                        var car = await db.Cars
                            .FirstOrDefaultAsync(c => c.Id == carGuid);
                        if (car == null)
                        {
                            return ApiResponseFactory.Error("Car not found.", StatusCodes.Status404NotFound);
                        }

                        if (!isGlobalAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);
                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error(
                                    "No contact person profile found. You are not authorized.",
                                    StatusCodes.Status403Forbidden);
                            }

                            var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(cpc => cpc.CompanyId)
                                .Distinct()
                                .ToList();

                            if (!associatedCompanyIds.Contains(car.CompanyId))
                            {
                                return ApiResponseFactory.Error("You are not authorized to delete this car.",
                                    StatusCodes.Status403Forbidden);
                            }
                        }

                        db.Cars.Remove(car);
                        await db.SaveChangesAsync();

                        return ApiResponseFactory.Success("Car deleted successfully.", StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error deleting car: {ex.Message}");
                        return ApiResponseFactory.Error("An unexpected error occurred while deleting the car.",
                            StatusCodes.Status500InternalServerError);
                    }
                });
        }
    }
}