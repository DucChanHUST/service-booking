namespace ServiceBooking.Api.DTOs.Bookings;

public class AvailableSlotsResponse
{
  public Guid ServiceId { get; set; }
  public Guid StaffId { get; set; }
  public DateOnly Date { get; set; }
  public int DurationMinutes { get; set; }

  public List<AvailableTimeRangeResponse> AvailableRanges { get; set; } = [];
}