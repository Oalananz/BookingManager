namespace BookingManager.Api.Models;

/// <summary>
/// Append-only record of every state change. Rows are written in the same
/// SaveChanges as the change they describe, so the log cannot drift from the data.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public AuditAction Action { get; set; }
    public required string EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}
