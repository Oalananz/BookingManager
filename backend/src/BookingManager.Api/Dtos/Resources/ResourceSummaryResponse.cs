using BookingManager.Api.Models;

namespace BookingManager.Api.Dtos.Resources;

/// <summary>Slim representation for list endpoints — avoids over-fetching.</summary>
public class ResourceSummaryResponse
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public int? Capacity { get; init; }
    public ResourceStatus Status { get; init; }
}
