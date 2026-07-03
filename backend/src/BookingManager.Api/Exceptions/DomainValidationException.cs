namespace BookingManager.Api.Exceptions;

public class DomainValidationException(string message) : Exception(message);
