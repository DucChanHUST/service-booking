namespace ServiceBooking.Api.Entities;

public class Staff
{
  public Guid Id { get; set; }

  public string FullName { get; set; } = null!;

  public string Email { get; set; } = null!;

  public bool IsActive { get; set; }

  public DateTime CreatedAt { get; set; }

  public ICollection<WorkSchedule> WorkSchedules { get; set; }
      = new List<WorkSchedule>();

  public ICollection<Booking> Bookings { get; set; }
      = new List<Booking>();
}