using System.ComponentModel.DataAnnotations.Schema;

namespace TruckManagement.Entities
{
    public class DriverCompensationSettings
    {
        public Guid Id { get; set; }

        public Guid DriverId { get; set; }
        public Driver Driver { get; set; } = default!;

        public double PercentageOfWork { get; set; }
        public bool NightHoursAllowed { get; set; }
        public bool NightHours19Percent { get; set; }

        public decimal DriverRatePerHour { get; set; }
        public decimal NightAllowanceRate { get; set; }
        public bool KilometerAllowanceEnabled { get; set; }

        public double KilometersOneWayValue { get; set; }
        public double KilometersMin { get; set; }
        public double KilometersMax { get; set; }
        public decimal KilometerAllowance { get; set; }

        public decimal Salary4Weeks { get; set; }
        public decimal WeeklySalary { get; set; }
        public DateTime DateOfEmployment { get; set; }
    }
}