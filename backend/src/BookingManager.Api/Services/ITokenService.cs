using BookingManager.Api.Models;

namespace BookingManager.Api.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(User user);
}
