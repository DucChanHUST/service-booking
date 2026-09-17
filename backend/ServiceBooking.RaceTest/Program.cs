using Microsoft.EntityFrameworkCore;
using ServiceBooking.Api.Data;
using ServiceBooking.Api.DTOs.Bookings;
using ServiceBooking.Api.Enums;
using ServiceBooking.Api.Services;
using Microsoft.Extensions.DependencyInjection;

// ============================================================
// CONFIGURATION
// ============================================================

const string connectionString =
  "Host=localhost;" +
  "Port=5432;" +
  "Database=service_booking;" +
  "Username=postgres;" +
  "Password=postgres";

// ------------------------------------------------------------
// Change these values when you want to test another case.
// ------------------------------------------------------------

var staffId = Guid.Parse("5c691514-5aac-4b62-9197-432de96cbd83");
var serviceId = Guid.Parse("f6862885-fd97-4689-b0e4-18115fb665a5");
var bookingDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
var bookingTime = new TimeOnly(10, 0);

// ============================================================
// MAIN
// ============================================================

Console.WriteLine("==============================================");
Console.WriteLine("   SERVICE BOOKING - RACE CONDITION TEST");
Console.WriteLine("==============================================");
Console.WriteLine();

await using (var db = CreateDbContext(connectionString))
{
  // --------------------------------------------------------
  // Check database connection
  // --------------------------------------------------------

  Console.WriteLine("Checking database connection...");

  if (!await db.Database.CanConnectAsync())
  {
    Console.WriteLine("ERROR: Cannot connect to PostgreSQL.");
    return;
  }

  Console.WriteLine("Database connection: OK");
  Console.WriteLine();

  // --------------------------------------------------------
  // Get service
  // --------------------------------------------------------

  var service = await db.Services
    .AsNoTracking()
    .FirstOrDefaultAsync(x => x.Id == serviceId);

  if (service is null)
  {
    Console.WriteLine($"ERROR: Service {serviceId} not found.");
    return;
  }

  if (!service.IsActive)
  {
    Console.WriteLine($"ERROR: Service '{service.Name}' is inactive.");
    return;
  }

  // --------------------------------------------------------
  // Get staff
  // --------------------------------------------------------

  var staff = await db.Staffs
    .AsNoTracking()
    .FirstOrDefaultAsync(x => x.Id == staffId);

  if (staff is null)
  {
    Console.WriteLine($"ERROR: Staff {staffId} not found.");
    return;
  }

  if (!staff.IsActive)
  {
    Console.WriteLine($"ERROR: Staff '{staff.FullName}' is inactive.");
    return;
  }

  // --------------------------------------------------------
  // Get two customers
  // --------------------------------------------------------

  var customers = await db.Users
    .AsNoTracking()
    .Where(x => x.Role == UserRole.Customer)
    .OrderBy(x => x.Email)
    .Select(x => new
    {
      x.Id,
      x.Email
    })
    .Take(2)
    .ToListAsync();

  if (customers.Count < 2)
  {
    Console.WriteLine("ERROR: Need at least 2 customer accounts.");
    return;
  }

  // --------------------------------------------------------
  // Check schedule
  // --------------------------------------------------------

  var schedule = await db.WorkSchedules
    .AsNoTracking()
    .FirstOrDefaultAsync(x =>
      x.StaffId == staffId &&
      x.WorkDate == bookingDate);

  if (schedule is null)
  {
    Console.WriteLine(
      $"ERROR: No work schedule found for " +
      $"{staff.FullName} on {bookingDate}.");

    return;
  }

  // --------------------------------------------------------
  // Build booking time
  // --------------------------------------------------------

  var startTime = bookingDate.ToDateTime(bookingTime);

  var endTime = startTime.AddMinutes(service.DurationMinutes);

  // --------------------------------------------------------
  // Display test information
  // --------------------------------------------------------

  Console.WriteLine($"Service : {service.Name}");
  Console.WriteLine($"Duration: {service.DurationMinutes} minutes");
  Console.WriteLine($"Staff   : {staff.FullName}");
  Console.WriteLine($"Date    : {bookingDate}");
  Console.WriteLine($"Time    : {startTime:HH:mm} - {endTime:HH:mm}");
  Console.WriteLine();
  Console.WriteLine($"Customer A: {customers[0].Email}");
  Console.WriteLine($"Customer B: {customers[1].Email}");
  Console.WriteLine();

  // --------------------------------------------------------
  // Check existing bookings
  // --------------------------------------------------------

  var existingBookings = await db.Bookings
      .AsNoTracking()
      .Where(x =>
          x.StaffId == staffId &&
          x.Status != BookingStatus.Cancelled &&
          x.StartTime < endTime.ToUniversalTime() &&
          x.EndTime > startTime.ToUniversalTime())
      .ToListAsync();

  if (existingBookings.Count > 0)
  {
    Console.WriteLine(
        "ERROR: The selected time slot already has " +
        "an active booking.");

    foreach (var booking in existingBookings)
    {
      Console.WriteLine(
          $"  {booking.BookingCode} " +
          $"- {booking.Status} " +
          $"- {booking.StartTime:u} -> " +
          $"{booking.EndTime:u}");
    }

    Console.WriteLine();

    Console.WriteLine(
        "Choose another bookingTime and run again.");

    return;
  }
}

// ============================================================
// REQUESTS
// ============================================================

var requestA = new CreateBookingRequest
{
  ServiceId = serviceId,
  StaffId = staffId,
  StartTime = bookingDate.ToDateTime(bookingTime),
  CustomerNote = "Race condition test - Request A"
};

