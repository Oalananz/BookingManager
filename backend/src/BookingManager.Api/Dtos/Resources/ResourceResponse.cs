using BookingManager.Api.Models;

namespace BookingManager.Api.Dtos.Resources;

/// <summary>Full resource representation, returned by detail and write endpoints.</summary>
public class ResourceResponse
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public string? Description { get; init; }
    public int? Capacity { get; init; }
    public ResourceStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
