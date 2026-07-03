namespace BookingManager.Api.Dtos.Auth;

public class AuthResponse
{
    public required string AccessToken { get; init; }
    public DateTime ExpiresAt { get; init; }
    public required UserResponse User { get; init; }
}