var requestB = new CreateBookingRequest
{
  ServiceId = serviceId,
  StaffId = staffId,
  StartTime = bookingDate.ToDateTime(bookingTime),
  CustomerNote = "Race condition test - Request B"
};

Guid customerA;
Guid customerB;

await using (var db = CreateDbContext(connectionString))
{
  var customers = await db.Users
    .AsNoTracking()
    .Where(x => x.Role == UserRole.Customer)
    .OrderBy(x => x.Email)
    .Select(x => new
    {
      x.Id,
      x.Email
    })
    .Take(2)
    .ToListAsync();

  customerA = customers[0].Id;
  customerB = customers[1].Id;
}

var services = new ServiceCollection();

services.AddLogging();
services.AddSignalR();

services.AddDbContext<AppDbContext>(options =>
  options.UseNpgsql(connectionString));

services.AddScoped<BookingService>();

using var provider = services.BuildServiceProvider();

var readyCount = 0;

var ready = new TaskCompletionSource(
    TaskCreationOptions.RunContinuationsAsynchronously);

async Task WaitForBothAsync()
{
  if (Interlocked.Increment(ref readyCount) == 2)
  {
    ready.SetResult();
  }

  await ready.Task;
}

async Task<BookingTestResult> RunBookingAsync(
  string requestName,
  Guid customerId,
  CreateBookingRequest request)
{
  try
  {
    Console.WriteLine($"{requestName}: ready");

    await WaitForBothAsync();

    using var scope = provider.CreateScope();

    var bookingService =
      scope.ServiceProvider.GetRequiredService<BookingService>();

    Console.WriteLine($"{requestName}: sending booking...");

    var result = await bookingService.CreateAsync(
      customerId,
      request);

    Console.WriteLine();
    Console.WriteLine($"{requestName}: SUCCESS");
    Console.WriteLine($"  Booking ID : {result.Id}");
    Console.WriteLine($"  BookingCode: {result.BookingCode}");
    Console.WriteLine($"  Status     : {result.Status}");

    return new BookingTestResult(
      requestName,
      true,
      null,
      result.Id,
      result.BookingCode);
  }
  catch (Exception ex)
  {
    Console.WriteLine();
    Console.WriteLine($"{requestName}: FAILED");
    Console.WriteLine($"  Exception: {ex.GetType().Name}");
    Console.WriteLine($"  Message  : {ex.Message}");

    return new BookingTestResult(
      requestName,
      false,
      ex,
      null,
      null);
  }
}

// ============================================================
// START RACE
// ============================================================

Console.WriteLine();
Console.WriteLine("----------------------------------------------");
Console.WriteLine("Starting concurrent booking requests...");
Console.WriteLine("----------------------------------------------");

Console.WriteLine();

var taskA = RunBookingAsync(
  "Request A",
  customerA,
  requestA);

var taskB = RunBookingAsync(
  "Request B",
  customerB,
  requestB);

var results = await Task.WhenAll(taskA, taskB);

// ============================================================
// RESULT
// ============================================================

Console.WriteLine();
Console.WriteLine("==============================================");
Console.WriteLine("                  RESULT");
Console.WriteLine("==============================================");

var successCount = results.Count(x => x.Success);

var conflictCount = results.Count(x => x.Exception is ConflictException);

Console.WriteLine($"Successful requests : {successCount}");
Console.WriteLine($"Conflict requests   : {conflictCount}");
Console.WriteLine();

// ============================================================
// VERIFY DATABASE
// ============================================================

Console.WriteLine(
    "Checking database...");

await using (var db =
    CreateDbContext(connectionString))
{
  var startUtc =
    bookingDate
      .ToDateTime(bookingTime)
      .ToUniversalTime();

  var endUtc =
    startUtc.AddMinutes(
      await db.Services
        .Where(x => x.Id == serviceId)
        .Select(x => x.DurationMinutes)
        .FirstAsync());

  var bookings = await db.Bookings
    .AsNoTracking()
    .Where(x =>
      x.StaffId == staffId &&
      x.StartTime < endUtc &&
      x.EndTime > startUtc)
    .OrderBy(x => x.CreatedAt)
    .ToListAsync();

  Console.WriteLine();

  Console.WriteLine(
      $"Bookings found in test slot: {bookings.Count}");

  foreach (var booking in bookings)
  {
    Console.WriteLine($"  {booking.BookingCode}");
    Console.WriteLine($"    Customer : {booking.CustomerId}");
    Console.WriteLine($"    Status   : {booking.Status}");
    Console.WriteLine($"    Start    : {booking.StartTime:u}");
    Console.WriteLine($"    End      : {booking.EndTime:u}");
  }

  Console.WriteLine();

  // ========================================================
  // FINAL ASSERTION
  // ========================================================

  if (successCount == 1 &&
      conflictCount == 1 &&
      bookings.Count == 1)
  {
    Console.WriteLine("PASS: Race condition was handled correctly.");
    Console.WriteLine("Only one booking was created.");
  }
  else
  {
    Console.WriteLine("FAIL: Unexpected concurrency result.");
    Console.WriteLine("Expected:");
    Console.WriteLine("  Successful requests : 1");
    Console.WriteLine("  Conflict requests   : 1");
    Console.WriteLine("  Database bookings   : 1");
  }
}

Console.WriteLine();


// ============================================================
// HELPERS
// ============================================================

static AppDbContext CreateDbContext(
  string connectionString)
{
  var options =
    new DbContextOptionsBuilder<AppDbContext>()
      .UseNpgsql(connectionString)
      .Options;

  return new AppDbContext(options);
}

record BookingTestResult(
  string RequestName,
  bool Success,
  Exception? Exception,
  Guid? BookingId,
  string? BookingCode
);