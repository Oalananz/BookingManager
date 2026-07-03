using BookingManager.Api.Data;
using BookingManager.Api.Dtos.Admin;
using BookingManager.Api.Dtos.Bookings;
using BookingManager.Api.Dtos.Common;
using BookingManager.Api.Models;
using BookingManager.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace BookingManager.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
[EnableRateLimiting("reads")]
public class AdminController(IBookingService bookingService, AppDbContext dbContext) : ControllerBase
{
    [HttpGet("bookings")]
    public async Task<IActionResult> GetAllBookings([FromQuery] AdminBookingQuery query)
    {
        return Ok(await bookingService.GetAllForAdminAsync(query));
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] AuditAction? action,
        [FromQuery] string? entityType,
        [FromQuery] Guid? actorUserId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var logs = dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (action.HasValue)
        {
            logs = logs.Where(l => l.Action == action.Value);
        }
        if (!string.IsNullOrWhiteSpace(entityType))
        {
            logs = logs.Where(l => l.EntityType == entityType);
        }
        if (actorUserId.HasValue)
        {
            logs = logs.Where(l => l.ActorUserId == actorUserId.Value);
        }

        var totalCount = await logs.LongCountAsync();
        var items = await logs
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var data = items.Select(l => new AuditLogResponse
        {
            Id = l.Id,
            ActorUserId = l.ActorUserId,
            Action = l.Action,
            EntityType = l.EntityType,
            EntityId = l.EntityId,
            OldValue = ParseJson(l.OldValue),
            NewValue = ParseJson(l.NewValue),
            IpAddress = l.IpAddress,
            CreatedAt = l.CreatedAt
        }).ToList();

        return Ok(new PagedResponse<AuditLogResponse>(data, new PaginationMeta
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        }));
    }

    private static System.Text.Json.JsonElement? ParseJson(string? json) =>
        json is null ? null : System.Text.Json.JsonDocument.Parse(json).RootElement.Clone();
}
