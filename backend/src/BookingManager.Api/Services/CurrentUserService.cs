using System.Security.Claims;
using BookingManager.Api.Exceptions;
using BookingManager.Api.Models;

namespace BookingManager.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id)
                ? id
                : throw new UnauthorizedException("Authentication is required.");
        }
    }

    public bool IsAdmin => Principal?.IsInRole(nameof(UserRole.Admin)) ?? false;

    public string? IpAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
