using BookingManager.Api.Models;

namespace BookingManager.Api.Dtos.Bookings;

/// <summary>Slim representation for list endpoints — avoids over-fetching.</summary>
public class BookingSummaryResponse
{
    public Guid Id { get; init; }
    public Guid ResourceId { get; init; }
    public string? ResourceName { get; init; }
    public Guid UserId { get; init; }
    public DateTime StartDateTime { get; init; }
    public DateTime EndDateTime { get; init; }
    public BookingStatus Status { get; init; }
}
