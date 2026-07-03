using System.ComponentModel.DataAnnotations;
using BookingManager.Api.Models;

namespace BookingManager.Api.Dtos.Resources;

public class CreateResourceRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Range(1, 100000)]
    public int? Capacity { get; set; }

    public ResourceStatus Status { get; set; } = ResourceStatus.Available;
}
