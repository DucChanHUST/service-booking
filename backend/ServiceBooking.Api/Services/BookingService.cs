using Microsoft.EntityFrameworkCore;
using ServiceBooking.Api.Data;
using ServiceBooking.Api.DTOs.Bookings;
using ServiceBooking.Api.Entities;
using ServiceBooking.Api.Enums;

namespace ServiceBooking.Api.Services;

public class BookingService
{
  private readonly AppDbContext _dbContext;

  public BookingService(AppDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<AvailableSlotsResponse> GetAvailableSlotsAsync(
      Guid serviceId,
      Guid staffId,
      DateOnly date)
  {
    var service = await _dbContext.Services
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == serviceId);

    if (service is null)
      throw new KeyNotFoundException("Service not found.");

    if (!service.IsActive)
      throw new ArgumentException("Service is inactive.");

    var staff = await _dbContext.Staffs
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == staffId);

    if (staff is null)
      throw new KeyNotFoundException("Staff not found.");

    if (!staff.IsActive)
      throw new ArgumentException("Staff is inactive.");

    var schedules = await _dbContext.WorkSchedules
      .AsNoTracking()
      .Where(x =>
        x.StaffId == staffId &&
        x.WorkDate == date)
      .OrderBy(x => x.StartTime)
      .ToListAsync();

    // Get bookings on this date

    var dateStart = date.ToDateTime(TimeOnly.MinValue);
    var dateEnd = dateStart.AddDays(1);

    var bookings = await _dbContext.Bookings
      .AsNoTracking()
      .Where(x =>
        x.StaffId == staffId &&
        x.StartTime < dateEnd &&
        x.EndTime > dateStart &&
        x.Status != BookingStatus.Cancelled)
      .OrderBy(x => x.StartTime)
      .ToListAsync();

    // Calculate available ranges
    var availableRanges =
        new List<AvailableTimeRangeResponse>();

    var now = DateTime.Now;

    foreach (var schedule in schedules)
    {
      var scheduleStart =
        date.ToDateTime(schedule.StartTime);

      var scheduleEnd =
        date.ToDateTime(schedule.EndTime);

      var current = scheduleStart;

      if (date == DateOnly.FromDateTime(now))
      {
        current = Max(current, now);
      }

      var scheduleBookings = bookings
        .Where(x =>
          x.StartTime < scheduleEnd &&
          x.EndTime > scheduleStart)
        .OrderBy(x => x.StartTime)
        .ToList();

      foreach (var booking in scheduleBookings)
      {
        if (booking.StartTime > current)
        {
          AddAvailableRange(
            availableRanges,
            current,
            booking.StartTime,
            service.DurationMinutes);
        }

        if (booking.EndTime > current)
        {
          current = booking.EndTime;
        }
      }

      if (current < scheduleEnd)
      {
        AddAvailableRange(
          availableRanges,
          current,
          scheduleEnd,
          service.DurationMinutes);
      }
    }

