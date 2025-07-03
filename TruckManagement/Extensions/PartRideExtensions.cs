using TruckManagement.Entities;
using TruckManagement.Services;

namespace TruckManagement.Extensions;

public static class PartRideExtensions
{
    public static void ApplyCalculated(this PartRide pr, PartRideCalculationResult r)
    {
        pr.DecimalHours           = r.DecimalHours;
        pr.NumberOfHours          = r.NumberOfHours;
        pr.TaxFreeCompensation    = r.TaxFreeCompensation;
        pr.NightAllowance         = r.NightAllowance;
        pr.KilometerReimbursement = r.KilometerReimbursement;
        pr.ConsignmentFee         = r.ConsignmentFee;
        pr.SaturdayHours          = r.SaturdayHours;
        pr.SundayHolidayHours     = r.SundayHolidayHours;
        pr.RestCalculated         = r.RestCalculated;
        pr.PeriodNumber           = r.PeriodNumber;
        pr.WeekNrInPeriod         = r.WeekNrInPeriod;
    }
}