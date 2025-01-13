using Microsoft.Extensions.DependencyInjection;

namespace TruckManagement.Extensions
{
    public static class AuthorizationPolicies
    {
        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // Employee
                options.AddPolicy("EmployeeOnly", policy =>
                {
                    policy.RequireRole("employee");
                });

                // Employer
                options.AddPolicy("EmployerOnly", policy =>
                {
                    policy.RequireRole("employer");
                });

                // Customer
                options.AddPolicy("CustomerOnly", policy =>
                {
                    policy.RequireRole("customer");
                });

                // CustomerAdmin
                options.AddPolicy("CustomerAdminOnly", policy =>
                {
                    policy.RequireRole("customerAdmin");
                });

                // CustomerAccountant
                options.AddPolicy("CustomerAccountantOnly", policy =>
                {
                    policy.RequireRole("customerAccountant");
                });

                // GlobalAdmin
                options.AddPolicy("GlobalAdminOnly", policy =>
                {
                    policy.RequireRole("globalAdmin");
                });
            });

            return services;
        }
    }
}