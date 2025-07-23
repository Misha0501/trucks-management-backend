using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Entities;

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
             * 1. Resolve the active contract that covers the given date.
             *    If none -> no entitlement.
             * -----------------------------------------------------------*/
            var contract = await _db.EmployeeContracts
                .AsNoTracking()
                .Where(c => c.DriverId == driverId)
                .Where(c =>
                    (c.DateOfEmployment ?? DateTime.MinValue) <= date &&
                    (c.LastWorkingDay     ?? DateTime.MaxValue) >= date)
                .OrderByDescending(c => c.DateOfEmployment) // newest first
                .FirstOrDefaultAsync();

            if (contract is null || contract.DateOfBirth is null)
                return 0;

            /* ------------------------------------------------------------
             * 2. Determine driver’s age at 31‑Dec of the year in question
             *    and look‑up the corresponding VacationRight row.
             * -----------------------------------------------------------*/
            var yearEnd = new DateTime(date.Year, 12, 31);
            int ageAtYearEnd = GetAge(contract.DateOfBirth.Value, yearEnd);

            var vacationRight = await _db.VacationRights
                .AsNoTracking()
                .Where(vr =>
                    (vr.AgeFrom ?? 0)        <= ageAtYearEnd &&
                    (vr.AgeTo   ?? int.MaxValue) >= ageAtYearEnd &&
                    vr.StartDate              <= date &&
                    (vr.EndDate == null || vr.EndDate >= date))
                .OrderByDescending(vr => vr.StartDate)
                .FirstOrDefaultAsync();

            if (vacationRight is null)
                return 0;

            /* ------------------------------------------------------------
             * 3.  Flat accrual per *worked* weekday.
             *     ‑ Calculate the total number of weekdays in the calendar
             *       year and divide the yearly entitlement (days × 8h) by
             *       that number. Every worked weekday earns exactly that
             *       amount, regardless of when employment starts.
             * -----------------------------------------------------------*/
            var yearStart = new DateTime(date.Year, 1, 1);
            int workdaysInYear = CountWeekdays(yearStart, yearEnd);
            if (workdaysInYear == 0) return 0; // should never happen

            double hoursPerWorkday = (vacationRight.Right * 8.0) / workdaysInYear;
            
            return Math.Round(hoursPerWorkday, 2);
        }

        private static int GetAge(DateTime birthDate, DateTime onDate)
        {
            int age = onDate.Year - birthDate.Year;
            if (birthDate.Date > onDate.AddYears(-age)) age--;
            return age;
        }

        /// <summary>
        /// Counts Monday‑to‑Friday days (inclusive) between <paramref name="from"/> and <paramref name="toInclusive"/>.
        /// </summary>
        private static int CountWeekdays(DateTime from, DateTime toInclusive)
        {
            int totalDays = (toInclusive - from).Days + 1;
            return Enumerable.Range(0, totalDays)
                .Select(i => from.AddDays(i))
                .Count(d => d.DayOfWeek >= DayOfWeek.Monday && d.DayOfWeek <= DayOfWeek.Friday);
        }
    }
}