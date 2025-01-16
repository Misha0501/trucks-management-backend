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
        app.MapPost("/register", async (
                RegisterRequest req,
                UserManager<ApplicationUser> userManager,
                RoleManager<ApplicationRole> roleManager,
                ApplicationDbContext dbContext) =>
            {
                // 1) Validate if 'companyId' is a correct GUID
                if (!Guid.TryParse(req.CompanyId, out var parsedCompanyId))
                {
                    return ApiResponseFactory.Error(
                        "The provided Company ID is not a valid GUID.",
                        StatusCodes.Status400BadRequest
                    );
                }

                // 2) Check if the company exists
                var companyExists = await dbContext.Companies.AnyAsync(c => c.Id == parsedCompanyId);
                if (!companyExists)
                {
                    return ApiResponseFactory.Error(
                        "The specified company does not exist. Please provide a valid Company ID.",
                        StatusCodes.Status400BadRequest
                    );
                }

                // 3) Check if password + confirm match
                if (req.Password != req.ConfirmPassword)
                {
                    return ApiResponseFactory.Error(
                        "Password and confirmation do not match.",
                        StatusCodes.Status400BadRequest
                    );
                }

                // 4) Create the user
                var user = new ApplicationUser
                {
                    UserName = req.Email,
                    Email = req.Email,
                    FirstName = req.FirstName,
                    LastName = req.LastName,
                    CompanyId = parsedCompanyId,
                    Address = req.Address,
                    PhoneNumber = req.PhoneNumber,
                    Postcode = req.Postcode,
                    City = req.City,
                    Country = req.Country,
                    Remark = req.Remark
                };

                // 5) Actually create the user in Identity
                var result = await userManager.CreateAsync(user, req.Password);
                if (!result.Succeeded)
                {
                    var errorMessages = result.Errors.Select(e => e.Description).ToList();
                    return ApiResponseFactory.Error(errorMessages, StatusCodes.Status400BadRequest);
                }

                // 6) If roles are provided, verify they're valid and assign them
                if (req.Roles != null && req.Roles.Count > 0)
                {
                    // Validate each role exists
                    foreach (var role in req.Roles)
                    {
                        if (!await roleManager.RoleExistsAsync(role))
                        {
                            // Rollback: delete the created user if role is invalid
                            await userManager.DeleteAsync(user);
                            return ApiResponseFactory.Error(
                                $"The specified role '{role}' does not exist. User was not created.",
                                StatusCodes.Status400BadRequest
                            );
                        }
                    }

                    // Assign roles to the new user
                    var roleResult = await userManager.AddToRolesAsync(user, req.Roles);
                    if (!roleResult.Succeeded)
                    {
                        // Rollback: remove user if role assignment fails
                        await userManager.DeleteAsync(user);
                        var errMsgs = roleResult.Errors.Select(e => e.Description).ToList();
                        return ApiResponseFactory.Error(errMsgs, StatusCodes.Status400BadRequest);
                    }
                }
                // If roles array is null or empty, no roles will be assigned, which is acceptable.

                // 7) Success
                return ApiResponseFactory.Success(
                    "User registered successfully.",
                    StatusCodes.Status200OK
                );
            })
            .RequireAuthorization("GlobalAdminOnly");


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