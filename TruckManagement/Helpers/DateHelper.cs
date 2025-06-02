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
    
    public static int GetIso8601WeekOfYear(DateTime time)
    {
        // This presumes that weeks start with Monday. 
        // Week 1 is the week that has at least four days in the new year.
        var day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
        if (day == DayOfWeek.Sunday)
        {
            time = time.AddDays(-1);
        }

        return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
            time,
            CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday
        );
    }

}
