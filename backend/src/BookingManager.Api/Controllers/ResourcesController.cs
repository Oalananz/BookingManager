using BookingManager.Api.Dtos.Common;
using BookingManager.Api.Dtos.Resources;
using BookingManager.Api.Models;
using BookingManager.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookingManager.Api.Controllers;

[ApiController]
[Route("api/resources")]
[Authorize]
public class ResourcesController(IResourceService resourceService, IBookingService bookingService) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting("reads")]
    public async Task<IActionResult> GetAll([FromQuery] string? type, [FromQuery] ResourceStatus? status)
    {
        var resources = await resourceService.GetAllAsync(type, status);
        return Ok(ApiResponse.Of(resources));
    }

    [HttpGet("{id:guid}")]
    [EnableRateLimiting("reads")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var resource = await resourceService.GetByIdAsync(id);
        return Ok(ApiResponse.Of(resource));
    }

    [HttpGet("{id:guid}/availability")]
    [EnableRateLimiting("reads")]
    public async Task<IActionResult> GetAvailability(
        Guid id,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int durationMinutes = 60)
    {
        var availability = await bookingService.GetAvailabilityAsync(id, from, to, durationMinutes);
        return Ok(ApiResponse.Of(availability));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create(CreateResourceRequest request)
    {
        var resource = await resourceService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = resource.Id }, ApiResponse.Of(resource));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, UpdateResourceRequest request)
    {
        var resource = await resourceService.UpdateAsync(id, request);
        return Ok(ApiResponse.Of(resource));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await resourceService.DeleteAsync(id);
        return NoContent();
    }
}
