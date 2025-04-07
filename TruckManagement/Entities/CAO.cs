// Entity class for CAO

namespace TruckManagement.Entities
{
    public class Cao
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public decimal StandardUntaxedAllowance { get; set; } // Normaal
        public decimal MultiDayAfter17Allowance { get; set; } // MRDna
        public decimal MultiDayBefore17Allowance { get; set; } // MRDvoor
        public decimal ShiftMoreThan12HAllowance { get; set; } // Doorbelasting
        public decimal MultiDayTaxedAllowance { get; set; } // Dbelast
        public decimal MultiDayUntaxedAllowance { get; set; } // Tussendag
        public decimal ConsignmentUntaxedAllowance { get; set; } // Onbelast
        public decimal ConsignmentTaxedAllowance { get; set; } // manually added or computed

        public int CommuteMinKilometers { get; set; }
        public int CommuteMaxKilometers { get; set; }
        public decimal KilometersAllowance { get; set; } // EUR 0.23

        public decimal NightHoursAllowanceRate { get; set; } // EUR 0.19
        public TimeSpan NightTimeStart { get; set; } // e.g. 22:00
        public TimeSpan NightTimeEnd { get; set; } // e.g. 06:00
    }
}