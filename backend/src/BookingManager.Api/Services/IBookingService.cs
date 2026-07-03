using BookingManager.Api.Dtos.Bookings;
using BookingManager.Api.Dtos.Common;

namespace BookingManager.Api.Services;

public interface IBookingService
{
    Task<BookingResponse> CreateAsync(CreateBookingRequest request);
    Task<PagedResponse<BookingSummaryResponse>> GetMyBookingsAsync(BookingQuery query);
    Task<BookingResponse> GetByIdAsync(Guid id);
    Task<BookingResponse> CancelAsync(Guid id);
    Task<PagedResponse<BookingSummaryResponse>> GetAllForAdminAsync(AdminBookingQuery query);
    Task<AvailabilityResponse> GetAvailabilityAsync(Guid resourceId, DateTime from, DateTime to, int durationMinutes);
}
