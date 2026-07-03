using BookingManager.Api.Data;
using BookingManager.Api.Dtos.Bookings;
using BookingManager.Api.Exceptions;
using BookingManager.Api.Models;
using BookingManager.Api.Services;
using BookingManager.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BookingManager.Tests.Services;

/// <summary>
/// Requires the docker-compose PostgreSQL instance (port 5433). This is the
/// proof for the extension task: the InMemory unit tests exercise the
/// application-level pre-check, but only real Postgres exercises the GiST
/// exclusion constraint that makes concurrent double-booking impossible.
/// </summary>
[Trait("Category", "Integration")]
public class BookingConcurrencyTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=booking_manager_db;Username=booking_manager_user;Password=booking_manager_password";

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);

    private static BookingService CreateService(AppDbContext dbContext, Guid userId) =>
        new(dbContext, new FakeCurrentUser(userId), new AuditService(dbContext, new HttpContextAccessor()));

    [Fact]
    public async Task CreateAsync_ConcurrentOverlappingRequests_OnlyOneSucceeds()
    {
        var resourceId = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var day = DateTime.UtcNow.Date.AddYears(1);
        var start = DateTime.SpecifyKind(day.AddHours(10), DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(day.AddHours(11), DateTimeKind.Utc);

        await using (var setupContext = CreateDbContext())
        {
            setupContext.Resources.Add(new Resource
            {
                Id = resourceId,
                Name = "Concurrency Test Room",
                Type = "Room",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            setupContext.Users.AddRange(
                MakeUser(userA, "concurrency-a"),
                MakeUser(userB, "concurrency-b"));
            await setupContext.SaveChangesAsync();
        }

        try
        {
            await using var contextA = CreateDbContext();
            await using var contextB = CreateDbContext();

            var request = new CreateBookingRequest
            {
                ResourceId = resourceId,
                StartDateTime = start,
                EndDateTime = end
            };

            var taskA = CreateService(contextA, userA).CreateAsync(request);
            var taskB = CreateService(contextB, userB).CreateAsync(request);

            var results = await Task.WhenAll(
                taskA.ContinueWith(t => t.Exception?.InnerException),
                taskB.ContinueWith(t => t.Exception?.InnerException));

            var overlapFailures = results.Count(ex => ex is BookingOverlapException);
            var successes = results.Count(ex => ex is null);

            Assert.Equal(1, successes);
            Assert.Equal(1, overlapFailures);

            await using var verifyContext = CreateDbContext();
            var activeCount = await verifyContext.Bookings
                .CountAsync(b => b.ResourceId == resourceId && b.Status == BookingStatus.Active);
            Assert.Equal(1, activeCount);
        }
        finally
        {
            await using var cleanupContext = CreateDbContext();
            cleanupContext.AuditLogs.RemoveRange(
                cleanupContext.AuditLogs.Where(l => l.ActorUserId == userA || l.ActorUserId == userB));
            cleanupContext.Bookings.RemoveRange(cleanupContext.Bookings.Where(b => b.ResourceId == resourceId));
            cleanupContext.Resources.RemoveRange(cleanupContext.Resources.Where(r => r.Id == resourceId));
            cleanupContext.Users.RemoveRange(cleanupContext.Users.Where(u => u.Id == userA || u.Id == userB));
            await cleanupContext.SaveChangesAsync();
        }
    }

    private static User MakeUser(Guid id, string name) => new()
    {
        Id = id,
        FullName = name,
        Email = $"{name}-{id:N}@test.local",
        PasswordHash = "not-a-real-hash",
        Role = UserRole.User,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
