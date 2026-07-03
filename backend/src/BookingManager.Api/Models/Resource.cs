namespace BookingManager.Api.Models;

public class Resource
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
