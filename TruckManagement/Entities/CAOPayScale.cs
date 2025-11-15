using System.ComponentModel.DataAnnotations;

namespace TruckManagement.Entities
{
    /// <summary>
    /// CAO (Collective Labor Agreement) pay scale table for transport workers.
    /// Based on TLN Beroepsgoederenvervoer CAO.
    /// </summary>
    public class CAOPayScale
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Pay scale letter (A, B, C, D, E, F, G, H)
        /// </summary>
        [Required]
        [MaxLength(1)]
        public string Scale { get; set; } = default!;

        /// <summary>
        /// Step within the scale (1-10, varies by scale)
        /// </summary>
        [Required]
        public int Step { get; set; }

        /// <summary>
        /// Weekly wage in euros
        /// </summary>
        [Required]
        public decimal WeeklyWage { get; set; }

        /// <summary>
        /// Four-week wage in euros
        /// </summary>
        [Required]
        public decimal FourWeekWage { get; set; }

        /// <summary>
        /// Monthly wage in euros
        /// </summary>
        [Required]
        public decimal MonthlyWage { get; set; }

        /// <summary>
        /// Base hourly wage (100%) in euros
        /// </summary>
        [Required]
        public decimal HourlyWage100 { get; set; }

        /// <summary>
        /// Overtime hourly wage (130%) in euros
        /// </summary>
        [Required]
        public decimal HourlyWage130 { get; set; }

        /// <summary>
        /// Double overtime hourly wage (150%) in euros
        /// </summary>
        [Required]
        public decimal HourlyWage150 { get; set; }

        /// <summary>
        /// Year this pay scale is effective (e.g., 2025)
        /// </summary>
        [Required]
        public int EffectiveYear { get; set; } = DateTime.UtcNow.Year;

        /// <summary>
        /// When this pay scale becomes effective
        /// </summary>
        public DateTime EffectiveFrom { get; set; } = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// When this pay scale expires (null = no expiration)
        /// </summary>
        public DateTime? EffectiveTo { get; set; }
    }
}

