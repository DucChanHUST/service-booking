using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ServiceBooking.Api.DTOs.Auth;
using ServiceBooking.Api.DTOs.Bookings;
using ServiceBooking.Api.Enums;
using ServiceBooking.Api.Tests.Infrastructure;

namespace ServiceBooking.Api.Tests;

[Collection("Database collection")]
public class BookingServiceTest5
{
  private readonly DatabaseFixture _database;

  public BookingServiceTest5(
      DatabaseFixture database)
  {
    _database = database;
  }

  [Fact]
  public async Task Customer_ShouldNotBeAbleToCompleteOwnBooking()
  {
    await _database.ResetAsync();

    // Arrange

    await using var db = _database.CreateDbContext();

    var customer = TestDataFactory.CreateUser(UserRole.Customer);
    var service = TestDataFactory.CreateService();
    var staff = TestDataFactory.CreateStaff();

    var date = DateOnly.FromDateTime(DateTime.Now.AddDays(1));

    var schedule = TestDataFactory.CreateWorkSchedule(staff.Id, date);

    db.Users.Add(customer);
    db.Services.Add(service);
    db.Staffs.Add(staff);
    db.WorkSchedules.Add(schedule);

    await db.SaveChangesAsync();

    await using var factory = new CustomWebApplicationFactory(_database.ConnectionString);

    using var client = factory.CreateClient();

    var login = await LoginAsync(client, customer.Email, "User@123");

    client.DefaultRequestHeaders.Authorization =
      new AuthenticationHeaderValue(
        "Bearer",
        login.AccessToken);


    var createResponse =
      await client.PostAsJsonAsync(
        "/api/bookings",
        new CreateBookingRequest
        {
          ServiceId = service.Id,
          StaffId = staff.Id,
          StartTime = date.ToDateTime(new TimeOnly(10, 0))
        });

    Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

    var bookingJson = await createResponse.Content.ReadAsStringAsync();

    using var bookingDocument = JsonDocument.Parse(bookingJson);

    var bookingId = bookingDocument.RootElement.GetProperty("id").GetGuid();

    var status = bookingDocument.RootElement.GetProperty("status").GetString();

    Assert.Equal("Pending", status);

    var updateResponse =
      await client.PatchAsJsonAsync(
        $"/api/bookings/{bookingId}/status",
        new UpdateBookingStatusRequest
        {
          Status = BookingStatus.Completed
        });

    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);

    await using var verifyDb = _database.CreateDbContext();

    var booking = await verifyDb.Bookings.FindAsync(bookingId);

    Assert.NotNull(booking);

    Assert.Equal(BookingStatus.Pending, booking.Status);
  }

  private static async Task<LoginResponse> LoginAsync(
      HttpClient client,
      string email,
      string password)
  {
    var response =
      await client.PostAsJsonAsync(
      "/api/auth/login",
      new LoginRequest
      {
        Email = email,
        Password = password
      });

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

    Assert.NotNull(result);

    return result;
  }
}