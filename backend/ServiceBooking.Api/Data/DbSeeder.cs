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
    var scheduleStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));

    for (var i = 0; i < 15; i++)
    {
      var date = scheduleStartDate.AddDays(i);

      schedules.Add(new WorkSchedule
      {
        Id = Guid.NewGuid(),
        StaffId = staff1.Id,
        WorkDate = date,
        StartTime = new TimeOnly(8, 0),
        EndTime = new TimeOnly(12, 0)
      });

      schedules.Add(new WorkSchedule
      {
        Id = Guid.NewGuid(),
        StaffId = staff2.Id,
        WorkDate = date,
        StartTime = new TimeOnly(8, 0),
        EndTime = new TimeOnly(12, 0)
      });

      schedules.Add(new WorkSchedule
      {
        Id = Guid.NewGuid(),
        StaffId = staff1.Id,
        WorkDate = date,
        StartTime = new TimeOnly(13, 30),
        EndTime = new TimeOnly(18, 0)
      });

      schedules.Add(new WorkSchedule
      {
        Id = Guid.NewGuid(),
        StaffId = staff2.Id,
        WorkDate = date,
        StartTime = new TimeOnly(13, 30),
        EndTime = new TimeOnly(18, 0)
      });
    }

    db.WorkSchedules.AddRange(schedules);

    await db.SaveChangesAsync();

    // =========================
    // BOOKINGS
    // =========================

    if (await db.Bookings.AnyAsync())
    {
      return;
    }

    var customers = await db.Users
        .Where(x => x.Role == UserRole.Customer)
        .OrderBy(x => x.Email)
        .ToListAsync();

    var staffMembers = await db.Staffs
        .OrderBy(x => x.FullName)
        .ToListAsync();

    var serviceList = await db.Services
        .OrderBy(x => x.Name)
        .ToListAsync();

    if (!customers.Any() || !staffMembers.Any() || !serviceList.Any())
    {
      return;
    }

    var bookingSeedStatus = new[]
    {
      BookingStatus.Completed,
      BookingStatus.Confirmed,
      BookingStatus.Pending,
      BookingStatus.Cancelled,
      BookingStatus.Completed,
      BookingStatus.Confirmed,
      BookingStatus.Pending,
      BookingStatus.Cancelled,
      BookingStatus.Pending,
      BookingStatus.Confirmed,
      BookingStatus.Cancelled,
      BookingStatus.Pending
    };

    var bookingDates = Enumerable.Range(0, 12)
        .Select(i => DateTime.UtcNow.AddDays(i - 6).Date)
        .ToArray();

    var bookingNotes = new[]
    {
      "Muốn cắt tóc ngắn, ưu tiên thời gian sáng.",
      "Cần chăm sóc da sau khi đi du lịch.",
      "Đặt lịch cho dịp cuối tuần.",
      "Muốn đổi sang giờ khác nếu có thể.",
      "Cần làm màu tóc và uốn nhẹ.",
      "Nhấn mạnh giữ nguyên kiểu cắt.",
      "Muốn massage thư giãn sau làm việc.",
      "Yêu cầu chăm sóc móng đơn giản.",
      "Đặt lịch cho buổi chiều.",
      "Cần hỗ trợ chăm sóc tóc gọn gàng.",
      "Đặt lịch cho khách hàng doanh nghiệp.",
      "Có thể đến muộn.",
      ""
    };

    var cancellationReasons = new[]
    {
      "Khách hàng đổi lịch cá nhân.",
      "Thay đổi kế hoạch công việc.",
      "Khách hàng hủy theo yêu cầu.",
      "Bị trùng lịch cá nhân.",
      "Khách hàng rút lịch do bận việc.",
      "Thích"
    };

    var bookings = new List<Booking>();

    for (var i = 0; i < 12; i++)
    {
      var customer = customers[i % customers.Count];
      var staff = staffMembers[i % staffMembers.Count];
      var service = serviceList[i % serviceList.Count];
      var startDate = bookingDates[i];
      var status = bookingSeedStatus[i];

      if (startDate.Date > DateTime.Today && status == BookingStatus.Completed)
      {
        status = BookingStatus.Confirmed;
      }

      var baseHour = (i % 2 == 0) ? 8 : 14;
      var startHour = baseHour + (i % 3);
      var startTime = startDate.Date.AddHours(startHour);
      var endTime = startTime.AddMinutes(service.DurationMinutes);

      bookings.Add(new Booking
      {
        Id = Guid.NewGuid(),
        BookingCode = $"BK-{startDate:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
        CustomerId = customer.Id,
        ServiceId = service.Id,
        StaffId = staff.Id,
        StartTime = startTime,
        EndTime = endTime,
        Status = status,
        CustomerNote = bookingNotes[i],
        CancellationReason = status == BookingStatus.Cancelled
            ? cancellationReasons[i % cancellationReasons.Length]
            : null,
        CreatedAt = DateTime.UtcNow.AddDays(-i)
      });
    }

    await db.Bookings.AddRangeAsync(bookings);
    await db.SaveChangesAsync();
  }
}