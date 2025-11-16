using TruckManagement.Entities;

namespace TruckManagement.Services.Reports;

public class OvertimeClassifier
{
    public OvertimeBreakdown ClassifyHours(
        PartRide partRide, 
        double weeklyTotalHours,
        bool isHoliday,
        bool isNightShift) =>
        ClassifyHoursInternal(
            partRide.DecimalHours ?? 0,
            partRide.Date.DayOfWeek,
            weeklyTotalHours,
            isHoliday,
            isNightShift);

    public OvertimeBreakdown ClassifyHours(
        RideDriverExecution execution,
        double weeklyTotalHours,
        bool isHoliday,
        bool isNightShift) =>
        ClassifyHoursInternal(
            (double)(execution.DecimalHours ?? 0m),
            execution.Ride?.PlannedDate?.DayOfWeek ?? DayOfWeek.Monday,
            weeklyTotalHours,
            isHoliday,
            isNightShift);

    private OvertimeBreakdown ClassifyHoursInternal(
        double totalHours,
        DayOfWeek dayOfWeek,
        double weeklyTotalHours, 
        bool isHoliday,
        bool isNightShift)
    {
        var breakdown = new OvertimeBreakdown();
        
        // Rule 4: Work on Sunday/Holidays/Nights → 200%
        if (dayOfWeek == DayOfWeek.Sunday || isHoliday || isNightShift)
        {
            breakdown.Premium200 = totalHours;
            return breakdown;
        }
        
        // Rule 3: Daily hours > 10 OR total week hours > 40 → 150%
        if (totalHours > 10 || weeklyTotalHours > 40)
        {
            // If weekly hours exceed 40, the excess goes to 150%
            if (weeklyTotalHours > 40)
            {
                var previousWeeklyHours = weeklyTotalHours - totalHours;
                if (previousWeeklyHours >= 40)
                {
                    // All hours for this day are 150%
                    breakdown.Overtime150 = totalHours;
                }
                else
                {
                    // Split: some hours at regular/130%, excess at 150%
                    var regularHours = 40 - previousWeeklyHours;
                    breakdown.Overtime150 = totalHours - regularHours;
                    
                    // Apply daily rules to remaining hours
                    var dailyBreakdown = ClassifyDailyHours(regularHours);
                    breakdown.Regular100 = dailyBreakdown.Regular100;
                    breakdown.Overtime130 = dailyBreakdown.Overtime130;
                }
            }
            else if (totalHours > 10)
            {
                // Hours over 10 go to 150%
                breakdown.Overtime150 = totalHours - 10;
                breakdown.Regular100 = 8;
                breakdown.Overtime130 = 2; // Hours 8-10
            }
            
            return breakdown;
        }
        
        // Rules 1 & 2: Apply daily hour classification
        return ClassifyDailyHours(totalHours);
    }
    
    private OvertimeBreakdown ClassifyDailyHours(double totalHours)
    {
        var breakdown = new OvertimeBreakdown();
        
        if (totalHours <= 8)
        {
            // Rule 1: Daily hours ≤ 8 → 100%
            breakdown.Regular100 = totalHours;
        }
        else if (totalHours <= 10)
        {
            // Rule 2: Daily hours > 8 and ≤ 10 → 130%
            breakdown.Regular100 = 8;
            breakdown.Overtime130 = totalHours - 8;
        }
        else
        {
            // This case is handled in the main method (Rule 3)
            breakdown.Regular100 = 8;
            breakdown.Overtime130 = 2;
            breakdown.Overtime150 = totalHours - 10;
        }
        
        return breakdown;
    }
    
    public bool IsNightShift(PartRide partRide)
    {
        // Consider it a night shift if it has night allowance or starts/ends during night hours
        if (partRide.NightAllowance > 0)
            return true;
            
        // Check if work starts before 6 AM or ends after 10 PM
        var start = partRide.Start.TotalHours;
        var end = partRide.End.TotalHours;
        
        // Handle shifts crossing midnight
        if (end < start)
        {
            // Shift crosses midnight - likely a night shift
            return true;
        }
        
        // Night hours: 22:00 - 06:00
        return start < 6.0 || end > 22.0 || start >= 22.0;
    }

    public bool IsNightShift(RideDriverExecution execution)
    {
        if ((execution.NightAllowance ?? 0) > 0)
            return true;

        var startTime = execution.ActualStartTime ?? execution.Ride?.PlannedStartTime;
        var endTime = execution.ActualEndTime ?? execution.Ride?.PlannedEndTime;

        if (startTime is null || endTime is null)
            return false;

        var start = startTime.Value.TotalHours;
        var end = endTime.Value.TotalHours;

        if (end < start)
        {
            return true;
        }

        return start < 6.0 || end > 22.0 || start >= 22.0;
    }
}

public class OvertimeBreakdown
{
    public double Regular100 { get; set; }
    public double Overtime130 { get; set; }
    public double Overtime150 { get; set; }
    public double Premium200 { get; set; }
    
    public double TotalHours => Regular100 + Overtime130 + Overtime150 + Premium200;
    
    public void Add(OvertimeBreakdown other)
    {
        Regular100 += other.Regular100;
        Overtime130 += other.Overtime130;
        Overtime150 += other.Overtime150;
        Premium200 += other.Premium200;
    }
} 