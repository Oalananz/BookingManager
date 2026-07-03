using BookingManager.Api.Data;
using BookingManager.Api.Dtos.Resources;
using BookingManager.Api.Exceptions;
using BookingManager.Api.Mapping;
using BookingManager.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BookingManager.Api.Services;

public class ResourceService(
    AppDbContext dbContext,
    IMemoryCache cache,
    IAuditService auditService) : IResourceService
{
    /// <summary>
    /// Resources change rarely and are read on every booking flow, so the full
    /// list is cached and filtered in memory. Every write invalidates the cache.
    /// Availability is never cached — it changes with every booking.
    /// </summary>
    private const string CacheKey = "resources:all";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<List<ResourceSummaryResponse>> GetAllAsync(string? type = null, ResourceStatus? status = null)
    {
        var all = await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            var resources = await dbContext.Resources.AsNoTracking()
                .OrderBy(r => r.Name)
                .ToListAsync();
            return resources.Select(r => r.ToSummaryResponse()).ToList();
        }) ?? [];

        IEnumerable<ResourceSummaryResponse> filtered = all;
        if (!string.IsNullOrWhiteSpace(type))
        {
            filtered = filtered.Where(r => string.Equals(r.Type, type, StringComparison.OrdinalIgnoreCase));
        }
        if (status.HasValue)
        {
            filtered = filtered.Where(r => r.Status == status.Value);
        }

        return filtered.ToList();
    }

    public async Task<ResourceResponse> GetByIdAsync(Guid id)
    {
        var resource = await dbContext.Resources.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException($"Resource '{id}' was not found.");
        return resource.ToResponse();
    }

    public async Task<ResourceResponse> CreateAsync(CreateResourceRequest request)
    {
        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            Description = request.Description?.Trim(),
            Capacity = request.Capacity,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Resources.Add(resource);
        auditService.Log(AuditAction.ResourceCreated, "Resource", resource.Id.ToString(),
            newValue: resource.ToResponse());
        await dbContext.SaveChangesAsync();

        cache.Remove(CacheKey);
        return resource.ToResponse();
    }

    public async Task<ResourceResponse> UpdateAsync(Guid id, UpdateResourceRequest request)
    {
        var resource = await dbContext.Resources.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException($"Resource '{id}' was not found.");

        var oldValue = resource.ToResponse();

        resource.Name = request.Name.Trim();
        resource.Type = request.Type.Trim();
        resource.Description = request.Description?.Trim();
        resource.Capacity = request.Capacity;
        resource.Status = request.Status;
        resource.UpdatedAt = DateTime.UtcNow;

        auditService.Log(AuditAction.ResourceUpdated, "Resource", resource.Id.ToString(),
            oldValue: oldValue, newValue: resource.ToResponse());
        await dbContext.SaveChangesAsync();

        cache.Remove(CacheKey);
        return resource.ToResponse();
    }

    /// <summary>
    /// Resources with booking history are never hard-deleted (bookings reference
    /// them); they are disabled instead, which removes them from booking flows.
    /// </summary>
    public async Task DeleteAsync(Guid id)
    {
        var resource = await dbContext.Resources.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException($"Resource '{id}' was not found.");

        var oldValue = resource.ToResponse();
        var hasBookings = await dbContext.Bookings.AnyAsync(b => b.ResourceId == id);

        if (hasBookings)
        {
            resource.Status = ResourceStatus.Disabled;
            resource.UpdatedAt = DateTime.UtcNow;
            auditService.Log(AuditAction.ResourceDeleted, "Resource", resource.Id.ToString(),
                oldValue: oldValue, newValue: resource.ToResponse());
        }
        else
        {
            dbContext.Resources.Remove(resource);
            auditService.Log(AuditAction.ResourceDeleted, "Resource", resource.Id.ToString(),
                oldValue: oldValue);
        }

        await dbContext.SaveChangesAsync();
        cache.Remove(CacheKey);
    }
}
