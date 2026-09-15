namespace ServiceBooking.Api.DTOs.Services;

public class ServiceResponse
{
  public Guid Id { get; set; }
  public string Name { get; set; } = null!;
  public string? Description { get; set; }
  public int DurationMinutes { get; set; }
  public decimal Price { get; set; }
  public bool IsActive { get; set; }
}