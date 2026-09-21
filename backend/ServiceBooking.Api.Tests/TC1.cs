using ServiceBooking.Api.Tests.Infrastructure;
using ServiceBooking.Api.Services;
using ServiceBooking.Api.DTOs.Bookings;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceBooking.Api.Tests;

[Collection("Database collection")]
public class BookingServiceTest1
{
  private readonly DatabaseFixture _database;
  public BookingServiceTest1(DatabaseFixture database)
  {
    _database = database;
  }

  [Fact]
  public async Task CreateAsync_ShouldRejectPastBooking()
  {
    // Arrange
    await _database.ResetAsync();

    await using var db = _database.CreateDbContext();

    var service = TestDataFactory.CreateService();
    var staff = TestDataFactory.CreateStaff();

    db.Services.Add(service);
    db.Staffs.Add(staff);

    await db.SaveChangesAsync();

    await using var provider = TestServiceProviderFactory.Create(db);

    var bookingService = provider.GetRequiredService<BookingService>();

    var request = new CreateBookingRequest
    {
      ServiceId = service.Id,
      StaffId = staff.Id,
      StartTime = DateTime.Now.AddHours(-1)
    };

    // Act
    var action = () => bookingService.CreateAsync(Guid.NewGuid(), request);

    // Assert
    await Assert.ThrowsAsync<ArgumentException>(action);
  }
}