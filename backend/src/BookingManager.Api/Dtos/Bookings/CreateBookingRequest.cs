using System.ComponentModel.DataAnnotations;

namespace BookingManager.Api.Dtos.Bookings;

/// <summary>
/// Deliberately has no UserId — the booking owner is always the authenticated
/// caller, taken from the JWT. Accepting a client-supplied userId would let any
/// user create bookings on someone else's behalf.
/// </summary>
public class CreateBookingRequest
{
    [Required]
    public Guid ResourceId { get; set; }

    [Required]
    public DateTime StartDateTime { get; set; }

    [Required]
    public DateTime EndDateTime { get; set; }
}
