using BookingManager.Api.Data;
using BookingManager.Api.Dtos;
using BookingManager.Api.Exceptions;
using BookingManager.Api.Models;
using BookingManager.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BookingManager.Tests.Services;

[Trait("Category", "Integration")]
public class BookingConcurrencyTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=booking_manager_db;Username=booking_manager_user;Password=booking_manager_password";

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);

    [Fact]
    public async Task CreateAsync_ConcurrentOverlappingRequests_OnlyOneSucceeds()
    {
        var resourceId = Guid.NewGuid();
        var day = DateTime.UtcNow.Date.AddYears(1);
        var start = day.AddHours(10);
        var end = day.AddHours(11);

        await using (var setupContext = CreateDbContext())
        {
            setupContext.Resources.Add(new Resource
            {
                Id = resourceId,
                Name = "Concurrency Test Room",
                Type = "Room",
                CreatedAt = DateTime.UtcNow
            });
            await setupContext.SaveChangesAsync();
        }

        try
        {
            await using var contextA = CreateDbContext();
            await using var contextB = CreateDbContext();
            var serviceA = new BookingService(contextA, new ResourceService(contextA));
            var serviceB = new BookingService(contextB, new ResourceService(contextB));

            var requestA = new CreateBookingRequest
            {
                ResourceId = resourceId,
                UserId = "concurrent-user-a",
                StartDateTime = start,
                EndDateTime = end
            };
            var requestB = new CreateBookingRequest
            {
                ResourceId = resourceId,
                UserId = "concurrent-user-b",
                StartDateTime = start,
                EndDateTime = end
            };

            var taskA = serviceA.CreateAsync(requestA);
            var taskB = serviceB.CreateAsync(requestB);

            var results = await Task.WhenAll(
                taskA.ContinueWith(t => t.Exception?.InnerException),
                taskB.ContinueWith(t => t.Exception?.InnerException));

            var overlapFailures = results.Count(ex => ex is BookingOverlapException);
            var successes = results.Count(ex => ex is null);

            Assert.Equal(1, successes);
            Assert.Equal(1, overlapFailures);

            await using var verifyContext = CreateDbContext();
            var confirmedCount = await verifyContext.Bookings
                .CountAsync(b => b.ResourceId == resourceId && b.Status == BookingStatus.Confirmed);
            Assert.Equal(1, confirmedCount);
        }
        finally
        {
            await using var cleanupContext = CreateDbContext();
            cleanupContext.Bookings.RemoveRange(cleanupContext.Bookings.Where(b => b.ResourceId == resourceId));
            cleanupContext.Resources.RemoveRange(cleanupContext.Resources.Where(r => r.Id == resourceId));
            await cleanupContext.SaveChangesAsync();
        }
    }
}
