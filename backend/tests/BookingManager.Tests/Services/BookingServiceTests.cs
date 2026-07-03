using BookingManager.Api.Data;
using BookingManager.Api.Dtos;
using BookingManager.Api.Exceptions;
using BookingManager.Api.Models;
using BookingManager.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BookingManager.Tests.Services;

public class BookingServiceTests
{
    private static readonly Guid RoomA = Guid.NewGuid();
    private static readonly Guid RoomB = Guid.NewGuid();

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new AppDbContext(options);

        dbContext.Resources.AddRange(
            new Resource { Id = RoomA, Name = "Room A", Type = "Room", CreatedAt = DateTime.UtcNow },
            new Resource { Id = RoomB, Name = "Room B", Type = "Room", CreatedAt = DateTime.UtcNow });
        dbContext.SaveChanges();

        return dbContext;
    }

    private static BookingService CreateService(AppDbContext dbContext) =>
        new(dbContext, new ResourceService(dbContext));

    private static CreateBookingRequest Request(Guid resourceId, DateTime start, DateTime end, string userId = "user-1") =>
        new() { ResourceId = resourceId, UserId = userId, StartDateTime = start, EndDateTime = end };

    [Fact]
    public async Task CreateAsync_NonOverlappingBookings_BothSucceed()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var day = DateTime.UtcNow.Date;

        await service.CreateAsync(Request(RoomA, day.AddHours(9), day.AddHours(10)));
        var second = await service.CreateAsync(Request(RoomA, day.AddHours(14), day.AddHours(15)));

        Assert.Equal(BookingStatus.Confirmed, second.Status);
    }

    [Fact]
    public async Task CreateAsync_BackToBackBookings_AreAllowed()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var day = DateTime.UtcNow.Date;

        await service.CreateAsync(Request(RoomA, day.AddHours(10), day.AddHours(11)));
        var second = await service.CreateAsync(Request(RoomA, day.AddHours(11), day.AddHours(12)));

        Assert.Equal(BookingStatus.Confirmed, second.Status);
    }

    [Fact]
    public async Task CreateAsync_PartialOverlap_ThrowsBookingOverlapException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var day = DateTime.UtcNow.Date;

        await service.CreateAsync(Request(RoomA, day.AddHours(10), day.AddHours(11)));

        await Assert.ThrowsAsync<BookingOverlapException>(() =>
            service.CreateAsync(Request(RoomA, day.AddHours(10).AddMinutes(30), day.AddHours(11).AddMinutes(30))));
    }

    [Fact]
    public async Task CreateAsync_FullyContainedOverlap_ThrowsBookingOverlapException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var day = DateTime.UtcNow.Date;

        await service.CreateAsync(Request(RoomA, day.AddHours(9), day.AddHours(17)));

        await Assert.ThrowsAsync<BookingOverlapException>(() =>
            service.CreateAsync(Request(RoomA, day.AddHours(12), day.AddHours(13))));
    }

    [Fact]
    public async Task CreateAsync_SameSlotDifferentResource_Succeeds()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var day = DateTime.UtcNow.Date;

        await service.CreateAsync(Request(RoomA, day.AddHours(10), day.AddHours(11)));
        var second = await service.CreateAsync(Request(RoomB, day.AddHours(10), day.AddHours(11)));

        Assert.Equal(BookingStatus.Confirmed, second.Status);
    }

    [Fact]
    public async Task CreateAsync_SameSlotAsCancelledBooking_Succeeds()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var day = DateTime.UtcNow.Date;

        var first = await service.CreateAsync(Request(RoomA, day.AddHours(10), day.AddHours(11)));
        await service.CancelAsync(first.Id);

        var second = await service.CreateAsync(Request(RoomA, day.AddHours(10), day.AddHours(11)));

        Assert.Equal(BookingStatus.Confirmed, second.Status);
    }

    [Fact]
    public async Task CreateAsync_EndBeforeStart_ThrowsDomainValidationException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var day = DateTime.UtcNow.Date;

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            service.CreateAsync(Request(RoomA, day.AddHours(11), day.AddHours(10))));
    }

    [Fact]
    public async Task CreateAsync_UnknownResource_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var day = DateTime.UtcNow.Date;

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateAsync(Request(Guid.NewGuid(), day.AddHours(10), day.AddHours(11))));
    }

    [Fact]
    public async Task GetForResourceAsync_FiltersByDateRange()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var day = DateTime.UtcNow.Date;

        await service.CreateAsync(Request(RoomA, day.AddHours(9), day.AddHours(10)));
        await service.CreateAsync(Request(RoomA, day.AddDays(5).AddHours(9), day.AddDays(5).AddHours(10)));

        var results = await service.GetForResourceAsync(RoomA, day, day.AddDays(1));

        var result = Assert.Single(results);
        Assert.Equal(day.AddHours(9), result.StartDateTime);
    }

    [Fact]
    public async Task CancelAsync_SetsStatusAndCancelledAt()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var day = DateTime.UtcNow.Date;

        var booking = await service.CreateAsync(Request(RoomA, day.AddHours(10), day.AddHours(11)));
        var cancelled = await service.CancelAsync(booking.Id);

        Assert.Equal(BookingStatus.Cancelled, cancelled.Status);
        Assert.NotNull(cancelled.CancelledAt);
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelled_ThrowsBookingAlreadyCancelledException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var day = DateTime.UtcNow.Date;

        var booking = await service.CreateAsync(Request(RoomA, day.AddHours(10), day.AddHours(11)));
        await service.CancelAsync(booking.Id);

        await Assert.ThrowsAsync<BookingAlreadyCancelledException>(() => service.CancelAsync(booking.Id));
    }

    [Fact]
    public async Task CancelAsync_UnknownBooking_ThrowsNotFoundException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() => service.CancelAsync(Guid.NewGuid()));
    }
}
