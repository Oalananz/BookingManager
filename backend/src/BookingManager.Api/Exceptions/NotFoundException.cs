namespace BookingManager.Api.Exceptions;

public class NotFoundException(string message)
    : ApiException(StatusCodes.Status404NotFound, "NOT_FOUND", message);
