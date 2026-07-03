namespace BookingManager.Api.Models;

public class Booking
{
    public Guid Id { get; set; }
    public Guid ResourceId { get; set; }
    public required string UserId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public Resource? Resource { get; set; }
}
