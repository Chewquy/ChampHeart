using ChampHeart.API.DTOs;
using ChampHeart.API.Exceptions;
using ChampHeart.API.Models;
using ChampHeart.API.Services;
using ChampHeart.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChampHeart.Tests.Services
{
    public class BookingServiceTests
    {
        private static CreateBookingDTO NewBooking(
            int resourceId = 1)
        {
            return new CreateBookingDTO
            {
                ResourceId = resourceId,
                StartTime = DateTimeOffset.UtcNow.AddDays(7),
                EndTime = DateTimeOffset.UtcNow
                    .AddDays(7).AddHours(1)
            };
        }

        private static Booking AddBooking(
            TestDatabase db,
            BookingStatus status,
            DateTime start,
            DateTime end,
            string userId = "member-1",
            int resourceId = 1)
        {
            var booking = new Booking
            {
                ResourceId = resourceId,
                UserId = userId,
                StartTime = start,
                EndTime = end,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            db.Context.Bookings.Add(booking);
            db.Context.SaveChanges();

            return booking;
        }

        [Fact]
        public async Task CreateBooking_ValidRequest_CreatesPendingBooking()
        {
            using var db = new TestDatabase();
            var service = new BookingService(db.Context);

            var request = NewBooking();

            var result = await service.CreateBookingAsync(
                request, "member-1");

            Assert.Equal("Pending", result.Status);
            Assert.Equal(1, result.ResourceId);

            Assert.Single(db.Context.Bookings);
        }

        [Fact]
        public async Task CreateBooking_PastDate_ThrowsException()
        {
            using var db = new TestDatabase();
            var service = new BookingService(db.Context);

            var request = new CreateBookingDTO
            {
                ResourceId = 1,
                StartTime = DateTimeOffset.UtcNow.AddDays(-2),
                EndTime = DateTimeOffset.UtcNow.AddDays(-2)
                    .AddHours(1)
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.CreateBookingAsync(
                    request, "member-1"));
        }

        [Fact]
        public async Task CreateBooking_InvalidTimeRange_ThrowsException()
        {
            using var db = new TestDatabase();
            var service = new BookingService(db.Context);

            var request = NewBooking();

            request.EndTime = request.StartTime.AddHours(-1);

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.CreateBookingAsync(
                    request, "member-1"));
        }

        [Fact]
        public async Task CreateBooking_InactiveResource_ThrowsException()
        {
            using var db = new TestDatabase();
            var service = new BookingService(db.Context);

            var request = NewBooking(resourceId: 3);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.CreateBookingAsync(
                    request, "member-1"));
        }

        [Fact]
        public async Task CancelBooking_ValidRequest_SetsCancelled()
        {
            using var db = new TestDatabase();
            var service = new BookingService(db.Context);

            var start = DateTime.UtcNow.AddDays(5);

            var booking = AddBooking(
                db,
                BookingStatus.Approved,
                start,
                start.AddHours(1));

            var result = await service.CancelBookingAsync(
                booking.Id, "member-1");

            Assert.True(result);

            Assert.Equal(
                BookingStatus.Cancelled,
                booking.Status);
        }

        [Fact]
        public async Task CancelBooking_OtherUser_ThrowsException()
        {
            using var db = new TestDatabase();
            var service = new BookingService(db.Context);

            var start = DateTime.UtcNow.AddDays(5);

            var booking = AddBooking(
                db,
                BookingStatus.Approved,
                start,
                start.AddHours(1));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.CancelBookingAsync(
                    booking.Id, "member-2"));

            Assert.Equal(
                BookingStatus.Approved,
                booking.Status);
        }

        [Fact]
        public async Task CancelBooking_Within24Hours_ThrowsException()
        {
            using var db = new TestDatabase();
            var service = new BookingService(db.Context);

            var start = DateTime.UtcNow.AddHours(12);

            var booking = AddBooking(
                db,
                BookingStatus.Approved,
                start,
                start.AddHours(1));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CancelBookingAsync(
                    booking.Id, "member-1"));

            Assert.Equal(
                BookingStatus.Approved,
                booking.Status);
        }

        [Fact]
        public async Task ApproveBooking_ValidBooking_SetsApproved()
        {
            using var db = new TestDatabase();
            var service = new BookingService(db.Context);

            var start = DateTime.UtcNow.AddDays(5);

            var booking = AddBooking(
                db,
                BookingStatus.Pending,
                start,
                start.AddHours(1));

            var result = await service.ApproveBookingAsync(
                booking.Id);

            Assert.Equal("Approved", result.Status);

            Assert.Equal(
                BookingStatus.Approved,
                booking.Status);
        }

        [Fact]
        public async Task ApproveBooking_OverlappingApproved_ThrowsConflict()
        {
            using var db = new TestDatabase();
            var service = new BookingService(db.Context);

            var start = DateTime.UtcNow.AddDays(5);

            AddBooking(
                db,
                BookingStatus.Approved,
                start,
                start.AddHours(2));

            var secondBooking = AddBooking(
                db,
                BookingStatus.Pending,
                start.AddHours(1),
                start.AddHours(3));

            await Assert.ThrowsAsync<BookingConflictException>(
                () => service.ApproveBookingAsync(
                    secondBooking.Id));

            Assert.Equal(
                BookingStatus.Pending,
                secondBooking.Status);
        }

        [Fact]
        public async Task ApproveBooking_AdjacentBookings_AllowsApproval()
        {
            using var db = new TestDatabase();
            var service = new BookingService(db.Context);

            var start = DateTime.UtcNow.AddDays(5);

            AddBooking(
                db,
                BookingStatus.Approved,
                start,
                start.AddHours(1));

            var secondBooking = AddBooking(
                db,
                BookingStatus.Pending,
                start.AddHours(1),
                start.AddHours(2));

            var result = await service.ApproveBookingAsync(
                secondBooking.Id);

            Assert.Equal("Approved", result.Status);
        }

        [Fact]
        public async Task ApproveBooking_OverlappingPending_AllowsApproval()
        {
            using var db = new TestDatabase();
            var service = new BookingService(db.Context);

            var start = DateTime.UtcNow.AddDays(5);

            AddBooking(
                db,
                BookingStatus.Pending,
                start,
                start.AddHours(2));

            var secondBooking = AddBooking(
                db,
                BookingStatus.Pending,
                start.AddHours(1),
                start.AddHours(3));

            var result = await service.ApproveBookingAsync(
                secondBooking.Id);

            Assert.Equal("Approved", result.Status);
        }

        [Fact]
        public async Task ApproveBooking_AlreadyApproved_ThrowsConflict()
        {
            using var db = new TestDatabase();
            var service = new BookingService(db.Context);

            var start = DateTime.UtcNow.AddDays(5);

            var booking = AddBooking(
                db,
                BookingStatus.Approved,
                start,
                start.AddHours(1));

            await Assert.ThrowsAsync<BookingConflictException>(
                () => service.ApproveBookingAsync(
                    booking.Id));
        }

        [Fact]
        public async Task RejectBooking_PendingBooking_SetsRejected()
        {
            using var db = new TestDatabase();
            var service = new BookingService(db.Context);

            var start = DateTime.UtcNow.AddDays(5);

            var booking = AddBooking(
                db,
                BookingStatus.Pending,
                start,
                start.AddHours(1));

            var result = await service.RejectBookingAsync(
                booking.Id);

            Assert.Equal("Rejected", result.Status);

            Assert.Equal(
                BookingStatus.Rejected,
                db.Context.Bookings.AsNoTracking()
                    .Single(b => b.Id == booking.Id).Status);
        }

        [Fact]
        public async Task RejectBooking_ApprovedBooking_ThrowsConflict()
        {
            using var db = new TestDatabase();
            var service = new BookingService(db.Context);

            var start = DateTime.UtcNow.AddDays(5);

            var booking = AddBooking(
                db,
                BookingStatus.Approved,
                start,
                start.AddHours(1));

            await Assert.ThrowsAsync<BookingConflictException>(
                () => service.RejectBookingAsync(
                    booking.Id));
        }

        [Fact]
        public async Task GetMyBookings_ReturnsOnlyOwnBookings()
        {
            using var db = new TestDatabase();
            var service = new BookingService(db.Context);

            var start = DateTime.UtcNow.AddDays(5);

            AddBooking(
                db,
                BookingStatus.Pending,
                start,
                start.AddHours(1),
                userId: "member-1");

            AddBooking(
                db,
                BookingStatus.Pending,
                start.AddHours(2),
                start.AddHours(3),
                userId: "member-2");

            var result = await service.GetMyBookingsAsync(
                "member-1");

            Assert.Single(result);
            Assert.Equal("Pending", result[0].Status);
        }
    }
}
