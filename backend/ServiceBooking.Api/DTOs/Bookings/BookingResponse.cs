using ServiceBooking.Api.Enums;

namespace ServiceBooking.Api.DTOs.Bookings;

public class BookingResponse
{
  public Guid Id { get; set; }
  public string BookingCode { get; set; } = null!;

  public Guid CustomerId { get; set; }
  public string CustomerEmail { get; set; } = null!;

  public Guid ServiceId { get; set; }
  public string ServiceName { get; set; } = null!;

  public Guid StaffId { get; set; }
  public string StaffName { get; set; } = null!;

  public DateTime StartTime { get; set; }
  public DateTime EndTime { get; set; }

  public BookingStatus Status { get; set; }

  public string? CustomerNote { get; set; }
  public string? CancellationReason { get; set; }

  public DateTime CreatedAt { get; set; }
}