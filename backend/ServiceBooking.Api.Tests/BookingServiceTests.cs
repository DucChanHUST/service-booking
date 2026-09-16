using ServiceBooking.Api.DTOs.Bookings;
using ServiceBooking.Api.Entities;
using ServiceBooking.Api.Enums;
using ServiceBooking.Api.Services;

namespace ServiceBooking.Api.Tests;

public class BookingServiceTests
{
  [Fact]
  public async Task CreateAsync_ShouldRejectPastBooking()
  {
    // Arrange
    await using var dbContext = TestDbContextFactory.Create();

    var service = new Service
    {
      Id = Guid.NewGuid(),
      Name = "Haircut",
      DurationMinutes = 60,
      Price = 100000,
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    var staff = new Staff
    {
      Id = Guid.NewGuid(),
      FullName = "John Doe",
      Email = "john@example.com",
      IsActive = true,
      CreatedAt = DateTime.UtcNow
    };

    dbContext.Services.Add(service);
    dbContext.Staffs.Add(staff);

    await dbContext.SaveChangesAsync();

    var bookingService = new BookingService(dbContext);

    var customerId = Guid.NewGuid();

    var request = new CreateBookingRequest
    {
      ServiceId = service.Id,
      StaffId = staff.Id,
      StartTime = DateTime.Now.AddHours(-1),
      CustomerNote = "Test booking"
    };

    // Act
    var action = () =>
        bookingService.CreateAsync(customerId, request);

    // Assert
    await Assert.ThrowsAsync<ArgumentException>(action);
  }
}