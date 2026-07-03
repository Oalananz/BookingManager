using BookingManager.Api.Data;
using BookingManager.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingManager.Api.Services;

public class ResourceService(AppDbContext dbContext) : IResourceService
{
    public Task<List<Resource>> GetAllAsync() =>
        dbContext.Resources.OrderBy(r => r.Name).ToListAsync();

    public Task<bool> ExistsAsync(Guid id) =>
        dbContext.Resources.AnyAsync(r => r.Id == id);
}
