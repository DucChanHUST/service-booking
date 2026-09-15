namespace ServiceBooking.Api.Entities;

public class Service
{
  public Guid Id { get; set; }

  public string Name { get; set; } = null!;

  public string? Description { get; set; }

  public int DurationMinutes { get; set; }

  public decimal Price { get; set; }

  public bool IsActive { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime UpdatedAt { get; set; }

  public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}