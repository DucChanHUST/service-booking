namespace ServiceBooking.Api.DTOs.Staffs;

public class UpdateStaffRequest
{
  public string FullName { get; set; } = null!;
  public string Email { get; set; } = null!;
  public bool IsActive { get; set; }
}