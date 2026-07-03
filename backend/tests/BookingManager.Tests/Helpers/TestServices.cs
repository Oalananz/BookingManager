using BookingManager.Api.Data;
using BookingManager.Api.Models;
using BookingManager.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BookingManager.Tests.Helpers;

public class FakeCurrentUser(Guid userId, bool isAdmin = false) : ICurrentUserService
{
    public Guid UserId => userId;
    public bool IsAdmin => isAdmin;
    public string? IpAddress => "127.0.0.1";
}

public static class TestServices
{
    public static readonly Guid RoomA = Guid.NewGuid();
    public static readonly Guid RoomB = Guid.NewGuid();
    public static readonly Guid DisabledRoom = Guid.NewGuid();
    public static readonly Guid UserOne = Guid.NewGuid();
    public static readonly Guid UserTwo = Guid.NewGuid();
    public static readonly Guid AdminUser = Guid.NewGuid();

    public static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new AppDbContext(options);

        dbContext.Resources.AddRange(
            new Resource { Id = RoomA, Name = "Room A", Type = "Room", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Resource { Id = RoomB, Name = "Room B", Type = "Room", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Resource
            {
                Id = DisabledRoom,
                Name = "Disabled Room",
                Type = "Room",
                Status = ResourceStatus.Disabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        dbContext.SaveChanges();

        return dbContext;
    }

    public static BookingService CreateBookingService(
        AppDbContext dbContext, Guid? userId = null, bool isAdmin = false)
    {
        var currentUser = new FakeCurrentUser(userId ?? UserOne, isAdmin);
        return new BookingService(dbContext, currentUser, new AuditService(dbContext, new HttpContextAccessor()));
    }
}
