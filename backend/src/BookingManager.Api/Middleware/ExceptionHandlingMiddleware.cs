using BookingManager.Api.Dtos.Common;
using BookingManager.Api.Exceptions;

namespace BookingManager.Api.Middleware;

/// <summary>
/// Maps domain exceptions to the API's { code, message } error shape.
/// Unexpected exceptions are logged in full but surface only a generic message —
/// internals (SQL, stack traces) must never leak to clients.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ErrorResponse
            {
                Code = ex.Code,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception processing {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An unexpected error occurred. Please try again later."
            });
        }
    }
}
