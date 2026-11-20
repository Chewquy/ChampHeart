using System.ComponentModel.DataAnnotations;

namespace ChampHeart.API.Models;

public class Booking
{
    public int Id { get; set; }

    [Required]
    public int ResourceId { get; set; }

    public Resource Resource { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [Required]
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum BookingStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}