using BookingManager.Api.Data;
using BookingManager.Api.Dtos;
using BookingManager.Api.Exceptions;
using BookingManager.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BookingManager.Api.Services;

public class BookingService(AppDbContext dbContext, IResourceService resourceService) : IBookingService
{
    public async Task<Booking> CreateAsync(CreateBookingRequest request)
    {
        if (request.EndDateTime <= request.StartDateTime)
        {
            throw new DomainValidationException("EndDateTime must be after StartDateTime.");
        }

        if (!await resourceService.ExistsAsync(request.ResourceId))
        {
            throw new NotFoundException($"Resource '{request.ResourceId}' was not found.");
        }

        var hasOverlap = await dbContext.Bookings.AnyAsync(b =>
            b.ResourceId == request.ResourceId &&
            b.Status == BookingStatus.Confirmed &&
            b.StartDateTime < request.EndDateTime &&
            request.StartDateTime < b.EndDateTime);

        if (hasOverlap)
        {
            throw new BookingOverlapException(
                "This resource already has a confirmed booking that overlaps the requested time window.");
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            ResourceId = request.ResourceId,
            UserId = request.UserId,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Bookings.Add(booking);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex) when (IsOverlapConflict(ex))
        {
            throw new BookingOverlapException(
                "This resource already has a confirmed booking that overlaps the requested time window.");
        }

        return booking;
    }

    private static bool IsOverlapConflict(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException
                {
                    SqlState: PostgresErrorCodes.ExclusionViolation or PostgresErrorCodes.DeadlockDetected
                })
            {
                return true;
            }
        }

        return false;
    }

    public async Task<List<Booking>> GetForResourceAsync(Guid resourceId, DateTime? from, DateTime? to)
    {
        var query = dbContext.Bookings.Where(b => b.ResourceId == resourceId);

        if (from.HasValue)
        {
            query = query.Where(b => b.EndDateTime > from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(b => b.StartDateTime < to.Value);
        }

        return await query.OrderBy(b => b.StartDateTime).ToListAsync();
    }

    public async Task<Booking> CancelAsync(Guid id)
    {
        var booking = await dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException($"Booking '{id}' was not found.");

        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new BookingAlreadyCancelledException($"Booking '{id}' is already cancelled.");
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return booking;
    }
}
