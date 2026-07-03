namespace BookingManager.Api.Models;

public enum AuditAction
{
    BookingCreated,
    BookingCancelled,
    ResourceCreated,
    ResourceUpdated,
    ResourceDeleted,
    UserRegistered,
    LoginFailed,
    AdminActionPerformed
}
