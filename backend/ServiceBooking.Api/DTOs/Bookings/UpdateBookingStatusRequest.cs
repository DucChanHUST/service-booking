using ServiceBooking.Api.Enums;

namespace ServiceBooking.Api.DTOs.Bookings;

public class UpdateBookingStatusRequest
{
  public BookingStatus Status { get; set; }
}