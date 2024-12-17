using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TruckManagement.Data;
using TruckManagement.Entities;

namespace TruckManagement.Extensions
{
    public static class IdentityServiceCollectionExtensions
    {
        public static IServiceCollection AddAppIdentity(this IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
                {
                    // Configure identity options here if needed
                    options.Password.RequireDigit = false;
                    options.Password.RequireUppercase = false;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            return services;
        }
    }
}