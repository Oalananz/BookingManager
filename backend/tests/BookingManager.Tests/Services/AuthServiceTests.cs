using BookingManager.Api.Dtos.Auth;
using BookingManager.Api.Exceptions;
using BookingManager.Api.Models;
using BookingManager.Api.Services;
using BookingManager.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace BookingManager.Tests.Services;

public class AuthServiceTests
{
    private static AuthService CreateService(BookingManager.Api.Data.AppDbContext dbContext)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "test",
                ["Jwt:Audience"] = "test",
                ["Jwt:SigningKey"] = "unit-test-signing-key-0123456789abcdef",
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build();

        return new AuthService(
            dbContext,
            new TokenService(configuration),
            new AuditService(dbContext, new HttpContextAccessor()));
    }

    [Fact]
    public async Task RegisterAsync_CreatesUserRoleUserAndReturnsToken()
    {
        await using var dbContext = TestServices.CreateInMemoryContext();
        var service = CreateService(dbContext);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            FullName = "Test User",
            Email = "Test@Example.com",
            Password = "Password1!"
        });

        Assert.NotEmpty(result.AccessToken);
        Assert.Equal(UserRole.User, result.User.Role);
        Assert.Equal("test@example.com", result.User.Email); // normalized
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsEmailTaken()
    {
        await using var dbContext = TestServices.CreateInMemoryContext();
        var service = CreateService(dbContext);
        var request = new RegisterRequest
        {
            FullName = "Test User",
            Email = "dupe@example.com",
            Password = "Password1!"
        };
        await service.RegisterAsync(request);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.RegisterAsync(request));

        Assert.Equal("EMAIL_TAKEN", ex.Code);
    }

    [Fact]
    public async Task LoginAsync_CorrectCredentials_ReturnsToken()
    {
        await using var dbContext = TestServices.CreateInMemoryContext();
        var service = CreateService(dbContext);
        await service.RegisterAsync(new RegisterRequest
        {
            FullName = "Test User",
            Email = "login@example.com",
            Password = "Password1!"
        });

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "login@example.com",
            Password = "Password1!"
        });

        Assert.NotEmpty(result.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsInvalidCredentialsAndAuditsFailure()
    {
        await using var dbContext = TestServices.CreateInMemoryContext();
        var service = CreateService(dbContext);
        await service.RegisterAsync(new RegisterRequest
        {
            FullName = "Test User",
            Email = "login@example.com",
            Password = "Password1!"
        });

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "login@example.com",
            Password = "WrongPassword!"
        }));

        Assert.Equal("INVALID_CREDENTIALS", ex.Code);
        Assert.Contains(dbContext.AuditLogs, l => l.Action == AuditAction.LoginFailed);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ReturnsSameErrorAsWrongPassword()
    {
        await using var dbContext = TestServices.CreateInMemoryContext();
        var service = CreateService(dbContext);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "nobody@example.com",
            Password = "Password1!"
        }));

        Assert.Equal("INVALID_CREDENTIALS", ex.Code);
    }
}
