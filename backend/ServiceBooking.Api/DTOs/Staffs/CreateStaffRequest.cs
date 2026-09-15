namespace ServiceBooking.Api.DTOs.Staffs;

public class CreateStaffRequest
{
  public string FullName { get; set; } = null!;
  public string Email { get; set; } = null!;
}