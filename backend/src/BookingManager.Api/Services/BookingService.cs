using BookingManager.Api.Data;
using BookingManager.Api.Dtos.Bookings;
using BookingManager.Api.Dtos.Common;
using BookingManager.Api.Exceptions;
using BookingManager.Api.Mapping;
using BookingManager.Api.Models;
using BookingManager.Api.Validation;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BookingManager.Api.Services;

public class BookingService(
    AppDbContext dbContext,
    ICurrentUserService currentUser,
    IAuditService auditService) : IBookingService
{
    private const int MaxPageSize = 100;

    public async Task<BookingResponse> CreateAsync(CreateBookingRequest request)
    {
        var start = BookingRules.EnsureUtc(request.StartDateTime, nameof(request.StartDateTime));
        var end = BookingRules.EnsureUtc(request.EndDateTime, nameof(request.EndDateTime));
        BookingRules.ValidateWindow(start, end, DateTime.UtcNow);

        var resource = await dbContext.Resources
                .FirstOrDefaultAsync(r => r.Id == request.ResourceId)
            ?? throw new NotFoundException($"Resource '{request.ResourceId}' was not found.");

        if (resource.Status != ResourceStatus.Available)
        {
            throw new ConflictException(
                $"Resource '{resource.Name}' is not available for booking ({resource.Status}).",
                "RESOURCE_UNAVAILABLE");
        }

        // Fast-path pre-check so the common conflict case gets a friendly 409
        // without a failed insert. This check alone is NOT race-safe — the DB
        // exclusion constraint below is the authoritative guard.
        var hasOverlap = await dbContext.Bookings.AnyAsync(b =>
            b.ResourceId == request.ResourceId &&
            b.Status == BookingStatus.Active &&
            b.StartDateTime < end &&
            start < b.EndDateTime);

        if (hasOverlap)
        {
            throw new BookingOverlapException(
                "This resource is already booked during the selected time.");
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            ResourceId = resource.Id,
            UserId = currentUser.UserId,
            StartDateTime = start,
            EndDateTime = end,
            Status = BookingStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Resource = resource
        };

        dbContext.Bookings.Add(booking);
        auditService.Log(AuditAction.BookingCreated, "Booking", booking.Id.ToString(),
            newValue: booking.ToResponse());

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex) when (IsOverlapConflict(ex))
        {
            // A concurrent request won the race; Postgres rejected this insert
            // via the GiST exclusion constraint.
            throw new BookingOverlapException(
                "This resource is already booked during the selected time.");
        }

        return booking.ToResponse();
    }

    public Task<PagedResponse<BookingSummaryResponse>> GetMyBookingsAsync(BookingQuery query) =>
        QueryBookingsAsync(query, ownerUserId: currentUser.UserId);

    public Task<PagedResponse<BookingSummaryResponse>> GetAllForAdminAsync(AdminBookingQuery query) =>
        QueryBookingsAsync(query, ownerUserId: query.UserId);

    public async Task<BookingResponse> GetByIdAsync(Guid id)
    {
        var booking = await dbContext.Bookings.AsNoTracking()
            .Include(b => b.Resource)
            .Where(OwnedByCurrentUserOrAdmin(id))
            .FirstOrDefaultAsync()
            // 404 (not 403) for other users' bookings: revealing that the id
            // exists would leak information (IDOR hardening).
            ?? throw new NotFoundException($"Booking '{id}' was not found.");

        return booking.ToResponse();
    }

    public async Task<BookingResponse> CancelAsync(Guid id)
    {
        var booking = await dbContext.Bookings
            .Include(b => b.Resource)
            .Where(OwnedByCurrentUserOrAdmin(id))
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"Booking '{id}' was not found.");

        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new BookingAlreadyCancelledException($"Booking '{id}' is already cancelled.");
        }

        if (booking.EffectiveStatus() == BookingStatus.Completed)
        {
            throw new ConflictException(
                "This booking has already completed and can no longer be cancelled.",
                "BOOKING_ALREADY_COMPLETED");
        }

        var oldValue = booking.ToResponse();

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        booking.CancelledBy = currentUser.UserId;
        booking.UpdatedAt = DateTime.UtcNow;

        auditService.Log(AuditAction.BookingCancelled, "Booking", booking.Id.ToString(),
            oldValue: oldValue, newValue: booking.ToResponse());
        await dbContext.SaveChangesAsync();

        return booking.ToResponse();
    }

    public async Task<AvailabilityResponse> GetAvailabilityAsync(
        Guid resourceId, DateTime from, DateTime to, int durationMinutes)
    {
        var fromUtc = BookingRules.EnsureUtc(from, "from");
        var toUtc = BookingRules.EnsureUtc(to, "to");

        if (toUtc <= fromUtc)
        {
            throw new DomainValidationException("'to' must be after 'from'.");
        }
        if (toUtc - fromUtc > BookingRules.MaxAvailabilityRange)
        {
            throw new DomainValidationException(
                $"Availability range cannot exceed {BookingRules.MaxAvailabilityRange.TotalDays:0} days.");
        }

        var duration = TimeSpan.FromMinutes(durationMinutes);
        if (duration < BookingRules.MinDuration || duration > BookingRules.MaxDuration)
        {
            throw new DomainValidationException(
                $"durationMinutes must be between {BookingRules.MinDuration.TotalMinutes:0} and {BookingRules.MaxDuration.TotalMinutes:0}.",
                "INVALID_DURATION");
        }

        if (!await dbContext.Resources.AnyAsync(r => r.Id == resourceId))
        {
            throw new NotFoundException($"Resource '{resourceId}' was not found.");
        }

        // Slots in the past cannot be booked, so the search effectively starts now.
        var searchFrom = fromUtc < DateTime.UtcNow ? DateTime.UtcNow : fromUtc;
        var slots = new List<AvailabilitySlot>();

        if (searchFrom < toUtc)
        {
            // Single indexed query for the busy intervals; the gap walk below is
            // bounded by the (validated) range, never by table size.
            var busy = await dbContext.Bookings.AsNoTracking()
                .Where(b => b.ResourceId == resourceId &&
                            b.Status == BookingStatus.Active &&
                            b.StartDateTime < toUtc &&
                            searchFrom < b.EndDateTime)
                .OrderBy(b => b.StartDateTime)
                .Select(b => new { b.StartDateTime, b.EndDateTime })
                .ToListAsync();

            var cursor = searchFrom;
            foreach (var interval in busy)
            {
                if (interval.StartDateTime > cursor && interval.StartDateTime - cursor >= duration)
                {
                    slots.Add(new AvailabilitySlot { Start = cursor, End = interval.StartDateTime });
                }
                if (interval.EndDateTime > cursor)
                {
                    cursor = interval.EndDateTime;
                }
            }

            if (cursor < toUtc && toUtc - cursor >= duration)
            {
                slots.Add(new AvailabilitySlot { Start = cursor, End = toUtc });
            }
        }

        return new AvailabilityResponse
        {
            ResourceId = resourceId,
            From = fromUtc,
            To = toUtc,
            DurationMinutes = durationMinutes,
            Slots = slots
        };
    }

    private System.Linq.Expressions.Expression<Func<Booking, bool>> OwnedByCurrentUserOrAdmin(Guid id)
    {
        if (currentUser.IsAdmin)
        {
            return b => b.Id == id;
        }

        var userId = currentUser.UserId;
        return b => b.Id == id && b.UserId == userId;
    }

    private async Task<PagedResponse<BookingSummaryResponse>> QueryBookingsAsync(
        BookingQuery query, Guid? ownerUserId)
    {
        var bookings = dbContext.Bookings.AsNoTracking().AsQueryable();

        if (ownerUserId.HasValue)
        {
            bookings = bookings.Where(b => b.UserId == ownerUserId.Value);
        }
        if (query.ResourceId.HasValue)
        {
            bookings = bookings.Where(b => b.ResourceId == query.ResourceId.Value);
        }
        if (query.From.HasValue)
        {
            var from = BookingRules.EnsureUtc(query.From.Value, "from");
            bookings = bookings.Where(b => b.EndDateTime > from);
        }
        if (query.To.HasValue)
        {
            var to = BookingRules.EnsureUtc(query.To.Value, "to");
            bookings = bookings.Where(b => b.StartDateTime < to);
        }
        if (query.Status.HasValue)
        {
            // Completed is derived, so the filter is translated to stored fields.
            var now = DateTime.UtcNow;
            bookings = query.Status.Value switch
            {
                BookingStatus.Active => bookings.Where(b =>
                    b.Status == BookingStatus.Active && b.EndDateTime > now),
                BookingStatus.Completed => bookings.Where(b =>
                    b.Status == BookingStatus.Active && b.EndDateTime <= now),
                _ => bookings.Where(b => b.Status == BookingStatus.Cancelled)
            };
        }

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var totalCount = await bookings.LongCountAsync();
        var items = await bookings
            .Include(b => b.Resource)
            .OrderBy(b => b.StartDateTime)
            .ThenBy(b => b.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<BookingSummaryResponse>(
            items.Select(b => b.ToSummaryResponse()).ToList(),
            new PaginationMeta
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
    }

    private static bool IsOverlapConflict(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
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
}
