using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ServiceBooking.Api.Data;
using ServiceBooking.Api.DTOs.Auth;

namespace ServiceBooking.Api.Services;

public class AuthService
{
  private readonly AppDbContext _dbContext;
  private readonly IConfiguration _configuration;

  public AuthService(
      AppDbContext dbContext,
      IConfiguration configuration)
  {
    _dbContext = dbContext;
    _configuration = configuration;
  }

  public async Task<LoginResponse?> LoginAsync(
      LoginRequest request)
  {
    var user = await _dbContext.Users
        .FirstOrDefaultAsync(x =>
            x.Email.ToLower() == request.Email.ToLower());

    if (user is null)
    {
      return null;
    }

    var passwordValid = BCrypt.Net.BCrypt.Verify(
        request.Password,
        user.PasswordHash);

    if (!passwordValid)
    {
      return null;
    }

    var token = GenerateToken(user);

    return new LoginResponse
    {
      AccessToken = token,
      User = new UserResponse
      {
        Id = user.Id,
        Email = user.Email,
        Role = user.Role.ToString()
      }
    };
  }

  private string GenerateToken(
      Entities.User user)
  {
    var key = _configuration["Jwt:Key"]
        ?? throw new InvalidOperationException(
            "JWT key is not configured.");

    var issuer = _configuration["Jwt:Issuer"];
    var audience = _configuration["Jwt:Audience"];

    var expiresInMinutes =
        _configuration.GetValue<int>("Jwt:ExpiresInMinutes");

    var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),

            new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new(
                ClaimTypes.Email,
                user.Email),

            new(
                ClaimTypes.Role,
                user.Role.ToString())
        };

    var credentials = new SigningCredentials(
        new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key)),
        SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(
            expiresInMinutes),
        signingCredentials: credentials);

    return new JwtSecurityTokenHandler()
        .WriteToken(token);
  }
}