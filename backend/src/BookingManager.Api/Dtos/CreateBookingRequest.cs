using System.ComponentModel.DataAnnotations;

namespace BookingManager.Api.Dtos;

public class CreateBookingRequest
{
    [Required]
    public Guid ResourceId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public DateTime StartDateTime { get; set; }

    [Required]
    public DateTime EndDateTime { get; set; }
}
