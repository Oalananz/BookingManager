using BookingManager.Api.Data;
using BookingManager.Api.Dtos.Auth;
using BookingManager.Api.Exceptions;
using BookingManager.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookingManager.Api.Services;

public class AuthService(
    AppDbContext dbContext,
    ITokenService tokenService,
    IAuditService auditService) : IAuthService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await dbContext.Users.AnyAsync(u => u.Email == email))
        {
            throw new ConflictException("An account with this email already exists.", "EMAIL_TAKEN");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = string.Empty,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
        auditService.Log(AuditAction.UserRegistered, "User", user.Id.ToString(),
            newValue: new { user.Id, user.Email, Role = user.Role.ToString() },
            actorUserId: user.Id);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConflictException("An account with this email already exists.", "EMAIL_TAKEN");
        }

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                == PasswordVerificationResult.Failed)
        {
            auditService.Log(AuditAction.LoginFailed, "User", user?.Id.ToString() ?? email,
                actorUserId: user?.Id);
            await dbContext.SaveChangesAsync();

            // Same error whether the email or the password is wrong — no account enumeration.
            throw new UnauthorizedException("Invalid email or password.", "INVALID_CREDENTIALS");
        }

        return BuildAuthResponse(user);
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var (token, expiresAt) = tokenService.CreateToken(user);
        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            User = new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            }
        };
    }

    private static bool IsUniqueViolation(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
            {
                return true;
            }
        }

        return false;
    }
}
