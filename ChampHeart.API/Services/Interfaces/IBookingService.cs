using ChampHeart.API.DTOs;

namespace ChampHeart.API.Services.Interfaces
{
    public interface IBookingService
    {
        Task<BookingResponseDTO> CreateBookingAsync(CreateBookingDTO request, string userId);

        Task<List<BookingResponseDTO>> GetMyBookingsAsync(string userId);

        Task<bool> CancelBookingAsync(int bookingId, string userId);

        Task<BookingResponseDTO> ApproveBookingAsync(int bookingId);

        Task<BookingResponseDTO> RejectBookingAsync(int bookingId);
    }
}
