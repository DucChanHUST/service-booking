namespace ServiceBooking.Api.DTOs.Auth;

public class LoginResponse
{
  public string AccessToken { get; set; } = null!;

  public UserResponse User { get; set; } = null!;
}

public class UserResponse
{
  public Guid Id { get; set; }

  public string Email { get; set; } = null!;

  public string Role { get; set; } = null!;
}