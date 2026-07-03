using BookingManager.Api.Dtos.Auth;
using BookingManager.Api.Dtos.Common;
using BookingManager.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookingManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Of(result));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        return Ok(ApiResponse.Of(result));
    }

    /// <summary>
    /// JWTs are stateless: logout is client-side token disposal. The endpoint
    /// exists so clients have a uniform call; a server-side deny-list would be
    /// the next step if token revocation became a requirement.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [DisableRateLimiting]
    public IActionResult Logout() => NoContent();
}
