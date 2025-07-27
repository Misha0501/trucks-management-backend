namespace TruckManagement.DTOs.Reports;

public class ReportTimeframe
{
    public Guid DriverId { get; set; }
    public int Year { get; set; }
    public ReportType Type { get; set; }
    public int? WeekNumber { get; set; }
    public int? PeriodNumber { get; set; }
    
    public static ReportTimeframe ForWeek(Guid driverId, int year, int weekNumber)
    {
        return new ReportTimeframe
        {
            DriverId = driverId,
            Year = year,
            Type = ReportType.SingleWeek,
            WeekNumber = weekNumber
        };
    }
    
    public static ReportTimeframe ForPeriod(Guid driverId, int year, int periodNumber)
    {
        return new ReportTimeframe
        {
            DriverId = driverId,
            Year = year,
            Type = ReportType.FullPeriod,
            PeriodNumber = periodNumber
        };
    }
    
    public List<int> GetWeekNumbers()
    {
        if (Type == ReportType.SingleWeek && WeekNumber.HasValue)
        {
            return new List<int> { WeekNumber.Value };
        }
        
        if (Type == ReportType.FullPeriod && PeriodNumber.HasValue)
        {
            // Period contains 4 weeks: calculate week numbers
            var startWeek = (PeriodNumber.Value - 1) * 4 + 1;
            return new List<int> { startWeek, startWeek + 1, startWeek + 2, startWeek + 3 };
        }
        
        return new List<int>();
    }
    
    public string GetPeriodRange()
    {
        if (Type == ReportType.SingleWeek && WeekNumber.HasValue)
        {
            return $"week {WeekNumber.Value}";
        }
        
        if (Type == ReportType.FullPeriod && PeriodNumber.HasValue)
        {
            var weeks = GetWeekNumbers();
            return $"week {weeks.First()} t/m {weeks.Last()}";
        }
        
        return "";
    }
}

public enum ReportType
{
    SingleWeek,
    FullPeriod
} 