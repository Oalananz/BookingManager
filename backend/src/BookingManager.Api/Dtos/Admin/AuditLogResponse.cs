using System.Text.Json;
using BookingManager.Api.Models;

namespace BookingManager.Api.Dtos.Admin;

public class AuditLogResponse
{
    public long Id { get; init; }
    public Guid? ActorUserId { get; init; }
    public AuditAction Action { get; init; }
    public required string EntityType { get; init; }
    public string? EntityId { get; init; }
    public JsonElement? OldValue { get; init; }
    public JsonElement? NewValue { get; init; }
    public string? IpAddress { get; init; }
    public DateTime CreatedAt { get; init; }
}
