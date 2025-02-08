using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;
using TruckManagement.Helpers;
using TruckManagement.Services;

namespace TruckManagement.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/register", [Authorize(Roles = "globalAdmin")] async (
            RegisterRequest req,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext dbContext
        ) =>
        {
            // 1) Check password vs. confirmation
            if (req.Password != req.ConfirmPassword)
            {
                return ApiResponseFactory.Error(
                    "Password and confirmation do not match.",
                    StatusCodes.Status400BadRequest
                );
            }

            // 2) Create the base ApplicationUser
            var user = new ApplicationUser
            {
                UserName = req.Email,
                Email = req.Email,
                FirstName = req.FirstName,
                LastName = req.LastName,
                Address = req.Address,
                PhoneNumber = req.PhoneNumber,
                Postcode = req.Postcode,
                City = req.City,
                Country = req.Country,
                Remark = req.Remark
            };

            var createUserResult = await userManager.CreateAsync(user, req.Password);
            if (!createUserResult.Succeeded)
            {
                var errors = createUserResult.Errors.Select(e => e.Description).ToList();
                return ApiResponseFactory.Error(errors, StatusCodes.Status400BadRequest);
            }

            // 3) If no roles specified, rollback and return error
            if (req.Roles == null || !req.Roles.Any())
            {
                await userManager.DeleteAsync(user);
                return ApiResponseFactory.Error(
                    "No roles specified. User was not created.",
                    StatusCodes.Status400BadRequest
                );
            }

            // 4) If roles are provided, verify and assign
            foreach (var role in req.Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    // Rollback user creation
                    await userManager.DeleteAsync(user);
                    return ApiResponseFactory.Error(
                        $"Specified role '{role}' does not exist. User was not created.",
                        StatusCodes.Status400BadRequest
                    );
                }
            }

            var assignRolesResult = await userManager.AddToRolesAsync(user, req.Roles);
            if (!assignRolesResult.Succeeded)
            {
                await userManager.DeleteAsync(user);
                var roleErrors = assignRolesResult.Errors.Select(e => e.Description).ToList();
                return ApiResponseFactory.Error(roleErrors, StatusCodes.Status400BadRequest);
            }

            // 5) Determine if user is driver or contact person
            bool isDriver = req.Roles != null && req.Roles.Contains("driver");
            bool isContactPerson = !isDriver; // If not driver => contact person

            // 6) If user is driver => single company from the first CompanyIds (if any).
            if (isDriver)
            {
                Guid? driverCompanyGuid = null;

                if (req.CompanyIds != null && req.CompanyIds.Any())
                {
                    // Take the first company in the list
                    var firstCompanyIdStr = req.CompanyIds.First();
                    if (!Guid.TryParse(firstCompanyIdStr, out var parsedGuid))
                    {
                        // Invalid GUID -> delete user & abort
                        await userManager.DeleteAsync(user);
                        return ApiResponseFactory.Error(
                            $"Invalid company ID '{firstCompanyIdStr}'. User was not created.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    // Check existence
                    bool companyExists = await dbContext.Companies.AnyAsync(c => c.Id == parsedGuid);
                    if (!companyExists)
                    {
                        // Not found -> delete user & abort
                        await userManager.DeleteAsync(user);
                        return ApiResponseFactory.Error(
                            $"Company ID '{parsedGuid}' does not exist. User was not created.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    driverCompanyGuid = parsedGuid; // valid
                }

                // Create the Driver (null if no company was provided or parse failed)
                var driverEntity = new Driver
                {
                    Id = Guid.NewGuid(),
                    AspNetUserId = user.Id,
                    CompanyId = driverCompanyGuid
                };
                dbContext.Drivers.Add(driverEntity);
            }

            // 7) If user is contact person => multiple companies + multiple clients
            if (isContactPerson)
            {
                // Create contact person entity
                var contactPerson = new ContactPerson
                {
                    Id = Guid.NewGuid(),
                    AspNetUserId = user.Id
                };
                dbContext.ContactPersons.Add(contactPerson);

                // A) Link contact person to multiple companies
                if (req.CompanyIds != null && req.CompanyIds.Any())
                {
                    foreach (var compStr in req.CompanyIds)
                    {
                        if (!Guid.TryParse(compStr, out var compGuid))
                        {
                            // parse fail -> delete user & abort
                            await userManager.DeleteAsync(user);
                            return ApiResponseFactory.Error(
                                $"Invalid company ID '{compStr}'. User was not created.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        var companyExists = await dbContext.Companies.AnyAsync(c => c.Id == compGuid);
                        if (!companyExists)
                        {
                            // not found -> delete user & abort
                            await userManager.DeleteAsync(user);
                            return ApiResponseFactory.Error(
                                $"Company ID '{compGuid}' does not exist. User was not created.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        // Create bridging row with only CompanyId
                        var cpcForCompany = new ContactPersonClientCompany
                        {
                            Id = Guid.NewGuid(),
                            ContactPersonId = contactPerson.Id,
                            CompanyId = compGuid,
                            ClientId = null // no client in this row
                        };
                        dbContext.ContactPersonClientCompanies.Add(cpcForCompany);
                    }
                }

                // B) Link contact person to multiple clients
                if (req.ClientIds != null && req.ClientIds.Any())
                {
                    foreach (var clientStr in req.ClientIds)
                    {
                        if (!Guid.TryParse(clientStr, out var clientGuid))
                        {
                            // parse fail -> delete user & abort
                            await userManager.DeleteAsync(user);
                            return ApiResponseFactory.Error(
                                $"Invalid client ID '{clientStr}'. User was not created.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        var clientExists = await dbContext.Clients.AnyAsync(cl => cl.Id == clientGuid);
                        if (!clientExists)
                        {
                            // not found -> delete user & abort
                            await userManager.DeleteAsync(user);
                            return ApiResponseFactory.Error(
                                $"Client ID '{clientGuid}' does not exist. User was not created.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        // Create bridging row with only ClientId
                        var cpcForClient = new ContactPersonClientCompany
                        {
                            Id = Guid.NewGuid(),
                            ContactPersonId = contactPerson.Id,
                            CompanyId = null, // no company in this row
                            ClientId = clientGuid
                        };
                        dbContext.ContactPersonClientCompanies.Add(cpcForClient);
                    }
                }
            }

            // 8) Save domain-level changes
            await dbContext.SaveChangesAsync();

            // 9) Return success
            return ApiResponseFactory.Success(
                "User registered successfully.",
                StatusCodes.Status200OK
            );
        });


        app.MapPost("/login", async (
            LoginRequest req,
            UserManager<ApplicationUser> userManager,
            IConfiguration config) =>
        {
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user == null)
            {
                return ApiResponseFactory.Error("Invalid credentials.", StatusCodes.Status400BadRequest);
            }

            var isCorrectPassword = await userManager.CheckPasswordAsync(user, req.Password);
            if (!isCorrectPassword)
            {
                return ApiResponseFactory.Error("Invalid credentials.", StatusCodes.Status400BadRequest);
            }

            // 1) Get the roles for this user
            var roles = await userManager.GetRolesAsync(user);

            // 2) Pass the roles to GenerateJwtToken
            var token = JwtTokenHelper.GenerateJwtToken(user, roles, config);
            var data = new { token };

            return ApiResponseFactory.Success(data, StatusCodes.Status200OK);
        });


        app.MapPost("/forgotpassword", async (
            ForgotPasswordRequest req,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IConfiguration config) =>
        {
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user == null)
            {
                // Return success message either way, so we don't reveal emails
                return ApiResponseFactory.Success(
                    "If that email exists, a reset link has been sent."
                );
            }

            // Generate a password reset token
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

            // Build the reset URL for your front-end or a dedicated route
            // e.g. https://my-frontend.com/reset-password?email=...&token=...
            var frontEndUrl = config["FrontEnd:ResetPasswordUrl"]
                              ?? "https://my-frontend.com/reset-password";

            // It's a good idea to URL-encode the token to avoid special character issues
            var resetLink = $"{frontEndUrl}?email={Uri.EscapeDataString(user.Email)}" +
                            $"&token={Uri.EscapeDataString(resetToken)}";

            // Create the email content
            var subject = "Password Reset Request";
            var body = $@"
                <p>You requested a password reset.</p>
                <p>Please click the link below (or copy it into your browser) to reset your password:</p>
                <a href=""{resetLink}"">{resetLink}</a>
                <p>If you did not request a password reset, you can ignore this message.</p>
            ";

            // Send the email
            await emailService.SendEmailAsync(user.Email, subject, body);

            // Return a success response (don't expose the token)
            return ApiResponseFactory.Success("If that email exists, a reset link has been sent.");
        });

        // 2) Reset Password Endpoint
        app.MapPost("/reset-password-token", async (
            ResetPasswordRequest req,
            UserManager<ApplicationUser> userManager) =>
        {
            // Check if new password matches the confirmation
            if (req.NewPassword != req.ConfirmPassword)
            {
                return ApiResponseFactory.Error("New password and confirmation do not match.");
            }

            // Find user
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user == null)
            {
                return ApiResponseFactory.Error("No user found with that email.");
            }

            // Reset the password
            var result = await userManager.ResetPasswordAsync(user, req.Token, req.NewPassword);
            if (!result.Succeeded)
            {
                // Gather all identity errors
                var errorMessages = result.Errors.Select(e => e.Description).ToList();
                return ApiResponseFactory.Error(errorMessages);
            }

            return ApiResponseFactory.Success("Password reset successful. You can now log in with your new password.");
        });

        return app;
    }
}