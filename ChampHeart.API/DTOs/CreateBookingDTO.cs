using System.ComponentModel.DataAnnotations;

namespace ChampHeart.API.DTOs
{
    public class CreateBookingDTO
    {
        [Range(1, int.MaxValue,
        ErrorMessage = "Please select a valid resource.")]
        public int ResourceId { get; set; }

        [Required]
        public DateTimeOffset StartTime { get; set; }

        [Required]
        public DateTimeOffset EndTime { get; set; }
    }
}
