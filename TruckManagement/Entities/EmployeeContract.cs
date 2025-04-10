using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TruckManagement.Entities
{
    public class EmployeeContract
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? DriverId { get; set; }
        public Guid? CompanyId { get; set; }

        public decimal? ReleaseVersion { get; set; }

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
        public DateTime DateOfEmployment { get; set; }
        public DateTime? LastWorkingDay { get; set; }

        public string Function { get; set; } = null!;
        public string? ProbationPeriod { get; set; }
        public double WorkweekDuration { get; set; } // e.g. 40
        public double WorkweekDurationPercentage { get; set; } // e.g. 100
        public string WeeklySchedule { get; set; } = null!; // e.g. Monday-Friday
        public string WorkingHours { get; set; } = null!; // e.g. 07:00 - 19:00
        public string NoticePeriod { get; set; } = null!; // e.g. 1 month

        public decimal CompensationPerMonthExclBtw { get; set; }
        public decimal CompensationPerMonthInclBtw { get; set; }

        public string PayScale { get; set; } = null!; // e.g. D
        public int PayScaleStep { get; set; }

        public decimal HourlyWage100Percent { get; set; }
        public decimal DeviatingWage { get; set; }

        public decimal TravelExpenses { get; set; } // e.g. 0.23
        public decimal MaxTravelExpenses { get; set; } // e.g. 100.00

        public int VacationAge { get; set; } // e.g. 44
        public int VacationDays { get; set; } // e.g. 28
        public decimal Atv { get; set; } // e.g. 3.5
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
}
