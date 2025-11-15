using System.ComponentModel.DataAnnotations;

namespace TruckManagement.DTOs;

public class CreateEmployeeContractRequest
{
    public string? DriverId { get; set; }
    public string CompanyId { get; set; } = default!;
    public decimal? ReleaseVersion { get; set; }
    public bool NightHoursAllowed { get; set; }
    public bool KilometersAllowanceAllowed { get; set; }
    public double CommuteKilometers { get; set; }

    public string EmployeeFirstName { get; set; } = default!;
    public string EmployeeLastName { get; set; } = default!;
    public string EmployeeAddress { get; set; } = default!;
    public string EmployeePostcode { get; set; } = default!;
    public string EmployeeCity { get; set; } = default!;
    public DateTime DateOfBirth { get; set; }
    public string Bsn { get; set; } = default!;
    public string? Iban { get; set; }
    public DateTime DateOfEmployment { get; set; }
    public DateTime? LastWorkingDay { get; set; }

    public string Function { get; set; } = default!;
    public string? ProbationPeriod { get; set; }
    public double WorkweekDuration { get; set; }
    public string WeeklySchedule { get; set; } = default!;
    public string WorkingHours { get; set; } = default!;
    public string NoticePeriod { get; set; } = default!;
    public decimal CompensationPerMonthExclBtw { get; set; }
    public decimal CompensationPerMonthInclBtw { get; set; }
    public string PayScale { get; set; } = default!;
    public int PayScaleStep { get; set; }
    public decimal HourlyWage100Percent { get; set; }
    public decimal DeviatingWage { get; set; }
    public decimal TravelExpenses { get; set; }
    public decimal MaxTravelExpenses { get; set; }
    public int VacationAge { get; set; }
    public int VacationDays { get; set; }
    public decimal Atv { get; set; }
    public decimal VacationAllowance { get; set; }

    public string CompanyName { get; set; } = default!;
    public string EmployerName { get; set; } = default!;
    public string CompanyAddress { get; set; } = default!;
    public string CompanyPostcode { get; set; } = default!;
    public string CompanyCity { get; set; } = default!;
    public string CompanyPhoneNumber { get; set; } = default!;
    public string CompanyBtw { get; set; } = default!;
    public string CompanyKvk { get; set; } = default!;
}
