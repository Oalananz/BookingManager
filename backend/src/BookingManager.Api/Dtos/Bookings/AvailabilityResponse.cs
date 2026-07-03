namespace BookingManager.Api.Dtos.Bookings;

public class AvailabilityResponse
{
    public Guid ResourceId { get; init; }
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public int DurationMinutes { get; init; }
    public required IReadOnlyList<AvailabilitySlot> Slots { get; init; }
}

/// <summary>A free window [Start, End) in which a booking of the requested duration fits.</summary>
public class AvailabilitySlot
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
}
