namespace BookingManager.Api.Exceptions;

public class BookingOverlapException(string message) : Exception(message);
