namespace BookingManager.Api.Exceptions;

public class UnauthorizedException(string message, string code = "UNAUTHORIZED")
    : ApiException(StatusCodes.Status401Unauthorized, code, message);
