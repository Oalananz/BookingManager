namespace BookingManager.Api.Exceptions;

public class BookingOverlapException(string message)
    : ApiException(StatusCodes.Status409Conflict, "BOOKING_CONFLICT", message);
