namespace BookingManager.Api.Exceptions;

public class ForbiddenException(string message)
    : ApiException(StatusCodes.Status403Forbidden, "FORBIDDEN", message);
