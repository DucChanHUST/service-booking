namespace ServiceBooking.Api.DTOs.Bookings;

public class CancelBookingRequest
{
  public string CancellationReason { get; set; } = null!;
}