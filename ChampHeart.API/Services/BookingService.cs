using ChampHeart.API.Data;
using ChampHeart.API.DTOs;
using ChampHeart.API.Models;
using ChampHeart.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
using ChampHeart.API.Exceptions;
using Microsoft.Data.SqlClient;

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


        public async Task<BookingResponseDTO> ApproveBookingAsync(int bookingId)
        {
            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                var booking = await _context.Bookings.Include(b => b.Resource).FirstOrDefaultAsync(b => b.Id == bookingId);


                //Validation
                if (booking == null)
                {
                    throw new KeyNotFoundException("Booking not found.");
                }
                    
                if (booking.Status != BookingStatus.Pending)
                {
                    throw new BookingConflictException("Only pending bookings can be approved.");
                }

                if (!booking.Resource.IsActive)
                {
                    throw new BookingConflictException("This resource is no longer available.");
                }
                    
                if (booking.StartTime <= DateTime.UtcNow)
                {
                    throw new BookingConflictException("Cannot approve an appointment in the past.");
                }

                // Method to detect any conflict - Done with AI help
                bool hasConflict = await _context.Bookings
                    .AnyAsync(b =>
                        b.ResourceId == booking.ResourceId &&
                        b.Id != booking.Id &&
                        b.Status == BookingStatus.Approved &&
                        b.StartTime < booking.EndTime &&
                        booking.StartTime < b.EndTime);

                if (hasConflict)
                {
                    throw new BookingConflictException("This resource already has an approved booking during that time.");
                }

                // No conflict: approve
                booking.Status = BookingStatus.Approved;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new BookingResponseDTO
                {
                    Id = booking.Id,
                    ResourceId = booking.ResourceId,
                    ResourceName = booking.Resource.Name,
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    Status = booking.Status.ToString(),
                    CreatedAt = booking.CreatedAt
                };
            }
            //Multiple request - Done with AI help
            catch (SqlException ex) when (ex.Number == 1205)
            {
                throw new BookingConflictException("Another booking is being processed. Please retry the approval.");
            }
        }

        public async Task<BookingResponseDTO> RejectBookingAsync(int bookingId)
        {
            int updatedRows = await _context.Bookings
                .Where(b =>
                    b.Id == bookingId &&
                    b.Status == BookingStatus.Pending)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        b => b.Status,
                        BookingStatus.Rejected));

            // Validation
            if (updatedRows == 0)
            {
                bool exists = await _context.Bookings.AnyAsync(b => b.Id == bookingId);

                if (!exists)
                {
                    throw new KeyNotFoundException("Booking not found.");
                }
                    
                throw new BookingConflictException("Only pending bookings can be rejected.");
            }


            var booking = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Resource)
                .FirstAsync(b => b.Id == bookingId);



            return new BookingResponseDTO
            {
                Id = booking.Id,
                ResourceId = booking.ResourceId,
                ResourceName = booking.Resource.Name,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Status = booking.Status.ToString(),
                CreatedAt = booking.CreatedAt
            };
        }
    }
}
