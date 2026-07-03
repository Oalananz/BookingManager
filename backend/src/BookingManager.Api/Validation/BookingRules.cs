using BookingManager.Api.Exceptions;

namespace BookingManager.Api.Validation;

/// <summary>
/// Business rules for booking time windows. Values are deliberate assumptions,
/// documented in the README: bookings are minute-scale office reservations.
/// </summary>
public static class BookingRules
{
    public static readonly TimeSpan MinDuration = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(24);

    /// <summary>Tolerance so "book starting now" doesn't fail on clock skew.</summary>
    public static readonly TimeSpan PastGrace = TimeSpan.FromMinutes(1);

    public static readonly TimeSpan MaxAvailabilityRange = TimeSpan.FromDays(31);

    /// <summary>
    /// All API timestamps must be unambiguous UTC. Local kinds are rejected and
    /// Unspecified would otherwise be silently misinterpreted by timestamptz.
    /// </summary>
    public static DateTime EnsureUtc(DateTime value, string fieldName)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => throw new DomainValidationException(
                $"{fieldName} must be an ISO 8601 UTC timestamp (e.g. 2026-07-03T09:00:00Z).",
                "INVALID_TIMESTAMP")
        };
    }

    public static void ValidateWindow(DateTime startUtc, DateTime endUtc, DateTime nowUtc)
    {
        if (endUtc <= startUtc)
        {
            throw new DomainValidationException("EndDateTime must be after StartDateTime.");
        }

        if (startUtc < nowUtc - PastGrace)
        {
            throw new DomainValidationException(
                "Bookings cannot start in the past.", "BOOKING_IN_PAST");
        }

        var duration = endUtc - startUtc;
        if (duration < MinDuration || duration > MaxDuration)
        {
            throw new DomainValidationException(
                $"Booking duration must be between {MinDuration.TotalMinutes:0} minutes and {MaxDuration.TotalHours:0} hours.",
                "INVALID_DURATION");
        }
    }
}
