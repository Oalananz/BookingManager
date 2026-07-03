using BookingManager.Api.Dtos.Resources;
using BookingManager.Api.Models;

namespace BookingManager.Api.Services;

public interface IResourceService
{
    Task<List<ResourceSummaryResponse>> GetAllAsync(string? type = null, ResourceStatus? status = null);
    Task<ResourceResponse> GetByIdAsync(Guid id);
    Task<ResourceResponse> CreateAsync(CreateResourceRequest request);
    Task<ResourceResponse> UpdateAsync(Guid id, UpdateResourceRequest request);
    Task DeleteAsync(Guid id);
}
