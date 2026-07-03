namespace BookingManager.Api.Exceptions;

public class DomainValidationException(string message, string code = "VALIDATION_ERROR")
    : ApiException(StatusCodes.Status400BadRequest, code, message);
