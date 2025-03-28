using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;

namespace TruckManagement.Helpers
{
    public static class UserCompanyHelper
    {
        public static async Task<bool> CheckUserBelongsToCompanyAsync(
            string aspNetUserId,
            Guid companyId,
            ApplicationDbContext dbContext
        )
        {
            // Check if user is a driver in that company
            var isDriverInCompany = await dbContext.Drivers
                .AnyAsync(d =>
                    d.AspNetUserId == aspNetUserId &&
                    d.CompanyId == companyId
                );
            if (isDriverInCompany) return true;

            // Or check if user is contact person in that company
            var contactPerson = await dbContext.ContactPersons
                .Include(cp => cp.ContactPersonClientCompanies)
                .FirstOrDefaultAsync(cp => cp.AspNetUserId == aspNetUserId);

            if (contactPerson != null)
            {
                var belongsToCompany = contactPerson.ContactPersonClientCompanies
                    .Any(cpc => cpc.CompanyId == companyId);

                if (belongsToCompany) return true;
            }

            return false;
        }
    }
}
