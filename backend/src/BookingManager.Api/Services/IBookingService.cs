using BookingManager.Api.Dtos;
using BookingManager.Api.Models;

namespace BookingManager.Api.Services;

public interface IBookingService
{
    Task<Booking> CreateAsync(CreateBookingRequest request);
    Task<List<Booking>> GetForResourceAsync(Guid resourceId, DateTime? from, DateTime? to);
    Task<Booking> CancelAsync(Guid id);
}
