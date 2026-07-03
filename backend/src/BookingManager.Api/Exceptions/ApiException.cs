namespace BookingManager.Api.Exceptions;

/// <summary>
/// Base for all domain errors. Carries the HTTP status and a stable machine-readable
/// code so the exception middleware can emit the API's { code, message } error shape.
/// </summary>
public abstract class ApiException(int statusCode, string code, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
}
