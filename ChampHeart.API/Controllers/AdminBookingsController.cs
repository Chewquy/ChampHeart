using ChampHeart.API.DTOs;
using ChampHeart.API.Exceptions;
using ChampHeart.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChampHeart.API.Controllers
{
    [ApiController]
    [Route("api/admin/bookings")]
    [Authorize(Roles = "Admin")]
    public class AdminBookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public AdminBookingsController(
            IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        // api/admin/bookings/5/approve
        [HttpPost("{id:int}/approve")]
        public async Task<ActionResult<BookingResponseDTO>> ApproveBooking(int id)
        {
            try
            {
                var booking = await _bookingService.ApproveBookingAsync(id);
                return Ok(booking);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (BookingConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // api/admin/bookings/5/reject
        [HttpPost("{id:int}/reject")]
        public async Task<ActionResult<BookingResponseDTO>> RejectBooking(int id)
        {
            try
            {
                var booking = await _bookingService.RejectBookingAsync(id);
                return Ok(booking);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (BookingConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}
