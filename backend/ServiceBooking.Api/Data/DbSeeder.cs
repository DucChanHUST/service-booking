using Microsoft.EntityFrameworkCore;
using ServiceBooking.Api.Entities;
using ServiceBooking.Api.Enums;

namespace ServiceBooking.Api.Data;

public static class DbSeeder
{
  public static async Task SeedAsync(
      AppDbContext db)
  {
    await db.Database.MigrateAsync();

    if (await db.Users.AnyAsync())
    {
      return;
    }

    var now = DateTime.UtcNow;

    // =========================
    // USERS
    // =========================

    var admin = new User
    {
      Id = Guid.NewGuid(),
      Email = "admin@example.com",
      PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
      Role = UserRole.Admin,
      CreatedAt = now
    };

    var customer1 = new User
    {
      Id = Guid.NewGuid(),
      Email = "customer1@example.com",
      PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
      Role = UserRole.Customer,
      CreatedAt = now
    };

    var customer2 = new User
    {
      Id = Guid.NewGuid(),
      Email = "customer2@example.com",
      PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
      Role = UserRole.Customer,
      CreatedAt = now
    };

    db.Users.AddRange(
        admin,
        customer1,
        customer2);

    // =========================
    // SERVICES
    // =========================

    var services = new List<Service>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Haircut",
                Description = "Professional haircut service",
                DurationMinutes = 60,
                Price = 150000,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },

            new()
            {
                Id = Guid.NewGuid(),
                Name = "Hair Coloring",
                Description = "Professional hair coloring",
                DurationMinutes = 120,
                Price = 400000,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },

            new()
            {
                Id = Guid.NewGuid(),
                Name = "Facial",
                Description = "Basic facial treatment",
                DurationMinutes = 60,
                Price = 250000,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },

            new()
            {
                Id = Guid.NewGuid(),
                Name = "Massage",
                Description = "Relaxing body massage",
                DurationMinutes = 90,
                Price = 300000,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },

            new()
            {
                Id = Guid.NewGuid(),
                Name = "Manicure",
                Description = "Basic manicure service",
                DurationMinutes = 45,
                Price = 120000,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

    db.Services.AddRange(services);

    // =========================
    // STAFFS
    // =========================

    var staff1 = new Staff
    {
      Id = Guid.NewGuid(),
      FullName = "Nguyen Van An",
      Email = "an@example.com",
      IsActive = true,
      CreatedAt = now
    };

    var staff2 = new Staff
    {
      Id = Guid.NewGuid(),
      FullName = "Tran Thi Binh",
      Email = "binh@example.com",
      IsActive = true,
      CreatedAt = now
    };

    db.Staffs.AddRange(staff1, staff2);

    // =========================
    // SAVE BEFORE FK DATA
    // =========================

    await db.SaveChangesAsync();

    // =========================
    // WORK SCHEDULES
    // =========================

    var schedules = new List<WorkSchedule>();

    for (var i = 0; i < 7; i++)
    {
      var date = DateOnly.FromDateTime(
          DateTime.UtcNow.AddDays(i));

      schedules.Add(
          new WorkSchedule
          {
            Id = Guid.NewGuid(),
            StaffId = staff1.Id,
            WorkDate = date,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
          });

      schedules.Add(
          new WorkSchedule
          {
            Id = Guid.NewGuid(),
            StaffId = staff2.Id,
            WorkDate = date,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
          });
    }

    db.WorkSchedules.AddRange(schedules);

    await db.SaveChangesAsync();
  }
}