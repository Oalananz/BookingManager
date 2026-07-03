using BookingManager.Api.Dtos;
using BookingManager.Api.Mapping;
using BookingManager.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingManager.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> Create(CreateBookingRequest request)
    {
        var booking = await bookingService.CreateAsync(request);
        return CreatedAtAction(nameof(GetForResource), new { resourceId = booking.ResourceId }, booking.ToResponse());
    }

    [HttpGet]
    public async Task<ActionResult<List<BookingResponse>>> GetForResource(
        [FromQuery] Guid resourceId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var bookings = await bookingService.GetForResourceAsync(resourceId, from, to);
        return Ok(bookings.Select(b => b.ToResponse()).ToList());
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<BookingResponse>> Cancel(Guid id)
    {
        var booking = await bookingService.CancelAsync(id);
        return Ok(booking.ToResponse());
    }
}
