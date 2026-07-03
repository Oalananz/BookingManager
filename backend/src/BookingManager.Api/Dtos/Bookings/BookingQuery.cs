using BookingManager.Api.Models;

namespace BookingManager.Api.Dtos.Bookings;

public class BookingQuery
{
    public Guid? ResourceId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public BookingStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>Admin variant: can additionally filter by the booking owner.</summary>
public class AdminBookingQuery : BookingQuery
{
    public Guid? UserId { get; set; }
}
