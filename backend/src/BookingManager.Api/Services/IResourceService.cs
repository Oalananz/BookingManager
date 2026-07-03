using BookingManager.Api.Models;

namespace BookingManager.Api.Services;

public interface IResourceService
{
    Task<List<Resource>> GetAllAsync();
    Task<bool> ExistsAsync(Guid id);
}
