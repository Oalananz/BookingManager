namespace BookingManager.Api.Exceptions;

public class BookingAlreadyCancelledException(string message)
    : ApiException(StatusCodes.Status409Conflict, "BOOKING_ALREADY_CANCELLED", message);
