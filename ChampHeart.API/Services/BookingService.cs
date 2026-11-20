using ChampHeart.API.Data;
using ChampHeart.API.DTOs;
using ChampHeart.API.Models;
using ChampHeart.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChampHeart.API.Services
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;

        public BookingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BookingResponseDTO> CreateBookingAsync(CreateBookingDTO request, string userId)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("You must be logged in to create a booking.");
            }

            if (request.EndTime <= request.StartTime)
            {
                throw new ArgumentException("End time must be after start time.");
            }

            if (request.StartTime <= DateTimeOffset.UtcNow)
            {
                throw new ArgumentException("Cannot create a booking in the past.");
            }

            var resource = await _context.Resources.FirstOrDefaultAsync(r =>
                    r.Id == request.ResourceId && r.IsActive);

            if (resource == null)
            {
                throw new KeyNotFoundException("Resource not found or unavailable.");
            }




            var booking = new Booking
            {
                ResourceId = resource.Id,
                UserId = userId,
                StartTime = request.StartTime.UtcDateTime,
                EndTime = request.EndTime.UtcDateTime,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };



            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();



            return new BookingResponseDTO
            {
                Id = booking.Id,
                ResourceId = resource.Id,
                ResourceName = resource.Name,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Status = booking.Status.ToString(),
                CreatedAt = booking.CreatedAt
            };
        }

        public async Task<List<BookingResponseDTO>> GetMyBookingsAsync(string userId)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("You must be logged in.");
            }

            return await _context.Bookings
                .AsNoTracking()
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.StartTime)
                .Select(b => new BookingResponseDTO
                {
                    Id = b.Id,
                    ResourceId = b.ResourceId,
                    ResourceName = b.Resource.Name,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    Status = b.Status.ToString(),
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<bool> CancelBookingAsync(int bookingId, string userId)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);

            // Validation
            if (booking == null)
            {
                throw new KeyNotFoundException("Booking not found.");
            }

            if (booking.UserId != userId)
            {
                throw new UnauthorizedAccessException("You cannot cancel another user's booking.");
            }

            if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Rejected)
            { 
                throw new InvalidOperationException("This booking cannot be cancelled."); 
            }

            if (booking.StartTime <= DateTime.UtcNow.AddHours(24))
            {
                throw new InvalidOperationException("Bookings must be cancelled at least 24 hours in advance.");
            }



            booking.Status = BookingStatus.Cancelled;


            await _context.SaveChangesAsync();


            return true;
        }

    }
}
