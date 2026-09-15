namespace ServiceBooking.Api.DTOs.Services;

public class CreateServiceRequest
{
  public string Name { get; set; } = null!;
  public string? Description { get; set; }
  public int DurationMinutes { get; set; }
  public decimal Price { get; set; }
}