using System.ComponentModel.DataAnnotations;

namespace TruckManagement.Entities
{
    /// <summary>
    /// CAO (Collective Labor Agreement) vacation days entitlement based on age.
    /// Based on TLN Beroepsgoederenvervoer CAO, Article 67a.
    /// </summary>
    public class CAOVacationDays
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Minimum age for this bracket (inclusive)
        /// </summary>
        [Required]
        public int AgeFrom { get; set; }

        /// <summary>
        /// Maximum age for this bracket (inclusive)
        /// </summary>
        [Required]
        public int AgeTo { get; set; }

        /// <summary>
        /// Description of the age group in Dutch (e.g., "45 t/m 49 jaar")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string AgeGroupDescription { get; set; } = default!;

        /// <summary>
        /// Total vacation days per year (includes both statutory and extra days)
        /// </summary>
        [Required]
        public int VacationDays { get; set; }

        /// <summary>
        /// Statutory vacation days (wettelijke vakantiedagen) = VacationDays - 4
        /// </summary>
        public int StatutoryVacationDays
        {
            get { return VacationDays - 4; }
        }

        /// <summary>
        /// Extra vacation days (bovenwettelijke vakantiedagen) - always 4
        /// </summary>
        public int ExtraVacationDays
        {
            get { return 4; }
        }

        /// <summary>
        /// Year this vacation entitlement is effective (e.g., 2025)
        /// </summary>
        [Required]
        public int EffectiveYear { get; set; } = DateTime.UtcNow.Year;

        /// <summary>
        /// When this vacation entitlement becomes effective
        /// </summary>
        public DateTime EffectiveFrom { get; set; } = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// When this vacation entitlement expires (null = no expiration)
        /// </summary>
        public DateTime? EffectiveTo { get; set; }
    }
}

