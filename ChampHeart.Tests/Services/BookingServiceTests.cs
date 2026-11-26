
using ChampHeart.API.Exceptions;
using ChampHeart.API.Models;
using ChampHeart.API.Services;
using ChampHeart.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChampHeart.Tests.Services;

public class BookingServiceTests
{
    // Helper: Insert a booking into the test database
    private static async Task<Booking> AddBookingAsync(
        TestDatabase db,
        BookingStatus status = BookingStatus.Pending,
        string userId = "member-1",
        int resourceId = 1,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        var booking = TestDataFactory.CreateBooking(
            status: status,
            userId: userId,
            resourceId: resourceId,
            startTime: startTime,
            endTime: endTime);

        db.Context.Bookings.Add(booking);
        await db.Context.SaveChangesAsync();

        return booking;
    }

    // ==========================================
    // SECTION 1: BOOKING CREATION
    // ==========================================

    // TEST 1: Valid booking creates Pending status
    [Fact]
    public async Task CreateBooking_ValidRequest_CreatesPendingBooking()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);
        var request = TestDataFactory.ValidBookingRequest();

        var result = await service.CreateBookingAsync(
            request, "member-1");

        Assert.NotNull(result);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(1, result.ResourceId);

        var saved = await db.Context.Bookings.SingleAsync();

        Assert.Equal("member-1", saved.UserId);
        Assert.Equal(BookingStatus.Pending, saved.Status);
    }

    // TEST 2: Cannot book in the past
    [Fact]
    public async Task CreateBooking_PastDate_ThrowsException()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);
        var request = TestDataFactory.ValidBookingRequest();

        request.StartTime = DateTimeOffset.UtcNow.AddDays(-2);
        request.EndTime = request.StartTime.AddHours(1);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateBookingAsync(request, "member-1"));

        Assert.Empty(db.Context.Bookings);
    }

    // TEST 3: End time must be after start time
    [Fact]
    public async Task CreateBooking_InvalidTimeRange_ThrowsException()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);
        var request = TestDataFactory.ValidBookingRequest();

        request.EndTime = request.StartTime.AddHours(-1);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateBookingAsync(request, "member-1"));

        Assert.Empty(db.Context.Bookings);
    }

    // TEST 4: Inactive resource cannot be booked
    [Fact]
    public async Task CreateBooking_InactiveResource_ThrowsException()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        // Resource 3 is inactive in TestDatabase
        var request = TestDataFactory.ValidBookingRequest(
            resourceId: 3);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateBookingAsync(request, "member-1"));

        Assert.Empty(db.Context.Bookings);
    }

    // ==========================================
    // SECTION 2: BOOKING CANCELLATION
    // ==========================================

    // TEST 5: Owner can cancel a valid booking
    [Fact]
    public async Task CancelBooking_ValidRequest_SetsCancelled()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var booking = await AddBookingAsync(
            db, BookingStatus.Approved);

        var result = await service.CancelBookingAsync(
            booking.Id, "member-1");

        Assert.True(result);

        var saved = await db.Context.Bookings
            .AsNoTracking()
            .SingleAsync(b => b.Id == booking.Id);

        Assert.Equal(BookingStatus.Cancelled, saved.Status);
    }

    // TEST 6: Member cannot cancel another user's booking
    [Fact]
    public async Task CancelBooking_OtherUser_ThrowsException()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var booking = await AddBookingAsync(
            db, BookingStatus.Approved);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CancelBookingAsync(
                booking.Id, "member-2"));

        Assert.Equal(
            BookingStatus.Approved,
            booking.Status);
    }

    // TEST 7: Cancellation requires 24-hour notice
    [Fact]
    public async Task CancelBooking_Within24Hours_ThrowsException()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var start = DateTime.UtcNow.AddHours(12);

        var booking = await AddBookingAsync(
            db,
            BookingStatus.Approved,
            startTime: start,
            endTime: start.AddHours(1));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CancelBookingAsync(
                booking.Id, "member-1"));

        Assert.Equal(
            BookingStatus.Approved,
            booking.Status);
    }

    // TEST 8: Already cancelled booking cannot be cancelled
    [Fact]
    public async Task CancelBooking_AlreadyCancelled_ThrowsException()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var booking = await AddBookingAsync(
            db, BookingStatus.Cancelled);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CancelBookingAsync(
                booking.Id, "member-1"));
    }

    // TEST 9: Rejected booking cannot be cancelled
    [Fact]
    public async Task CancelBooking_RejectedBooking_ThrowsException()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var booking = await AddBookingAsync(
            db, BookingStatus.Rejected);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CancelBookingAsync(
                booking.Id, "member-1"));
    }

    // ==========================================
    // SECTION 3: APPROVAL AND CONFLICT DETECTION
    // ==========================================

    // TEST 10: Approve a valid pending booking
    [Fact]
    public async Task ApproveBooking_ValidBooking_SetsApproved()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var booking = await AddBookingAsync(db);

        var result = await service.ApproveBookingAsync(booking.Id);

        Assert.Equal("Approved", result.Status);

        var saved = await db.Context.Bookings
            .AsNoTracking()
            .SingleAsync(b => b.Id == booking.Id);

        Assert.Equal(BookingStatus.Approved, saved.Status);
    }

    // TEST 11: Overlapping approved booking prevents approval
    [Fact]
    public async Task ApproveBooking_OverlappingApproved_ThrowsConflict()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var start = DateTime.UtcNow.AddDays(7);

        await AddBookingAsync(
            db,
            BookingStatus.Approved,
            startTime: start,
            endTime: start.AddHours(2));

        var second = await AddBookingAsync(
            db,
            BookingStatus.Pending,
            startTime: start.AddHours(1),
            endTime: start.AddHours(3));

        await Assert.ThrowsAsync<BookingConflictException>(
            () => service.ApproveBookingAsync(second.Id));

        var saved = await db.Context.Bookings
            .AsNoTracking()
            .SingleAsync(b => b.Id == second.Id);

        Assert.Equal(BookingStatus.Pending, saved.Status);
    }

    // TEST 12: Adjacent bookings do not overlap
    [Fact]
    public async Task ApproveBooking_AdjacentBookings_AllowsApproval()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var start = DateTime.UtcNow.AddDays(7);

        await AddBookingAsync(
            db,
            BookingStatus.Approved,
            startTime: start,
            endTime: start.AddHours(1));

        var second = await AddBookingAsync(
            db,
            BookingStatus.Pending,
            startTime: start.AddHours(1),
            endTime: start.AddHours(2));

        var result = await service.ApproveBookingAsync(second.Id);

        Assert.Equal("Approved", result.Status);
    }

    // TEST 13: Overlapping Pending bookings are allowed
    [Fact]
    public async Task ApproveBooking_OverlappingPending_AllowsApproval()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var start = DateTime.UtcNow.AddDays(7);

        var first = await AddBookingAsync(
            db,
            BookingStatus.Pending,
            startTime: start,
            endTime: start.AddHours(2));

        var second = await AddBookingAsync(
            db,
            BookingStatus.Pending,
            startTime: start.AddHours(1),
            endTime: start.AddHours(3));

        var result = await service.ApproveBookingAsync(second.Id);

        Assert.Equal("Approved", result.Status);

        var firstSaved = await db.Context.Bookings
            .AsNoTracking()
            .SingleAsync(b => b.Id == first.Id);

        Assert.Equal(BookingStatus.Pending, firstSaved.Status);
    }

    // TESTS 14-15: Rejected/Cancelled bookings don't block
    [Theory]
    [InlineData(BookingStatus.Rejected)]
    [InlineData(BookingStatus.Cancelled)]
    public async Task ApproveBooking_InactiveBookingStatus_AllowsApproval(
        BookingStatus existingStatus)
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var start = DateTime.UtcNow.AddDays(7);

        await AddBookingAsync(
            db,
            existingStatus,
            startTime: start,
            endTime: start.AddHours(2));

        var second = await AddBookingAsync(
            db,
            BookingStatus.Pending,
            startTime: start.AddHours(1),
            endTime: start.AddHours(3));

        var result = await service.ApproveBookingAsync(second.Id);

        Assert.Equal("Approved", result.Status);
    }

    // TEST 16: Already approved booking cannot be approved again
    [Fact]
    public async Task ApproveBooking_AlreadyApproved_ThrowsConflict()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var booking = await AddBookingAsync(
            db, BookingStatus.Approved);

        await Assert.ThrowsAsync<BookingConflictException>(
            () => service.ApproveBookingAsync(booking.Id));
    }

    // TEST 17: Inactive resource cannot receive approval
    [Fact]
    public async Task ApproveBooking_InactiveResource_ThrowsConflict()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var booking = await AddBookingAsync(
            db,
            BookingStatus.Pending,
            resourceId: 3);

        await Assert.ThrowsAsync<BookingConflictException>(
            () => service.ApproveBookingAsync(booking.Id));
    }

    // TEST 18: Past appointment cannot be approved
    [Fact]
    public async Task ApproveBooking_PastBooking_ThrowsConflict()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var start = DateTime.UtcNow.AddDays(-2);

        var booking = await AddBookingAsync(
            db,
            BookingStatus.Pending,
            startTime: start,
            endTime: start.AddHours(1));

        await Assert.ThrowsAsync<BookingConflictException>(
            () => service.ApproveBookingAsync(booking.Id));
    }

    // ==========================================
    // SECTION 4: BOOKING REJECTION
    // ==========================================

    // TEST 19: Admin can reject a pending booking
    [Fact]
    public async Task RejectBooking_PendingBooking_SetsRejected()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var booking = await AddBookingAsync(db);

        var result = await service.RejectBookingAsync(booking.Id);

        Assert.Equal("Rejected", result.Status);

        var saved = await db.Context.Bookings
            .AsNoTracking()
            .SingleAsync(b => b.Id == booking.Id);

        Assert.Equal(BookingStatus.Rejected, saved.Status);
    }

    // TEST 20: Approved booking cannot be rejected
    [Fact]
    public async Task RejectBooking_ApprovedBooking_ThrowsConflict()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var booking = await AddBookingAsync(
            db, BookingStatus.Approved);

        await Assert.ThrowsAsync<BookingConflictException>(
            () => service.RejectBookingAsync(booking.Id));
    }

    // TEST 21: Already rejected booking cannot be rejected
    [Fact]
    public async Task RejectBooking_AlreadyRejected_ThrowsConflict()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var booking = await AddBookingAsync(
            db, BookingStatus.Rejected);

        await Assert.ThrowsAsync<BookingConflictException>(
            () => service.RejectBookingAsync(booking.Id));
    }

    // ==========================================
    // SECTION 5: MEMBER BOOKING HISTORY
    // ==========================================

    // TEST 22: Member sees only their own bookings
    [Fact]
    public async Task GetMyBookings_ReturnsOnlyOwnBookings()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        await AddBookingAsync(
            db, userId: "member-1");

        await AddBookingAsync(
            db, userId: "member-2");

        var result = await service.GetMyBookingsAsync(
            "member-1");

        Assert.Single(result);
        Assert.Equal("Pending", result[0].Status);
    }

    // TEST 23: Member with no bookings receives empty list
    [Fact]
    public async Task GetMyBookings_NoBookings_ReturnsEmpty()
    {
        using var db = new TestDatabase();
        var service = new BookingService(db.Context);

        var result = await service.GetMyBookingsAsync(
            "member-1");

        Assert.Empty(result);
    }
}
