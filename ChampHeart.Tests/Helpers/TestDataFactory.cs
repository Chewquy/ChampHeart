using ChampHeart.API.DTOs;
using ChampHeart.API.Models;

namespace ChampHeart.Tests.Helpers;

public static class TestDataFactory
{
    // Create a valid booking request
    public static CreateBookingDTO ValidBookingRequest(
        int resourceId = 1)
    {
        var start = DateTimeOffset.UtcNow.AddDays(7);

        return new CreateBookingDTO
        {
            ResourceId = resourceId,
            StartTime = start,
            EndTime = start.AddHours(1)
        };
    }

    // Create a booking entity for test data
    public static Booking CreateBooking(
        BookingStatus status = BookingStatus.Pending,
        string userId = "member-1",
        int resourceId = 1,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        var start = startTime ?? DateTime.UtcNow.AddDays(7);

        return new Booking
        {
            UserId = userId,
            ResourceId = resourceId,
            StartTime = start,
            EndTime = endTime ?? start.AddHours(1),
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
    }
}
