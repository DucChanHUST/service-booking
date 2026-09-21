using ServiceBooking.Api.Entities;
using ServiceBooking.Api.Enums;

public static class TestDataFactory
{
  public static Service CreateService(bool isActive = true)
  {
    return new Service
    {
      Id = Guid.NewGuid(),
      Name = "Haircut",
      Description = "Test service",
      DurationMinutes = 60,
      Price = 150000,
      IsActive = isActive,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };
  }

  public static Staff CreateStaff(bool isActive = true)
  {
    return new Staff
    {
      Id = Guid.NewGuid(),
      FullName = "Test Staff",
      Email = $"staff-{Guid.NewGuid()}@test.com",
      IsActive = isActive,
      CreatedAt = DateTime.UtcNow
    };
  }

  public static WorkSchedule CreateWorkSchedule(
    Guid staffId,
    DateOnly date)
  {
    return new WorkSchedule
    {
      Id = Guid.NewGuid(),
      StaffId = staffId,
      WorkDate = date,
      StartTime = new TimeOnly(8, 0),
      EndTime = new TimeOnly(17, 0)
    };
  }

  public static User CreateUser(UserRole role)
  {
    return new User
    {
      Id = Guid.NewGuid(),
      Email = $"role-{Guid.NewGuid()}@test.com",
      PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
      Role = role,
      CreatedAt = DateTime.UtcNow
    };
  }
}