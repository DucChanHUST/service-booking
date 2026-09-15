using ServiceBooking.Api.Enums;

namespace ServiceBooking.Api.Entities;

public class User
{
  public Guid Id { get; set; }
  public string Email { get; set; } = null!;
  public string PasswordHash { get; set; } = null!;
  public UserRole Role { get; set; }
  public DateTime CreatedAt { get; set; }
  public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}