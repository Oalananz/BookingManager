using BookingManager.Api.Models;

namespace BookingManager.Api.Dtos;

public class BookingResponse
{
    public Guid Id { get; set; }
    public Guid ResourceId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}
