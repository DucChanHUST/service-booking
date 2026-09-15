namespace ServiceBooking.Api.DTOs.Staffs;

public class StaffResponse
{
  public Guid Id { get; set; }
  public string FullName { get; set; } = null!;
  public string Email { get; set; } = null!;
  public bool IsActive { get; set; }
}