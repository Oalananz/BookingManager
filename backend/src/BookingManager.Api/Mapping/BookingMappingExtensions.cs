using BookingManager.Api.Dtos;
using BookingManager.Api.Models;

namespace BookingManager.Api.Mapping;

public static class BookingMappingExtensions
{
    public static BookingResponse ToResponse(this Booking booking) => new()
    {
        Id = booking.Id,
        ResourceId = booking.ResourceId,
        UserId = booking.UserId,
        StartDateTime = booking.StartDateTime,
        EndDateTime = booking.EndDateTime,
        Status = booking.Status,
        CreatedAt = booking.CreatedAt,
        CancelledAt = booking.CancelledAt
    };

    public static ResourceResponse ToResponse(this Resource resource) => new()
    {
        Id = resource.Id,
        Name = resource.Name,
        Type = resource.Type
    };
}
