namespace ServiceBooking.Api.Services;

public class ConflictException(string message) : Exception(message)
{
}