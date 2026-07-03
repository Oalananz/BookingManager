namespace BookingManager.Api.Dtos.Common;

/// <summary>Success envelope: every successful response body is { "data": ... }.</summary>
public class ApiResponse<T>(T data)
{
    public T Data { get; } = data;
}

public static class ApiResponse
{
    public static ApiResponse<T> Of<T>(T data) => new(data);
}

/// <summary>Paged success envelope: { "data": [...], "meta": {...} }.</summary>
public class PagedResponse<T>(IReadOnlyList<T> data, PaginationMeta meta)
{
    public IReadOnlyList<T> Data { get; } = data;
    public PaginationMeta Meta { get; } = meta;
}

public class PaginationMeta
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public long TotalCount { get; init; }
    public int TotalPages { get; init; }
}

/// <summary>Error body: { "code": "...", "message": "...", "errors": {...}? }.</summary>
public class ErrorResponse
{
    public required string Code { get; init; }
    public required string Message { get; init; }

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, string[]>? Errors { get; init; }
}
