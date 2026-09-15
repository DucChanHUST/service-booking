namespace ServiceBooking.Api.DTOs.Bookings;

public class CreateBookingRequest
{
  public Guid ServiceId { get; set; }
  public Guid StaffId { get; set; }
  public DateTime StartTime { get; set; }
  public string? CustomerNote { get; set; }
}