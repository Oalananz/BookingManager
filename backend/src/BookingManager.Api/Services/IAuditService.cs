using BookingManager.Api.Models;

namespace BookingManager.Api.Services;

public interface IAuditService
{
    /// <summary>
    /// Stages an audit row on the current DbContext. It is persisted by the
    /// caller's SaveChanges, so the log entry is atomic with the change itself.
    /// </summary>
    void Log(AuditAction action, string entityType, string? entityId,
        object? oldValue = null, object? newValue = null, Guid? actorUserId = null);
}
