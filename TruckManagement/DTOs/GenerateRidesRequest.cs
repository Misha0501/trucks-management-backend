using System.ComponentModel.DataAnnotations;

namespace TruckManagement.DTOs
{
    public class GenerateRidesRequest
    {
        [Required]
        public DateTime WeekStartDate { get; set; }
        [Required]
        public List<DayRideGenerationDto> Days { get; set; } = new List<DayRideGenerationDto>();
    }

    public class DayRideGenerationDto
    {
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public List<ClientRideGenerationDto> Clients { get; set; } = new List<ClientRideGenerationDto>();
    }

    public class ClientRideGenerationDto
    {
        [Required]
        public Guid ClientId { get; set; }
        [Required]
        public int TrucksToGenerate { get; set; }
    }
}


