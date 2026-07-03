using BookingManager.Api.Models;

namespace BookingManager.Api.Dtos.Auth;

public class UserResponse
{
    public Guid Id { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public UserRole Role { get; init; }
}
