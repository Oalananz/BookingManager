namespace BookingManager.Api.Exceptions;

public class ConflictException(string message, string code = "CONFLICT")
    : ApiException(StatusCodes.Status409Conflict, code, message);
