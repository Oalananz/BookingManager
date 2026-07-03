using BookingManager.Api.Dtos.Bookings;
using BookingManager.Api.Dtos.Common;
using BookingManager.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookingManager.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("booking-write")]
    public async Task<IActionResult> Create(CreateBookingRequest request)
    {
        var booking = await bookingService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, ApiResponse.Of(booking));
    }

    /// <summary>Non-admin callers only ever see their own bookings.</summary>
    [HttpGet]
    [EnableRateLimiting("reads")]
    public async Task<IActionResult> GetMine([FromQuery] BookingQuery query)
    {
        return Ok(await bookingService.GetMyBookingsAsync(query));
    }

    [HttpGet("{id:guid}")]
    [EnableRateLimiting("reads")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var booking = await bookingService.GetByIdAsync(id);
        return Ok(ApiResponse.Of(booking));
    }

    [HttpPost("{id:guid}/cancel")]
    [EnableRateLimiting("booking-write")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var booking = await bookingService.CancelAsync(id);
        return Ok(ApiResponse.Of(booking));
    }
}
