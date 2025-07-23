using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Helpers;

namespace TruckManagement.Services
{
    /// <summary>
    /// Calculates the vacation hours a single <see cref="PartRide"/> earns,
    /// using EmployeeContract / VacationRight data pulled from the database.
    /// </summary>
    public sealed class VacationAccrualService
    {
        private readonly ApplicationDbContext _db;

        public VacationAccrualService(ApplicationDbContext db) => _db = db;

        /// <summary>
        /// Returns the number of vacation hours earned by <paramref name="ride"/>.
        /// </summary>
        public async Task<double> GetEarnedHoursAsync(Guid driverId, DateTime date)
        {
            /* ------------------------------------------------------------
             * 1. Resolve the *active* contract: the contract whose
             *    [DateOfEmployment … LastWorkingDay] range covers ride.Date.
             * -----------------------------------------------------------*/
            var contract = await _db.EmployeeContracts
                .AsNoTracking()
                .Where(c => c.DriverId == driverId)
                .Where(c =>
                    (c.DateOfEmployment ?? DateTime.MinValue) <= date &&
                    (c.LastWorkingDay   ?? DateTime.MaxValue) >= date)
                .OrderByDescending(c => c.DateOfEmployment)   // newest first, in case overlap
                .FirstOrDefaultAsync();

            if (contract is null) return 0;                  // no contract → no entitlement

            /* ------------------------------------------------------------
             * 2. Determine driver’s *age on 31-Dec* of the ride’s year
             *    (NL vacation tables are set per calendar year).
             * -----------------------------------------------------------*/
            if (contract.DateOfBirth is null) return 0;

            var yearEnd          = new DateTime(date.Year, 12, 31);
            int ageAtYearEnd     = GetAge(contract.DateOfBirth.Value, yearEnd);

            /* ------------------------------------------------------------
             * 3. Fetch the VacationRight that matches the age range and is
             *    effective at ride.Date.
             * -----------------------------------------------------------*/
            var vacationRight = await _db.VacationRights
                .AsNoTracking()
                .Where(vr =>                             // age window
                    (vr.AgeFrom ?? 0)  <= ageAtYearEnd &&
                    (vr.AgeTo   ?? 200) >= ageAtYearEnd)
                .Where(vr =>                             // validity window
                    vr.StartDate <= date &&
                    (vr.EndDate == null || vr.EndDate >= date))
                .OrderByDescending(vr => vr.StartDate)   // newest rule wins
                .FirstOrDefaultAsync();

            if (vacationRight is null) return 0;

            /* ------------------------------------------------------------
             * 4. Compute daily accrual
             * -----------------------------------------------------------*/
            // employment window inside calendar year
            var yearStart        = new DateTime(date.Year, 1, 1);
            var yearEndInclusive = new DateTime(date.Year, 12, 31);

            var employmentStart  = (contract.DateOfEmployment ?? yearStart)
                                   .ClampToRange(yearStart, yearEndInclusive);
            var employmentEnd    = (contract.LastWorkingDay   ?? yearEndInclusive)
                                   .ClampToRange(yearStart, yearEndInclusive);

            if (date < employmentStart || date > employmentEnd)
                return 0;

            int entitledDaysInYear = CountWorkdays(employmentStart, employmentEnd);
            if (entitledDaysInYear == 0) return 0;

            double hoursPerEntitledDay = vacationRight.Right * 8.0 / entitledDaysInYear;

            return Math.Round(hoursPerEntitledDay, 2);
        }

        private static int GetAge(DateTime birthDate, DateTime onDate)
        {
            int age = onDate.Year - birthDate.Year;
            if (birthDate.Date > onDate.AddYears(-age)) age--;
            return age;
        }
        
        private static int CountWorkdays(DateTime from, DateTime toInclusive)
        {
            int days = (toInclusive - from).Days + 1;

            return Enumerable.Range(0, days)
                .Select(i => from.AddDays(i))
                .Count(d => d.DayOfWeek >= DayOfWeek.Monday && d.DayOfWeek <= DayOfWeek.Friday);
        }
    }
}