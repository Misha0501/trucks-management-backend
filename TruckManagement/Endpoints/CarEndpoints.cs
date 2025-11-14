using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;
using TruckManagement.Helpers;
using TruckManagement.Models; // Where ApiResponseFactory is defined
using TruckManagement.Options;

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
                    ClaimsPrincipal currentUser,
                    IWebHostEnvironment env,
                    IOptions<StorageOptions> cfg,
                    IResourceLocalizer resourceLocalizer
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

                        using var transaction = await db.Database.BeginTransactionAsync();

                        var car = new Car
                        {
                            Id = Guid.NewGuid(),
                            LicensePlate = request.LicensePlate,
                            VehicleYear = request.VehicleYear,
                            RegistrationDate = request.RegistrationDate,
                            Remark = request.Remark,
                            CompanyId = companyGuid
                        };

                        db.Cars.Add(car);
                        await db.SaveChangesAsync();

                        // Handle file uploads
                        var newUploads = request.NewUploads ?? new List<UploadFileRequest>();
                        if (newUploads.Any())
                        {
                            var tmpRoot = Path.Combine(env.ContentRootPath, cfg.Value.TmpPath);
                            var finalRoot = Path.Combine(env.ContentRootPath, cfg.Value.BasePathCompanies);

                            CarFileHelper.MoveUploadsToCar(car.Id, car.CompanyId, newUploads, tmpRoot,
                                finalRoot, db, resourceLocalizer);

                            await db.SaveChangesAsync(); // Save CarFile entries
                        }

                        await transaction.CommitAsync();

                        // Load car with files for response
                        var createdCar = await db.Cars
                            .Include(c => c.Files)
                            .FirstOrDefaultAsync(c => c.Id == car.Id);

                        var responseData = new CarDto
                        {
                            Id = createdCar!.Id,
                            LicensePlate = createdCar.LicensePlate,
                            VehicleYear = createdCar.VehicleYear,
                            RegistrationDate = createdCar.RegistrationDate,
                            Remark = createdCar.Remark,
                            CompanyId = createdCar.CompanyId,
                            Files = createdCar.Files.Select(f => new CarFileDto
                            {
                                Id = f.Id,
                                FileName = f.FileName,
                                OriginalFileName = f.OriginalFileName,
                                ContentType = f.ContentType,
                                UploadedAt = f.UploadedAt
                            }).ToList()
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
                    HttpContext httpContext,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    [FromQuery] int pageNumber = 1,
                    [FromQuery] int pageSize = 1000,
                    [FromQuery] string? search = null
                ) =>
                {
                    try
                    {
                        var companyIdsRaw = httpContext.Request.Query["companyIds"];
                        var companiesIds = GuidHelper.ParseGuids(companyIdsRaw, "companyIds");

                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error(
                                "User not authenticated.",
                                StatusCodes.Status401Unauthorized
                            );
                        }

                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                        List<Guid> associatedCompanyIds;

                        if (isGlobalAdmin)
                        {
                            if (!companiesIds.Any())
                            {
                                // Global admin and no companyIds provided: show all cars
                                var allCompanyIds = await db.Companies.Select(c => c.Id).ToListAsync();
                                associatedCompanyIds = allCompanyIds;
                            }
                            else
                            {
                                associatedCompanyIds = companiesIds;
                            }
                        }
                        else
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

                            associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(cpc => cpc.CompanyId)
                                .Where(id => id.HasValue)
                                .Select(id => id.Value)
                                .Distinct()
                                .ToList();

                            // New logic: use associatedCompanyIds if companiesIds is empty, otherwise check authorization
                            if (!companiesIds.Any())
                            {
                                companiesIds = associatedCompanyIds;
                            }
                            else if (companiesIds.Any(id => !associatedCompanyIds.Contains(id)))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to view cars of one or more requested companies.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                            // Non-global admin: use provided companiesIds for the query
                            associatedCompanyIds = companiesIds;
                        }

                        // Build base query with company filtering
                        var carsQuery = db.Cars
                            .Where(c => associatedCompanyIds.Contains(c.CompanyId));

                        // Optional license plate search
                        if (!string.IsNullOrWhiteSpace(search))
                        {
                            // Case-insensitive contains; use ILIKE for PostgreSQL
                            carsQuery = carsQuery.Where(c =>
                                EF.Functions.ILike(c.LicensePlate, $"%{search.Trim()}%"));
                        }

                        var totalCars = await carsQuery.CountAsync();

                        var cars = await carsQuery
                            .Include(c => c.Files)
                            .Include(c => c.Driver)
                                .ThenInclude(d => d.User)
                            .OrderBy(c => c.LicensePlate)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .Select(c => new CarDto
                            {
                                Id = c.Id,
                                LicensePlate = c.LicensePlate,
                                VehicleYear = c.VehicleYear,
                                RegistrationDate = c.RegistrationDate,
                                Remark = c.Remark,
                                CompanyId = c.CompanyId,
                                DriverId = c.Driver != null ? c.Driver.Id : null,
                                DriverFirstName = c.Driver != null && c.Driver.User != null ? c.Driver.User.FirstName : null,
                                DriverLastName = c.Driver != null && c.Driver.User != null ? c.Driver.User.LastName : null,
                                DriverEmail = c.Driver != null && c.Driver.User != null ? c.Driver.User.Email : null,
                                Files = c.Files.Select(f => new CarFileDto
                                {
                                    Id = f.Id,
                                    FileName = f.FileName,
                                    OriginalFileName = f.OriginalFileName,
                                    ContentType = f.ContentType,
                                    UploadedAt = f.UploadedAt
                                }).ToList()
                            })
                            .ToListAsync();

                        var totalPages = (int)Math.Ceiling((double)totalCars / pageSize);

                        var responseData = new
                        {
                            totalCars,
                            totalPages,
                            pageNumber,
                            pageSize,
                            cars
                        };

                        return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                    }
                    catch (ArgumentException ex)
                    {
                        return ApiResponseFactory.Error("Invalid GUID value: " + ex.Message, StatusCodes.Status400BadRequest);
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
                    ClaimsPrincipal currentUser,
                    IWebHostEnvironment env,
                    IOptions<StorageOptions> cfg,
                    IResourceLocalizer resourceLocalizer
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

                        using var transaction = await db.Database.BeginTransactionAsync();

                        // Update car fields
                        if (!string.IsNullOrWhiteSpace(request.LicensePlate))
                        {
                            car.LicensePlate = request.LicensePlate;
                        }

                        if (request.VehicleYear.HasValue)
                        {
                            car.VehicleYear = request.VehicleYear;
                        }

                        if (request.RegistrationDate.HasValue)
                        {
                            car.RegistrationDate = request.RegistrationDate;
                        }

                        if (!string.IsNullOrWhiteSpace(request.Remark))
                        {
                            car.Remark = request.Remark;
                        }

                        // Handle file operations
                        var newUploads = request.NewUploads ?? new List<UploadFileRequest>();
                        var fileIdsToDelete = request.FileIdsToDelete ?? new List<Guid>();

                        if (newUploads.Any())
                        {
                            var tmpRoot = Path.Combine(env.ContentRootPath, cfg.Value.TmpPath);
                            var finalRoot = Path.Combine(env.ContentRootPath, cfg.Value.BasePathCompanies);

                            CarFileHelper.MoveUploadsToCar(car.Id, car.CompanyId, newUploads, tmpRoot,
                                finalRoot, db, resourceLocalizer);
                        }

                        if (fileIdsToDelete.Any())
                        {
                            var finalRoot = Path.Combine(env.ContentRootPath, cfg.Value.BasePathCompanies);
                            CarFileDeleteHelper.DeleteCarFiles(car.Id, fileIdsToDelete, finalRoot, db);
                        }

                        await db.SaveChangesAsync();
                        await transaction.CommitAsync();

                        // Load updated car with files for response
                        var updatedCar = await db.Cars
                            .Include(c => c.Files)
                            .FirstOrDefaultAsync(c => c.Id == car.Id);

                        var responseData = new CarDto
                        {
                            Id = updatedCar!.Id,
                            LicensePlate = updatedCar.LicensePlate,
                            VehicleYear = updatedCar.VehicleYear,
                            RegistrationDate = updatedCar.RegistrationDate,
                            Remark = updatedCar.Remark,
                            CompanyId = updatedCar.CompanyId,
                            Files = updatedCar.Files.Select(f => new CarFileDto
                            {
                                Id = f.Id,
                                FileName = f.FileName,
                                OriginalFileName = f.OriginalFileName,
                                ContentType = f.ContentType,
                                UploadedAt = f.UploadedAt
                            }).ToList()
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
                    ClaimsPrincipal currentUser,
                    IWebHostEnvironment env,
                    IOptions<StorageOptions> cfg
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

                        // Use transaction to ensure atomicity
                        using var transaction = await db.Database.BeginTransactionAsync();

                        try
                        {
                            // 1. Get all associated files
                            var carFiles = await db.CarFiles
                                .Where(f => f.CarId == car.Id)
                                .ToListAsync();

                            // 2. Delete physical files from storage
                            if (carFiles.Any())
                            {
                                var basePath = Path.Combine(env.ContentRootPath, cfg.Value.BasePath ?? "storage");
                                
                                foreach (var carFile in carFiles)
                                {
                                    var absolutePath = Path.Combine(basePath, carFile.FilePath);
                                    if (File.Exists(absolutePath))
                                    {
                                        File.Delete(absolutePath);
                                    }
                                }

                                // 3. Delete CarFile database records
                                db.CarFiles.RemoveRange(carFiles);
                            }

                            // 4. Delete the car
                            db.Cars.Remove(car);

                            // 5. Commit all changes
                            await db.SaveChangesAsync();
                            await transaction.CommitAsync();

                            return ApiResponseFactory.Success("Car deleted successfully.", StatusCodes.Status200OK);
                        }
                        catch (Exception deleteEx)
                        {
                            await transaction.RollbackAsync();
                            Console.Error.WriteLine($"Error during car deletion transaction: {deleteEx.Message}");
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error deleting car: {ex.Message}");
                        return ApiResponseFactory.Error("An unexpected error occurred while deleting the car.",
                            StatusCodes.Status500InternalServerError);
                    }
                });
            app.MapGet("/cars/{id}",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer, customer, customerAccountant")]
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
                        bool isContactPerson = !isGlobalAdmin; // Covers all other roles in the list

                        var carQuery = db.Cars.AsQueryable();

                        if (isContactPerson)
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

                            carQuery = carQuery.Where(c => associatedCompanyIds.Contains(c.CompanyId));
                        }

                        var car = await carQuery
                            .Include(c => c.Company)
                            .Include(c => c.Files)
                            .Include(c => c.Driver)
                                .ThenInclude(d => d.User)
                            .FirstOrDefaultAsync(c => c.Id == carGuid);

                        if (car == null)
                        {
                            return ApiResponseFactory.Error("Car not found.", StatusCodes.Status404NotFound);
                        }

                        var responseData = new CarDto
                        {
                            Id = car.Id,
                            LicensePlate = car.LicensePlate,
                            VehicleYear = car.VehicleYear,
                            RegistrationDate = car.RegistrationDate,
                            Remark = car.Remark,
                            CompanyId = car.CompanyId,
                            DriverId = car.Driver?.Id,
                            DriverFirstName = car.Driver?.User?.FirstName,
                            DriverLastName = car.Driver?.User?.LastName,
                            DriverEmail = car.Driver?.User?.Email,
                            Company = new CompanyDto
                            {
                                Id = car.Company.Id,
                                Name = car.Company.Name,
                                Address = car.Company.Address,
                                Postcode = car.Company.Postcode,
                                City = car.Company.City,
                                Country = car.Company.Country,
                                PhoneNumber = car.Company.PhoneNumber,
                                Email = car.Company.Email,
                                Remark = car.Company.Remark,
                                IsApproved = car.Company.IsApproved
                            },
                            Files = car.Files.Select(f => new CarFileDto
                            {
                                Id = f.Id,
                                FileName = f.FileName,
                                OriginalFileName = f.OriginalFileName,
                                ContentType = f.ContentType,
                                UploadedAt = f.UploadedAt
                            }).ToList()
                        };

                        return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error retrieving car details: {ex.Message}");
                        return ApiResponseFactory.Error("An unexpected error occurred while fetching car details.",
                            StatusCodes.Status500InternalServerError);
                    }
                });
        }
    }
}