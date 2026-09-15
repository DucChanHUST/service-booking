using ServiceBooking.Api.Enums;

namespace ServiceBooking.Api.Entities;

public class Booking
{
  public Guid Id { get; set; }

  public string BookingCode { get; set; } = null!;

  public Guid CustomerId { get; set; }

  public Guid ServiceId { get; set; }

  public Guid StaffId { get; set; }

  public DateTime StartTime { get; set; }

  public DateTime EndTime { get; set; }

  public BookingStatus Status { get; set; }

  public string? CustomerNote { get; set; }

  public string? CancellationReason { get; set; }

  public DateTime CreatedAt { get; set; }

  public User Customer { get; set; } = null!;

  public Service Service { get; set; } = null!;

  public Staff Staff { get; set; } = null!;
}