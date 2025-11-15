namespace TruckManagement.DTOs;

public class UpdateEmployeeContractRequest
{
    public string? CompanyId { get; set; }
    public string? DriverId { get; set; }

    public bool NightHoursAllowed { get; set; }
    public bool KilometersAllowanceAllowed { get; set; }
    public double CommuteKilometers { get; set; }

    public string EmployeeFirstName { get; set; } = null!;
    public string EmployeeLastName { get; set; } = null!;
    public string EmployeeAddress { get; set; } = null!;
    public string EmployeePostcode { get; set; } = null!;
    public string EmployeeCity { get; set; } = null!;

    public DateTime DateOfBirth { get; set; }
    public string Bsn { get; set; } = null!;
    public string? Iban { get; set; }
    public DateTime DateOfEmployment { get; set; }
    public DateTime? LastWorkingDay { get; set; }

    public string Function { get; set; } = null!;
    public string? ProbationPeriod { get; set; }
    public double WorkweekDuration { get; set; }
    public double WorkweekDurationPercentage { get; set; }
    public string WeeklySchedule { get; set; } = null!;
    public string WorkingHours { get; set; } = null!;
    public string NoticePeriod { get; set; } = null!;

    public decimal CompensationPerMonthExclBtw { get; set; }
    public decimal CompensationPerMonthInclBtw { get; set; }

    public string PayScale { get; set; } = null!;
    public int PayScaleStep { get; set; }

    public decimal HourlyWage100Percent { get; set; }
    public decimal DeviatingWage { get; set; }

    public decimal TravelExpenses { get; set; }
    public decimal MaxTravelExpenses { get; set; }

    public int VacationAge { get; set; }
    public int VacationDays { get; set; }
    public decimal Atv { get; set; }
    public decimal VacationAllowance { get; set; }

    public string CompanyName { get; set; } = null!;
    public string EmployerName { get; set; } = null!;
    public string CompanyAddress { get; set; } = null!;
    public string CompanyPostcode { get; set; } = null!;
    public string CompanyCity { get; set; } = null!;
    public string CompanyPhoneNumber { get; set; } = null!;
    public string CompanyBtw { get; set; } = null!;
    public string CompanyKvk { get; set; } = null!;
}