using BookingManager.Api.Dtos.Bookings;
using BookingManager.Api.Dtos.Resources;
using BookingManager.Api.Models;

namespace BookingManager.Api.Mapping;

public static class BookingMappingExtensions
{
    /// <summary>
    /// Completed is never stored: an Active booking whose end time has passed
    /// is reported as Completed at read time.
    /// </summary>
    public static BookingStatus EffectiveStatus(this Booking booking) =>
        booking.Status == BookingStatus.Active && booking.EndDateTime <= DateTime.UtcNow
            ? BookingStatus.Completed
            : booking.Status;

    public static BookingResponse ToResponse(this Booking booking) => new()
    {
        Id = booking.Id,
        ResourceId = booking.ResourceId,
        ResourceName = booking.Resource?.Name,
        UserId = booking.UserId,
        StartDateTime = booking.StartDateTime,
        EndDateTime = booking.EndDateTime,
        Status = booking.EffectiveStatus(),
        CreatedAt = booking.CreatedAt,
        UpdatedAt = booking.UpdatedAt,
        CancelledAt = booking.CancelledAt,
        CancelledBy = booking.CancelledBy
    };

    public static BookingSummaryResponse ToSummaryResponse(this Booking booking) => new()
    {
        Id = booking.Id,
        ResourceId = booking.ResourceId,
        ResourceName = booking.Resource?.Name,
        UserId = booking.UserId,
        StartDateTime = booking.StartDateTime,
        EndDateTime = booking.EndDateTime,
        Status = booking.EffectiveStatus()
    };

    public static ResourceResponse ToResponse(this Resource resource) => new()
    {
        Id = resource.Id,
        Name = resource.Name,
        Type = resource.Type,
        Description = resource.Description,
        Capacity = resource.Capacity,
        Status = resource.Status,
        CreatedAt = resource.CreatedAt,
        UpdatedAt = resource.UpdatedAt
    };

    public static ResourceSummaryResponse ToSummaryResponse(this Resource resource) => new()
    {
        Id = resource.Id,
        Name = resource.Name,
        Type = resource.Type,
        Capacity = resource.Capacity,
        Status = resource.Status
    };
}
