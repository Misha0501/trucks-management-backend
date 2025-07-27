namespace TruckManagement.DTOs.Reports;

public class DriverTimesheetReport 
{
    // Header Section
    public string CompanyName { get; set; } = default!;
    public string PersonnelId { get; set; } = default!;
    public string DriverName { get; set; } = default!;
    public int Year { get; set; }
    public int PeriodNumber { get; set; }
    public string PeriodRange { get; set; } = default!; // "week 1 t/m 4"
    
    // Employee Info Section
    public EmployeeInfoSection EmployeeInfo { get; set; } = default!;
    
    // Hours Summary Section  
    public HoursSummarySection HoursSummary { get; set; } = default!;
    
    // Vacation Section
    public VacationSection Vacation { get; set; } = default!;
    
    // Time-for-Time Section
    public TvTSection TimeForTime { get; set; } = default!;
    
    // Weekly Breakdown
    public List<WeeklyBreakdown> Weeks { get; set; } = new List<WeeklyBreakdown>();
    
    // Totals
    public TotalSection GrandTotal { get; set; } = default!;
}

public class EmployeeInfoSection
{
    public string EmploymentType { get; set; } = default!; // "fultime"
    public double EmploymentPercentage { get; set; } // 100%
    public DateTime? BirthDate { get; set; }
    public DateTime? EmploymentStartDate { get; set; }
    public DateTime? EmploymentEndDate { get; set; }
    public double CommuteKilometers { get; set; }
}

public class HoursSummarySection
{
    public double Hours100 { get; set; }
    public double Hours130 { get; set; }
    public double Hours150 { get; set; }
    public double Hours200 { get; set; }
    public double NightAllowance19Percent { get; set; }
    public decimal TotalNightAllowanceAmount { get; set; }
}

public class VacationSection
{
    public double AnnualEntitlementHours { get; set; } // uren jaar tegoed
    public double HoursUsed { get; set; } // opgenomen uren
    public double HoursRemaining { get; set; } // restant uren
    public double TotalVacationDays { get; set; } // totaal vakantie dagen
}

public class TvTSection
{
    public double SavedTvTHours { get; set; } // gespaarde tvt uren
    public double ConvertedTvTHours { get; set; } // omgerekede tvt uren
    public double UsedTvTHours { get; set; } // opgenomen tvt uren
    public double MonthEndTvTHours { get; set; } // einde v/d maand tvt uren
}

public class WeeklyBreakdown
{
    public int WeekNumber { get; set; }
    public List<DailyEntry> Days { get; set; } = new List<DailyEntry>();
    public WeeklyTotal WeekTotal { get; set; } = default!;
}

public class DailyEntry 
{
    public int WeekNumber { get; set; }
    public string DayName { get; set; } = default!;
    public DateTime Date { get; set; }
    public string ServiceCode { get; set; } = default!;
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public TimeSpan? BreakTime { get; set; }
    public double Corrections { get; set; }
    public double TotalHours { get; set; }
    
    // Hour Categories
    public double Hours100 { get; set; }
    public double Hours130 { get; set; }
    public double Hours150 { get; set; }
    public double Hours200 { get; set; }
    
    // Allowances
    public decimal AccommodationAllowance { get; set; } // verblijfskostenvergoeding
    public decimal TravelAllowance { get; set; } // woonwerkvergoeding
    public decimal ConsignmentFee { get; set; } // Consignatie vergoeding
    public decimal VariousCompensation { get; set; } // Diversevergoedingen
    public decimal TaxFreeAmount { get; set; } // onbelast
    public decimal TaxableAmount { get; set; } // belast
    public decimal NightAllowanceAmount { get; set; } // Nacht
    public double Kilometers { get; set; } // KM
    public decimal KilometerAllowance { get; set; } // km vergoeding
    public decimal DiverseAllowance { get; set; } // div. vergoeding
    public double TvTHours { get; set; } // tvt uren
    public string Remarks { get; set; } = default!; // Toelichting
}

public class WeeklyTotal
{
    public double TotalHours { get; set; }
    public double Hours100 { get; set; }
    public double Hours130 { get; set; }
    public double Hours150 { get; set; }
    public double Hours200 { get; set; }
    public decimal TotalAllowances { get; set; }
    public decimal TaxFreeAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal NightAllowanceAmount { get; set; }
    public double TotalKilometers { get; set; }
    public decimal KilometerAllowance { get; set; }
    public decimal DiverseAllowance { get; set; }
    public double TvTHours { get; set; }
}

public class TotalSection
{
    public double TotalHours { get; set; }
    public double Hours100 { get; set; }
    public double Hours130 { get; set; }
    public double Hours150 { get; set; }
    public double Hours200 { get; set; }
    public decimal TotalAllowances { get; set; }
    public decimal TaxFreeAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal NightAllowanceAmount { get; set; }
    public double TotalKilometers { get; set; }
    public decimal KilometerAllowance { get; set; }
    public decimal DiverseAllowance { get; set; }
    public double TvTHours { get; set; }
} 