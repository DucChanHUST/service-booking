using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ServiceBooking.Api.DTOs.Auth;
using ServiceBooking.Api.DTOs.Bookings;
using ServiceBooking.Api.Enums;
using ServiceBooking.Api.Tests.Infrastructure;
using System.Text.Json;

namespace ServiceBooking.Api.Tests;

[Collection("Database collection")]
public class BookingServiceTest4
{
  private readonly DatabaseFixture _database;

  public BookingServiceTest4(
      DatabaseFixture database)
  {
    _database = database;
  }

  [Fact]
  public async Task Customer_ShouldOnlySeeOwnBookings()
  {
    // Arrange
    await _database.ResetAsync();

    await using var db = _database.CreateDbContext();

    var customerA = TestDataFactory.CreateUser(UserRole.Customer);
    var customerB = TestDataFactory.CreateUser(UserRole.Customer);
    var service = TestDataFactory.CreateService();
    var staff = TestDataFactory.CreateStaff();

    var date = DateOnly.FromDateTime(DateTime.Now.AddDays(1));

    var schedule = TestDataFactory.CreateWorkSchedule(staff.Id, date);

    db.Users.AddRange(customerA, customerB);
    db.Services.Add(service);
    db.Staffs.Add(staff);
    db.WorkSchedules.Add(schedule);

    await db.SaveChangesAsync();

    await using var factory =
      new CustomWebApplicationFactory(_database.ConnectionString);

    using var client = factory.CreateClient();

    var loginA = await LoginAsync(client, customerA.Email, "User@123");

    client.DefaultRequestHeaders.Authorization =
      new AuthenticationHeaderValue(
        "Bearer",
        loginA.AccessToken);


    var createBookingA = await client.PostAsJsonAsync(
      "/api/bookings",
      new CreateBookingRequest
      {
        ServiceId = service.Id,
        StaffId = staff.Id,
        StartTime = date.ToDateTime(new TimeOnly(10, 0))
      });

    Assert.Equal(HttpStatusCode.OK, createBookingA.StatusCode);

    var bookingAJson =
        await createBookingA.Content.ReadAsStringAsync();

    using var bookingADocument = JsonDocument.Parse(bookingAJson);

    var bookingAId = bookingADocument.RootElement.GetProperty("id").GetGuid();

    client.DefaultRequestHeaders.Authorization = null;

    var loginB = await LoginAsync(client, customerB.Email, "User@123");

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(
          "Bearer",
          loginB.AccessToken);

    var createBookingB =
      await client.PostAsJsonAsync(
        "/api/bookings",
        new CreateBookingRequest
        {
          ServiceId = service.Id,
          StaffId = staff.Id,
          StartTime = date.ToDateTime(new TimeOnly(14, 0))
        });

    Assert.Equal(
      HttpStatusCode.OK,
      createBookingB.StatusCode);

    var bookingBJson = await createBookingB.Content.ReadAsStringAsync();

    using var bookingBDocument = JsonDocument.Parse(bookingBJson);

    var bookingBId = bookingBDocument.RootElement.GetProperty("id").GetGuid();

    client.DefaultRequestHeaders.Authorization = null;

    loginA = await LoginAsync(client, customerA.Email, "User@123");

    client.DefaultRequestHeaders.Authorization =
      new AuthenticationHeaderValue("Bearer", loginA.AccessToken);

    // Act
    var response = await client.GetAsync("/api/bookings/my-bookings");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var json = await response.Content.ReadAsStringAsync();

    using var document = JsonDocument.Parse(json);

    var items = document.RootElement.GetProperty("items");

    var bookingIds = items.EnumerateArray()
      .Select(x => x.GetProperty("id").GetGuid())
      .ToList();

    Assert.Contains(bookingAId, bookingIds);

    Assert.DoesNotContain(bookingBId, bookingIds);
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

  private sealed class MyBookingsResponse
  {
    public List<BookingResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
  }
}