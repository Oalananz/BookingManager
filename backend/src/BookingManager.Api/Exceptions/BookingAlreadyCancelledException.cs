namespace BookingManager.Api.Exceptions;

public class BookingAlreadyCancelledException(string message) : Exception(message);
