using System.Text.Json;
using BookingManager.Api.Data;
using BookingManager.Api.Models;

namespace BookingManager.Api.Services;

public class AuditService(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor) : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Log(AuditAction action, string entityType, string? entityId,
        object? oldValue = null, object? newValue = null, Guid? actorUserId = null)
    {
        var httpContext = httpContextAccessor.HttpContext;

        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId ?? GetAuthenticatedUserId(),
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue, JsonOptions),
            NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue, JsonOptions),
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
            CreatedAt = DateTime.UtcNow
        });
    }

    private Guid? GetAuthenticatedUserId()
    {
        var value = httpContextAccessor.HttpContext?.User
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
