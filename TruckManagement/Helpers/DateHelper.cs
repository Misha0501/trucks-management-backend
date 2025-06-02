using System.Globalization;

namespace TruckManagement.Helpers;

public static class DateHelper
{
    /// <summary>
    /// 13 periods per year, 4 ISO-weeks each (week 1-4 ⇒ P1, 5-8 ⇒ P2 … 49-52 ⇒ P13).
    /// </summary>
    public static (int year, int periodNr, int weekNrInPeriod) GetPeriod(DateTime dateUtc)
    {
        var weekIso   = GetIso8601WeekOfYear(dateUtc);       // 1-52 / 53
        var period    = (int)Math.Ceiling(weekIso / 4.0);     // 1-13
        var weekInPer = weekIso - (period - 1) * 4;           // 1-4

        // ISO weeks 52/53 that fall in January actually belong to previous year’s period-13.
        var periodYear = weekIso >= 52 && dateUtc.Month == 1
            ? dateUtc.Year - 1
            : dateUtc.Year;

        return (periodYear, period, weekInPer);
    }
    
    public static int GetIso8601WeekOfYear(DateTime date)
    {
        // ISO 8601: Week starts on Monday, and the first week has at least 4 days
        var day = (int)date.DayOfWeek;
        if (day == 0) day = 7; // Sunday → 7

        // Adjust date to Thursday of the current week
        var thursday = date.AddDays(4 - day);

        // Get the first Thursday of the year
        var firstThursday = new DateTime(thursday.Year, 1, 4);

        day = (int)firstThursday.DayOfWeek;
        if (day == 0) day = 7;

        var week1 = firstThursday.AddDays(-day + 1);

        return (thursday - week1).Days / 7 + 1;
    }
    
    public static int GetWeekNumberOfPeriod(int year, int period, int weekNrInPeriod)
    {
        return (period - 1) * 4 + weekNrInPeriod;
    }
    
    public static (DateTime fromDate, DateTime toDate) GetPeriodDateRange(int year, int period)
    {
        // Step 1: Get ISO week 1 start date (Monday)
        var jan4 = new DateTime(year, 1, 4); // Jan 4 is always in ISO week 1
        int daysToMonday = DayOfWeek.Monday - jan4.DayOfWeek;
        var week1Start = jan4.AddDays(daysToMonday);

        // Step 2: Offset to start of the given period
        var fromDate = week1Start.AddDays((period - 1) * 7 * 4);
        var toDate = fromDate.AddDays(27); // 4 weeks = 28 days

        return (fromDate, toDate);
    }

}
