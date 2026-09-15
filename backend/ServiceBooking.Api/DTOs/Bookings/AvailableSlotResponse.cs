namespace ServiceBooking.Api.DTOs.Bookings;

public class AvailableTimeRangeResponse
{
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}