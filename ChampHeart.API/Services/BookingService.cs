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

        // Create a new booking
        public async Task<BookingResponseDTO> CreateBookingAsync(
            CreateBookingDTO request,
            string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "You must be logged in to create a booking.");
            }

            // Validate appointment dates
            if (request.EndTime <= request.StartTime)
            {
                throw new ArgumentException(
                    "End time must be after start time.");
            }

            if (request.StartTime <= DateTimeOffset.UtcNow)
            {
                throw new ArgumentException(
                    "Cannot create a booking in the past.");
            }

            // Check if the resource exists and is active
            var resource = await _context.Resources
                .FirstOrDefaultAsync(r =>
                    r.Id == request.ResourceId && r.IsActive);

            if (resource == null)
            {
                throw new KeyNotFoundException(
                    "Resource not found or unavailable.");
            }

            // Create the booking
            var booking = new Booking
            {
                ResourceId = resource.Id,
                UserId = userId,
                StartTime = request.StartTime.UtcDateTime,
                EndTime = request.EndTime.UtcDateTime,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            // Save to SQL Server
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Return the booking response
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

        // Get all bookings belonging to a member
        public async Task<List<BookingResponseDTO>> GetMyBookingsAsync(
            string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "You must be logged in.");
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
    }
}