    return new AvailableSlotsResponse
    {
      ServiceId = serviceId,
      StaffId = staffId,
      Date = date,
      DurationMinutes = service.DurationMinutes,
      AvailableRanges = availableRanges
    };
  }
  public async Task<BookingResponse> CreateAsync(
    Guid customerId,
    CreateBookingRequest request)
  {
    var service = await _dbContext.Services
      .FirstOrDefaultAsync(x => x.Id == request.ServiceId);

    if (service is null)
      throw new KeyNotFoundException("Service not found.");

    if (!service.IsActive)
      throw new ArgumentException("Service is inactive.");

    var staff = await _dbContext.Staffs
        .FirstOrDefaultAsync(x => x.Id == request.StaffId);

    if (staff is null)
      throw new KeyNotFoundException("Staff not found.");

    if (!staff.IsActive)
      throw new ArgumentException("Staff is inactive.");

    var startTime = request.StartTime;

    if (startTime <= DateTime.Now)
    {
      throw new ArgumentException("Booking time cannot be in the past.");
    }


    var endTime =
      startTime.AddMinutes(service.DurationMinutes);

    var workDate = DateOnly.FromDateTime(startTime);

    var schedule = await _dbContext.WorkSchedules
      .FirstOrDefaultAsync(x =>
        x.StaffId == request.StaffId &&
        x.WorkDate == workDate &&
        x.StartTime <= TimeOnly.FromDateTime(startTime) &&
        x.EndTime >= TimeOnly.FromDateTime(endTime));

    if (schedule is null)
    {
      throw new ArgumentException("Booking time is outside staff working hours.");
    }

    var hasConflict = await _dbContext.Bookings
      .AnyAsync(x =>
        x.StaffId == request.StaffId &&
        x.Status != BookingStatus.Cancelled &&
        startTime < x.EndTime &&
        endTime > x.StartTime);

    if (hasConflict)
    {
      throw new ConflictException("The selected time conflicts with an existing booking.");
    }

    var booking = new Booking
    {
      Id = Guid.NewGuid(),
      BookingCode = GenerateBookingCode(),

      CustomerId = customerId,
      ServiceId = service.Id,
      StaffId = staff.Id,

      StartTime = startTime,
      EndTime = endTime,

      Status = BookingStatus.Pending,

      CustomerNote = request.CustomerNote?.Trim(),

      CreatedAt = DateTime.UtcNow
    };

    _dbContext.Bookings.Add(booking);

    await _dbContext.SaveChangesAsync();

    return await GetBookingResponseAsync(booking.Id);
  }

  public async Task<List<BookingResponse>> GetMyBookingsAsync(
      Guid customerId)
  {
    return await _dbContext.Bookings
      .AsNoTracking()
      .Where(x => x.CustomerId == customerId)
      .OrderByDescending(x => x.StartTime)
      .Select(x => new BookingResponse
      {
        Id = x.Id,
        BookingCode = x.BookingCode,

        CustomerId = x.CustomerId,
        CustomerEmail = x.Customer.Email,

        ServiceId = x.ServiceId,
        ServiceName = x.Service.Name,

        StaffId = x.StaffId,
        StaffName = x.Staff.FullName,

        StartTime = x.StartTime,
        EndTime = x.EndTime,

        Status = x.Status,

        CustomerNote = x.CustomerNote,
        CancellationReason = x.CancellationReason,

        CreatedAt = x.CreatedAt
      })
      .ToListAsync();
  }

  public async Task<BookingResponse?> CancelAsync(
      Guid customerId,
      Guid bookingId,
      string cancellationReason)
  {
    if (string.IsNullOrWhiteSpace(cancellationReason))
    {
      throw new ArgumentException("Cancellation reason is required.");
    }

    var booking = await _dbContext.Bookings
      .FirstOrDefaultAsync(x =>
        x.Id == bookingId &&
        x.CustomerId == customerId);

    if (booking is null)
      return null;

    if (booking.Status == BookingStatus.Completed)
    {
      throw new ArgumentException("Completed booking cannot be cancelled.");
    }

    if (booking.Status == BookingStatus.Cancelled)
    {
      throw new ArgumentException("Booking is already cancelled.");
    }

    if (booking.StartTime <= DateTime.Now)
    {
      throw new ArgumentException("A booking that has already started cannot be cancelled.");
    }

    booking.Status = BookingStatus.Cancelled;
    booking.CancellationReason =
        cancellationReason.Trim();

    await _dbContext.SaveChangesAsync();

    return await GetBookingResponseAsync(booking.Id);
  }

  // ADMIN
  public async Task<List<BookingResponse>> GetAllAsync()
  {
    return await _dbContext.Bookings
      .AsNoTracking()
      .OrderByDescending(x => x.StartTime)
      .Select(x => new BookingResponse
      {
        Id = x.Id,
        BookingCode = x.BookingCode,

        CustomerId = x.CustomerId,
        CustomerEmail = x.Customer.Email,

        ServiceId = x.ServiceId,
        ServiceName = x.Service.Name,

        StaffId = x.StaffId,
        StaffName = x.Staff.FullName,

        StartTime = x.StartTime,
        EndTime = x.EndTime,

        Status = x.Status,

        CustomerNote = x.CustomerNote,
        CancellationReason = x.CancellationReason,

        CreatedAt = x.CreatedAt
      })
      .ToListAsync();
  }

  public async Task<BookingResponse?> UpdateStatusAsync(
      Guid bookingId,
      BookingStatus status)
  {
    var booking = await _dbContext.Bookings
        .FirstOrDefaultAsync(x => x.Id == bookingId);

    if (booking is null)
      return null;

    if (booking.Status == BookingStatus.Cancelled &&
        status != BookingStatus.Cancelled)
    {
      throw new ArgumentException("Cancelled booking cannot be reactivated.");
    }

    booking.Status = status;

    await _dbContext.SaveChangesAsync();

    return await GetBookingResponseAsync(booking.Id);
  }

  // HELPERS
  private async Task<BookingResponse> GetBookingResponseAsync(
    Guid bookingId)
  {
    var booking = await _dbContext.Bookings
      .AsNoTracking()
      .Where(x => x.Id == bookingId)
      .Select(x => new BookingResponse
      {
        Id = x.Id,
        BookingCode = x.BookingCode,

        CustomerId = x.CustomerId,
        CustomerEmail = x.Customer.Email,

        ServiceId = x.ServiceId,
        ServiceName = x.Service.Name,

        StaffId = x.StaffId,
        StaffName = x.Staff.FullName,

        StartTime = x.StartTime,
        EndTime = x.EndTime,

        Status = x.Status,

        CustomerNote = x.CustomerNote,
        CancellationReason = x.CancellationReason,

        CreatedAt = x.CreatedAt
      })
      .FirstAsync();

    return booking;
  }

  private static string GenerateBookingCode()
  {
    return $"BK-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
  }

  private static void AddAvailableRange(
      List<AvailableTimeRangeResponse> ranges,
      DateTime start,
      DateTime end,
      int durationMinutes)
  {
    // The range is too short to contain the service.
    if (start.AddMinutes(durationMinutes) > end)
      return;

    ranges.Add(new AvailableTimeRangeResponse
    {
      StartTime = TimeOnly.FromDateTime(start),
      EndTime = TimeOnly.FromDateTime(end)
    });
  }

  private static DateTime Max(
      DateTime a,
      DateTime b)
  {
    return a > b ? a : b;
  }
}

