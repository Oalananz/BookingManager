using BookingManager.Api.Models;

namespace BookingManager.Api.Dtos.Bookings;

/// <summary>Full booking representation, returned by detail and write endpoints.</summary>
public class BookingResponse
{
    public Guid Id { get; init; }
    public Guid ResourceId { get; init; }
    public string? ResourceName { get; init; }
    public Guid UserId { get; init; }
    public DateTime StartDateTime { get; init; }
    public DateTime EndDateTime { get; init; }
    public BookingStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public Guid? CancelledBy { get; init; }
}
