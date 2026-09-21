using ServiceBooking.Api.Tests.Infrastructure;
using ServiceBooking.Api.Services;
using ServiceBooking.Api.DTOs.Bookings;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceBooking.Api.Tests;

[Collection("Database collection")]
public class BookingServiceTest2
{
  private readonly DatabaseFixture _database;
  public BookingServiceTest2(DatabaseFixture database)
  {
    _database = database;
  }

  [Fact]
  public async Task CreateAsync_ShouldRejectBookingOutsideWorkingHours()
  {
    // Arrange
    await _database.ResetAsync();

    await using var db = _database.CreateDbContext();

    var service = TestDataFactory.CreateService();
    var staff = TestDataFactory.CreateStaff();

    var date = DateOnly.FromDateTime(DateTime.Now.AddDays(1));

    var schedule = TestDataFactory.CreateWorkSchedule(staff.Id, date);

    db.Services.Add(service);
    db.Staffs.Add(staff);
    db.WorkSchedules.Add(schedule);

    await db.SaveChangesAsync();

    await using var provider = TestServiceProviderFactory.Create(db);

    var bookingService = provider.GetRequiredService<BookingService>();

    var request = new CreateBookingRequest
    {
      ServiceId = service.Id,
      StaffId = staff.Id,
      StartTime = date.ToDateTime(new TimeOnly(18, 0))
    };

    // Act
    var action = () => bookingService.CreateAsync(Guid.NewGuid(), request);

    // Assert
    await Assert.ThrowsAsync<ArgumentException>(action);
  }
}