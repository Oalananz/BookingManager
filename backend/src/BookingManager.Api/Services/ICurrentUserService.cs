namespace BookingManager.Api.Services;

/// <summary>
/// Identity of the caller, taken exclusively from the validated JWT.
/// The client can never supply a userId in a request body.
/// </summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
    bool IsAdmin { get; }
    string? IpAddress { get; }
}
