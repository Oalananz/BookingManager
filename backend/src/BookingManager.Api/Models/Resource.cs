namespace BookingManager.Api.Models;

public class Resource
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public ResourceStatus Status { get; set; } = ResourceStatus.Available;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
