using ServiceBooking.Api.Tests.Infrastructure;
using ServiceBooking.Api.Services;
using ServiceBooking.Api.DTOs.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceBooking.Api.Tests;

[Collection("Database collection")]
public class BookingServiceTest3
{
  private readonly DatabaseFixture _database;
  public BookingServiceTest3(DatabaseFixture database)
  {
    _database = database;
  }

  [Fact]
  public async Task CreateAsync_ShouldRejectOverlapBooking()
  {
    // Arrange
    await _database.ResetAsync();

    await using var db = _database.CreateDbContext();

    var service = TestDataFactory.CreateService();
    var staff = TestDataFactory.CreateStaff();
    var customer = TestDataFactory.CreateUser(Enums.UserRole.Customer);

    var date = DateOnly.FromDateTime(DateTime.Now.AddDays(1));

    var schedule = TestDataFactory.CreateWorkSchedule(staff.Id, date);

    db.Services.Add(service);
    db.Staffs.Add(staff);
    db.Users.Add(customer);
    db.WorkSchedules.Add(schedule);

    await db.SaveChangesAsync();

    await using var provider = TestServiceProviderFactory.Create(db);

    var bookingService = provider.GetRequiredService<BookingService>();

    var request1 = new CreateBookingRequest
    {
      ServiceId = service.Id,
      StaffId = staff.Id,
      StartTime = date.ToDateTime(new TimeOnly(10, 0))
    };

    var request2 = new CreateBookingRequest
    {
      ServiceId = service.Id,
      StaffId = staff.Id,
      StartTime = date.ToDateTime(new TimeOnly(10, 0))
    };

    // Act
    var result1 = await bookingService.CreateAsync(customer.Id, request1);

    // Assert request1
    Assert.NotNull(result1);

    var bookingCount = await db.Bookings.CountAsync();

    Assert.Equal(1, bookingCount);

    // Act + Assert request2
    var action = () => bookingService.CreateAsync(customer.Id, request2);

    await Assert.ThrowsAsync<ConflictException>(action);

    bookingCount = await db.Bookings.CountAsync();

    Assert.Equal(1, bookingCount);
  }
}