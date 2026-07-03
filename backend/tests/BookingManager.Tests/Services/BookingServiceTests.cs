using BookingManager.Api.Dtos.Bookings;
using BookingManager.Api.Exceptions;
using BookingManager.Api.Models;
using BookingManager.Tests.Helpers;
using static BookingManager.Tests.Helpers.TestServices;

namespace BookingManager.Tests.Services;

public class BookingServiceTests
{
    private static readonly DateTime Day = DateTime.UtcNow.Date.AddDays(7);

    private static CreateBookingRequest Request(Guid resourceId, DateTime start, DateTime end) =>
        new() { ResourceId = resourceId, StartDateTime = start, EndDateTime = end };

    // --- Create -----------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ValidBooking_IsActiveAndOwnedByCaller()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext, UserOne);

        var booking = await service.CreateAsync(Request(RoomA, Day.AddHours(9), Day.AddHours(10)));

        Assert.Equal(BookingStatus.Active, booking.Status);
        Assert.Equal(UserOne, booking.UserId);
    }

    [Fact]
    public async Task CreateAsync_NonOverlappingBookings_BothSucceed()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);

        await service.CreateAsync(Request(RoomA, Day.AddHours(9), Day.AddHours(10)));
        var second = await service.CreateAsync(Request(RoomA, Day.AddHours(14), Day.AddHours(15)));

        Assert.Equal(BookingStatus.Active, second.Status);
    }

    [Fact]
    public async Task CreateAsync_BackToBackBookings_AreAllowed()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);

        await service.CreateAsync(Request(RoomA, Day.AddHours(10), Day.AddHours(11)));
        var second = await service.CreateAsync(Request(RoomA, Day.AddHours(11), Day.AddHours(12)));

        Assert.Equal(BookingStatus.Active, second.Status);
    }

    [Fact]
    public async Task CreateAsync_PartialOverlap_ThrowsBookingOverlapException()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);

        await service.CreateAsync(Request(RoomA, Day.AddHours(10), Day.AddHours(11)));

        await Assert.ThrowsAsync<BookingOverlapException>(() =>
            service.CreateAsync(Request(RoomA, Day.AddHours(10).AddMinutes(30), Day.AddHours(11).AddMinutes(30))));
    }

    [Fact]
    public async Task CreateAsync_FullyContainedOverlap_ThrowsBookingOverlapException()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);

        await service.CreateAsync(Request(RoomA, Day.AddHours(9), Day.AddHours(17)));

        await Assert.ThrowsAsync<BookingOverlapException>(() =>
            service.CreateAsync(Request(RoomA, Day.AddHours(12), Day.AddHours(13))));
    }

    [Fact]
    public async Task CreateAsync_SameSlotDifferentResource_Succeeds()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);

        await service.CreateAsync(Request(RoomA, Day.AddHours(10), Day.AddHours(11)));
        var second = await service.CreateAsync(Request(RoomB, Day.AddHours(10), Day.AddHours(11)));

        Assert.Equal(BookingStatus.Active, second.Status);
    }

    [Fact]
    public async Task CreateAsync_SameSlotAsCancelledBooking_Succeeds()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);

        var first = await service.CreateAsync(Request(RoomA, Day.AddHours(10), Day.AddHours(11)));
        await service.CancelAsync(first.Id);

        var second = await service.CreateAsync(Request(RoomA, Day.AddHours(10), Day.AddHours(11)));

        Assert.Equal(BookingStatus.Active, second.Status);
    }

    [Fact]
    public async Task CreateAsync_EndBeforeStart_ThrowsDomainValidationException()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            service.CreateAsync(Request(RoomA, Day.AddHours(11), Day.AddHours(10))));
    }

    [Fact]
    public async Task CreateAsync_StartInThePast_ThrowsWithBookingInPastCode()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);
        var yesterday = DateTime.UtcNow.AddDays(-1);

        var ex = await Assert.ThrowsAsync<DomainValidationException>(() =>
            service.CreateAsync(Request(RoomA, yesterday, yesterday.AddHours(1))));

        Assert.Equal("BOOKING_IN_PAST", ex.Code);
    }

    [Theory]
    [InlineData(5)]      // below 15-minute minimum
    [InlineData(60 * 25)] // above 24-hour maximum
    public async Task CreateAsync_UnreasonableDuration_ThrowsWithInvalidDurationCode(int minutes)
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);

        var ex = await Assert.ThrowsAsync<DomainValidationException>(() =>
            service.CreateAsync(Request(RoomA, Day.AddHours(9), Day.AddHours(9).AddMinutes(minutes))));

        Assert.Equal("INVALID_DURATION", ex.Code);
    }

    [Fact]
    public async Task CreateAsync_NonUtcTimestamp_ThrowsWithInvalidTimestampCode()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);
        var unspecified = DateTime.SpecifyKind(Day.AddHours(9), DateTimeKind.Unspecified);

        var ex = await Assert.ThrowsAsync<DomainValidationException>(() =>
            service.CreateAsync(Request(RoomA, unspecified, unspecified.AddHours(1))));

        Assert.Equal("INVALID_TIMESTAMP", ex.Code);
    }

    [Fact]
    public async Task CreateAsync_UnknownResource_ThrowsNotFoundException()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateAsync(Request(Guid.NewGuid(), Day.AddHours(10), Day.AddHours(11))));
    }

    [Fact]
    public async Task CreateAsync_DisabledResource_ThrowsResourceUnavailable()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(Request(DisabledRoom, Day.AddHours(10), Day.AddHours(11))));

        Assert.Equal("RESOURCE_UNAVAILABLE", ex.Code);
    }

    [Fact]
    public async Task CreateAsync_WritesAuditLog()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);

        var booking = await service.CreateAsync(Request(RoomA, Day.AddHours(9), Day.AddHours(10)));

        var log = Assert.Single(dbContext.AuditLogs);
        Assert.Equal(AuditAction.BookingCreated, log.Action);
        Assert.Equal(booking.Id.ToString(), log.EntityId);
    }

    // --- Retrieve ----------------------------------------------------------

    [Fact]
    public async Task GetMyBookingsAsync_ReturnsOnlyOwnBookings()
    {
        await using var dbContext = CreateInMemoryContext();
        await CreateBookingService(dbContext, UserOne)
            .CreateAsync(Request(RoomA, Day.AddHours(9), Day.AddHours(10)));
        await CreateBookingService(dbContext, UserTwo)
            .CreateAsync(Request(RoomA, Day.AddHours(11), Day.AddHours(12)));

        var result = await CreateBookingService(dbContext, UserOne)
            .GetMyBookingsAsync(new BookingQuery());

        var booking = Assert.Single(result.Data);
        Assert.Equal(UserOne, booking.UserId);
    }

    [Fact]
    public async Task GetMyBookingsAsync_FiltersByDateRange()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);

        await service.CreateAsync(Request(RoomA, Day.AddHours(9), Day.AddHours(10)));
        await service.CreateAsync(Request(RoomA, Day.AddDays(5).AddHours(9), Day.AddDays(5).AddHours(10)));

        var result = await service.GetMyBookingsAsync(new BookingQuery { From = Day, To = Day.AddDays(1) });

        var booking = Assert.Single(result.Data);
        Assert.Equal(Day.AddHours(9), booking.StartDateTime);
    }

    [Fact]
    public async Task GetMyBookingsAsync_PaginatesAndReportsTotals()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);

        for (var i = 0; i < 5; i++)
        {
            await service.CreateAsync(Request(RoomA, Day.AddHours(9 + i), Day.AddHours(10 + i)));
        }

        var page2 = await service.GetMyBookingsAsync(new BookingQuery { Page = 2, PageSize = 2 });

        Assert.Equal(2, page2.Data.Count);
        Assert.Equal(5, page2.Meta.TotalCount);
        Assert.Equal(3, page2.Meta.TotalPages);
        Assert.Equal(Day.AddHours(11), page2.Data[0].StartDateTime);
    }

    [Fact]
    public async Task GetByIdAsync_AnotherUsersBooking_ThrowsNotFound()
    {
        await using var dbContext = CreateInMemoryContext();
        var booking = await CreateBookingService(dbContext, UserOne)
            .CreateAsync(Request(RoomA, Day.AddHours(9), Day.AddHours(10)));

        // IDOR guard: user two probing user one's booking id gets a 404.
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateBookingService(dbContext, UserTwo).GetByIdAsync(booking.Id));
    }

    [Fact]
    public async Task GetByIdAsync_AdminCanReadAnyBooking()
    {
        await using var dbContext = CreateInMemoryContext();
        var booking = await CreateBookingService(dbContext, UserOne)
            .CreateAsync(Request(RoomA, Day.AddHours(9), Day.AddHours(10)));

        var fetched = await CreateBookingService(dbContext, AdminUser, isAdmin: true)
            .GetByIdAsync(booking.Id);

        Assert.Equal(booking.Id, fetched.Id);
    }

    [Fact]
    public async Task GetAllForAdminAsync_SeesAllUsersBookings()
    {
        await using var dbContext = CreateInMemoryContext();
        await CreateBookingService(dbContext, UserOne)
            .CreateAsync(Request(RoomA, Day.AddHours(9), Day.AddHours(10)));
        await CreateBookingService(dbContext, UserTwo)
            .CreateAsync(Request(RoomA, Day.AddHours(11), Day.AddHours(12)));

        var result = await CreateBookingService(dbContext, AdminUser, isAdmin: true)
            .GetAllForAdminAsync(new AdminBookingQuery());

        Assert.Equal(2, result.Data.Count);
    }

    // --- Cancel -------------------------------------------------------------

    [Fact]
    public async Task CancelAsync_SetsStatusCancelledAtAndCancelledBy()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext, UserOne);

        var booking = await service.CreateAsync(Request(RoomA, Day.AddHours(10), Day.AddHours(11)));
        var cancelled = await service.CancelAsync(booking.Id);

        Assert.Equal(BookingStatus.Cancelled, cancelled.Status);
        Assert.NotNull(cancelled.CancelledAt);
        Assert.Equal(UserOne, cancelled.CancelledBy);
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelled_Throws()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);

        var booking = await service.CreateAsync(Request(RoomA, Day.AddHours(10), Day.AddHours(11)));
        await service.CancelAsync(booking.Id);

        await Assert.ThrowsAsync<BookingAlreadyCancelledException>(() => service.CancelAsync(booking.Id));
    }

    [Fact]
    public async Task CancelAsync_AnotherUsersBooking_ThrowsNotFound()
    {
        await using var dbContext = CreateInMemoryContext();
        var booking = await CreateBookingService(dbContext, UserOne)
            .CreateAsync(Request(RoomA, Day.AddHours(10), Day.AddHours(11)));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateBookingService(dbContext, UserTwo).CancelAsync(booking.Id));
    }

    [Fact]
    public async Task CancelAsync_AdminCanCancelAnyBooking()
    {
        await using var dbContext = CreateInMemoryContext();
        var booking = await CreateBookingService(dbContext, UserOne)
            .CreateAsync(Request(RoomA, Day.AddHours(10), Day.AddHours(11)));

        var cancelled = await CreateBookingService(dbContext, AdminUser, isAdmin: true)
            .CancelAsync(booking.Id);

        Assert.Equal(BookingStatus.Cancelled, cancelled.Status);
        Assert.Equal(AdminUser, cancelled.CancelledBy);
    }

    [Fact]
    public async Task CancelAsync_CompletedBooking_ThrowsConflict()
    {
        await using var dbContext = CreateInMemoryContext();

        // Seed a booking that ended an hour ago directly (the service rightly
        // refuses to create bookings in the past).
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            ResourceId = RoomA,
            UserId = UserOne,
            StartDateTime = DateTime.UtcNow.AddHours(-2),
            EndDateTime = DateTime.UtcNow.AddHours(-1),
            Status = BookingStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            CreateBookingService(dbContext, UserOne).CancelAsync(booking.Id));

        Assert.Equal("BOOKING_ALREADY_COMPLETED", ex.Code);
    }

    // --- Availability --------------------------------------------------------

    [Fact]
    public async Task GetAvailabilityAsync_NoBookings_ReturnsFullRange()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);

        var result = await service.GetAvailabilityAsync(RoomA, Day.AddHours(8), Day.AddHours(18), 60);

        var slot = Assert.Single(result.Slots);
        Assert.Equal(Day.AddHours(8), slot.Start);
        Assert.Equal(Day.AddHours(18), slot.End);
    }

    [Fact]
    public async Task GetAvailabilityAsync_ReturnsGapsAroundBookings()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);
        await service.CreateAsync(Request(RoomA, Day.AddHours(10), Day.AddHours(11)));
        await service.CreateAsync(Request(RoomA, Day.AddHours(13), Day.AddHours(14)));

        var result = await service.GetAvailabilityAsync(RoomA, Day.AddHours(8), Day.AddHours(18), 60);

        Assert.Equal(3, result.Slots.Count);
        Assert.Equal((Day.AddHours(8), Day.AddHours(10)), (result.Slots[0].Start, result.Slots[0].End));
        Assert.Equal((Day.AddHours(11), Day.AddHours(13)), (result.Slots[1].Start, result.Slots[1].End));
        Assert.Equal((Day.AddHours(14), Day.AddHours(18)), (result.Slots[2].Start, result.Slots[2].End));
    }

    [Fact]
    public async Task GetAvailabilityAsync_ExcludesGapsShorterThanDuration()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);
        await service.CreateAsync(Request(RoomA, Day.AddHours(10), Day.AddHours(11)));
        await service.CreateAsync(Request(RoomA, Day.AddHours(11).AddMinutes(30), Day.AddHours(12)));

        // The 30-minute gap between the bookings cannot fit a 60-minute booking.
        var result = await service.GetAvailabilityAsync(RoomA, Day.AddHours(10), Day.AddHours(12), 60);

        Assert.Empty(result.Slots);
    }

    [Fact]
    public async Task GetAvailabilityAsync_FullyBooked_ReturnsNoSlots()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);
        await service.CreateAsync(Request(RoomA, Day.AddHours(8), Day.AddHours(18)));

        var result = await service.GetAvailabilityAsync(RoomA, Day.AddHours(8), Day.AddHours(18), 60);

        Assert.Empty(result.Slots);
    }

    [Fact]
    public async Task GetAvailabilityAsync_CancelledBookingDoesNotBlockSlot()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);
        var booking = await service.CreateAsync(Request(RoomA, Day.AddHours(10), Day.AddHours(11)));
        await service.CancelAsync(booking.Id);

        var result = await service.GetAvailabilityAsync(RoomA, Day.AddHours(8), Day.AddHours(18), 60);

        var slot = Assert.Single(result.Slots);
        Assert.Equal(Day.AddHours(8), slot.Start);
    }

    [Fact]
    public async Task GetAvailabilityAsync_RangeOverMaximum_Throws()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateBookingService(dbContext);

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            service.GetAvailabilityAsync(RoomA, Day, Day.AddDays(60), 60));
    }
}
