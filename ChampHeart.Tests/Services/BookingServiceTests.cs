using ChampHeart.API.Models;
using ChampHeart.API.Services;
using ChampHeart.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChampHeart.Tests.Services;

public class BookingServiceTests
{
    // TEST 1: Valid booking
    [Fact]
    public async Task CreateBooking_ValidRequest_CreatesPendingBooking()
    {
        // Arrange
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var request = TestDataFactory.ValidBookingRequest();

        // Act
        var result = await service.CreateBookingAsync(
            request, "member-1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(1, result.ResourceId);

        var savedBooking = await db.Context.Bookings.SingleAsync();

        Assert.Equal("member-1", savedBooking.UserId);
        Assert.Equal(BookingStatus.Pending, savedBooking.Status);
    }

    // TEST 2: Booking in the past
    [Fact]
    public async Task CreateBooking_PastDate_ThrowsException()
    {
        // Arrange
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var request = TestDataFactory.ValidBookingRequest();

        request.StartTime = DateTimeOffset.UtcNow.AddDays(-2);
        request.EndTime = request.StartTime.AddHours(1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateBookingAsync(
                request, "member-1"));

        Assert.Empty(db.Context.Bookings);
    }

    // TEST 3: Invalid time range
    [Fact]
    public async Task CreateBooking_InvalidTimeRange_ThrowsException()
    {
        // Arrange
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var request = TestDataFactory.ValidBookingRequest();

        request.EndTime = request.StartTime.AddHours(-1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateBookingAsync(
                request, "member-1"));

        Assert.Empty(db.Context.Bookings);
    }

    // TEST 4: Inactive resource
    [Fact]
    public async Task CreateBooking_InactiveResource_ThrowsException()
    {
        // Arrange
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        // Resource 3 is inactive in TestDatabase
        var request = TestDataFactory.ValidBookingRequest(
            resourceId: 3);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateBookingAsync(
                request, "member-1"));

        Assert.Empty(db.Context.Bookings);
    }
}
