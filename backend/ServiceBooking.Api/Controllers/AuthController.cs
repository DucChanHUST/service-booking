using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Api.DTOs.Auth;
using ServiceBooking.Api.Services;
using System.Security.Claims;

namespace ServiceBooking.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
  private readonly AuthService _authService;

  public AuthController(AuthService authService)
  {
    _authService = authService;
  }

  [HttpPost("login")]
  public async Task<ActionResult<LoginResponse>> Login(
      LoginRequest request)
  {
    var result = await _authService.LoginAsync(request);

    if (result is null)
    {
      return Unauthorized(new
      {
        message = "Invalid email or password."
      });
    }

    return Ok(result);
  }

  [Authorize]
  [HttpGet("me")]
  public ActionResult<UserResponse> Me()
  {
    var id = User.FindFirstValue(
        ClaimTypes.NameIdentifier);

    var email = User.FindFirstValue(
        ClaimTypes.Email);

    var role = User.FindFirstValue(
        ClaimTypes.Role);

    return Ok(new UserResponse
    {
      Id = Guid.Parse(id!),
      Email = email!,
      Role = role!
    });
  }
}