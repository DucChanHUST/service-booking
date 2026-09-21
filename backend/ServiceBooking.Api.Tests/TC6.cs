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
public class BookingServiceTest6
{
  private readonly DatabaseFixture _database;

  public BookingServiceTest6(
      DatabaseFixture database)
  {
    _database = database;
  }

  [Fact]
  public async Task Customer_ShouldNotBeAbleToCancelCompletedBooking()
  {
    // Arrange
    await _database.ResetAsync();

    await using var db = _database.CreateDbContext();

    var customer = TestDataFactory.CreateUser(UserRole.Customer);
    var admin = TestDataFactory.CreateUser(UserRole.Admin);
    var service = TestDataFactory.CreateService();
    var staff = TestDataFactory.CreateStaff();

    var date = DateOnly.FromDateTime(DateTime.Now.AddDays(1));

    var schedule = TestDataFactory.CreateWorkSchedule(staff.Id, date);

    db.Users.AddRange(customer, admin);

    db.Services.Add(service);
    db.Staffs.Add(staff);
    db.WorkSchedules.Add(schedule);

    await db.SaveChangesAsync();

    await using var factory =
      new CustomWebApplicationFactory(_database.ConnectionString);

    using var client = factory.CreateClient();

    var customerLogin =
      await LoginAsync(
        client,
        customer.Email,
        "User@123");

    client.DefaultRequestHeaders.Authorization =
      new AuthenticationHeaderValue(
        "Bearer",
        customerLogin.AccessToken);

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

    var createJson = await createResponse.Content.ReadAsStringAsync();

    using var createDocument = JsonDocument.Parse(createJson);

    var bookingId = createDocument.RootElement.GetProperty("id").GetGuid();

    var initialStatus = createDocument.RootElement.GetProperty("status").GetString();

    Assert.Equal("Pending", initialStatus);

    client.DefaultRequestHeaders.Authorization = null;

    var adminLogin =
      await LoginAsync(
        client,
        admin.Email,
        "User@123");

    client.DefaultRequestHeaders.Authorization =
      new AuthenticationHeaderValue(
        "Bearer",
        adminLogin.AccessToken);

    var completeResponse =
      await client.PatchAsJsonAsync(
        $"/api/bookings/{bookingId}/status",
        new UpdateBookingStatusRequest
        {
          Status = BookingStatus.Completed
        });

    Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

    await using var verifyCompletedDb = _database.CreateDbContext();

    var completedBooking = await verifyCompletedDb.Bookings.FindAsync(bookingId);

    Assert.NotNull(completedBooking);

    Assert.Equal(BookingStatus.Completed, completedBooking.Status);

    client.DefaultRequestHeaders.Authorization = null;

    customerLogin =
      await LoginAsync(
        client,
        customer.Email,
        "User@123");

    client.DefaultRequestHeaders.Authorization =
      new AuthenticationHeaderValue(
        "Bearer",
        customerLogin.AccessToken);

    var cancelResponse =
      await client.PatchAsJsonAsync(
        $"/api/bookings/{bookingId}/cancel",
        new CancelBookingRequest
        {
          CancellationReason =
                "I want to cancel this booking."
        });

    // Assert

    Assert.Equal(HttpStatusCode.BadRequest, cancelResponse.StatusCode);

    var cancelJson = await cancelResponse.Content.ReadAsStringAsync();

    Assert.Contains("Completed booking cannot be cancelled.", cancelJson);

    await using var finalDb = _database.CreateDbContext();

    var finalBooking = await finalDb.Bookings.FindAsync(bookingId);

    Assert.NotNull(finalBooking);

    Assert.Equal(BookingStatus.Completed, finalBooking.Status);
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
