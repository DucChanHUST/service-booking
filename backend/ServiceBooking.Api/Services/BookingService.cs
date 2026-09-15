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

  // ============================================================
  // AVAILABLE SLOTS
  // ============================================================

  public async Task<AvailableSlotsResponse> GetAvailableSlotsAsync(
      Guid serviceId,
      Guid staffId,
      DateOnly date)
  {
    var service = await _dbContext.Services
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == serviceId);

    if (service is null)
    {
      throw new KeyNotFoundException(
          "Service not found."
      );
    }

    if (!service.IsActive)
    {
      throw new ArgumentException(
          "Service is inactive."
      );
    }

    var staff = await _dbContext.Staffs
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == staffId);

    if (staff is null)
    {
      throw new KeyNotFoundException(
          "Staff not found."
      );
    }

    if (!staff.IsActive)
    {
      throw new InvalidOperationException(
          "Staff is inactive."
      );
    }

    var response = new AvailableSlotsResponse
    {
      ServiceId = serviceId,
      StaffId = staffId,
      Date = date,
      DurationMinutes = service.DurationMinutes,
      AvailableRanges = []
    };

    var today = DateOnly.FromDateTime(DateTime.Now);

    // Past date => no available time.
    if (date < today)
    {
      return response;
    }

    // --------------------------------------------------------
    // Get all schedules for this staff on this date.
    // This supports multiple shifts, e.g.
    // 08:00 - 12:00
    // 13:30 - 18:00
    // --------------------------------------------------------

    var schedules = await _dbContext.WorkSchedules
        .AsNoTracking()
        .Where(x =>
            x.StaffId == staffId &&
            x.WorkDate == date)
        .OrderBy(x => x.StartTime)
        .ToListAsync();

    if (schedules.Count == 0)
    {
      return response;
    }

    // --------------------------------------------------------
    // Convert the local date boundaries to UTC.
    //
    // Booking timestamps are stored as UTC in PostgreSQL.
    // WorkSchedule is local business time.
    // --------------------------------------------------------

    var localDayStart = date.ToDateTime(
        TimeOnly.MinValue
    );

    var localDayEnd = date
        .AddDays(1)
        .ToDateTime(TimeOnly.MinValue);

    var dayStartUtc = LocalToUtc(localDayStart);
    var dayEndUtc = LocalToUtc(localDayEnd);

    // --------------------------------------------------------
    // Get all non-cancelled bookings that intersect this date.
    // --------------------------------------------------------

    var bookings = await _dbContext.Bookings
        .AsNoTracking()
        .Where(x =>
            x.StaffId == staffId &&
            x.Status != BookingStatus.Cancelled &&
            x.StartTime < dayEndUtc &&
            x.EndTime > dayStartUtc)
        .OrderBy(x => x.StartTime)
        .ToListAsync();

    // --------------------------------------------------------
    // Calculate available ranges for every work schedule.
    // --------------------------------------------------------

    foreach (var schedule in schedules)
    {
      var rangeStart = schedule.StartTime;
      var rangeEnd = schedule.EndTime;

      // For today, don't allow booking before current time.
      if (date == today)
      {
        var currentTime = TimeOnly.FromDateTime(
            DateTime.Now
        );

        if (currentTime > rangeStart)
        {
          rangeStart = currentTime;
        }
      }

      if (rangeStart >= rangeEnd)
      {
        continue;
      }

      var current = rangeStart;

      foreach (var booking in bookings)
      {
        // DB stores UTC -> convert back to local
        // before comparing with WorkSchedule.
        var bookingStartLocal =
            UtcToLocal(booking.StartTime);

        var bookingEndLocal =
            UtcToLocal(booking.EndTime);

        var bookingStart =
            TimeOnly.FromDateTime(
                bookingStartLocal
            );

        var bookingEnd =
            TimeOnly.FromDateTime(
                bookingEndLocal
            );

        // Booking doesn't intersect this schedule.
        if (
            bookingEnd <= rangeStart ||
            bookingStart >= rangeEnd
        )
        {
          continue;
        }

        // ------------------------------------------------
        // Free time before this booking.
        // ------------------------------------------------

        if (current < bookingStart)
        {
          var freeEnd =
              bookingStart < rangeEnd
                  ? bookingStart
                  : rangeEnd;

          AddAvailableRange(
              response.AvailableRanges,
              current,
              freeEnd,
              service.DurationMinutes
          );
        }

        // Move cursor after booking.
        if (bookingEnd > current)
        {
          current = bookingEnd;
        }

        if (current >= rangeEnd)
        {
          break;
        }
      }

      // ----------------------------------------------------
      // Free time after the last booking.
      // ----------------------------------------------------

      if (current < rangeEnd)
      {
        AddAvailableRange(
            response.AvailableRanges,
            current,
            rangeEnd,
            service.DurationMinutes
        );
      }
    }

    return response;
  }

  // ============================================================
  // CREATE BOOKING
  // ============================================================

  public async Task<BookingResponse> CreateAsync(
      Guid customerId,
      CreateBookingRequest request)
  {
    // --------------------------------------------------------
    // Validate service
    // --------------------------------------------------------

    var service = await _dbContext.Services
        .FirstOrDefaultAsync(x =>
            x.Id == request.ServiceId);

    if (service is null)
    {
      throw new KeyNotFoundException(
          "Service not found."
      );
    }

    if (!service.IsActive)
    {
      throw new ArgumentException(
          "Service is inactive."
      );
    }

    // --------------------------------------------------------
    // Validate staff
    // --------------------------------------------------------

    var staff = await _dbContext.Staffs
        .FirstOrDefaultAsync(x =>
            x.Id == request.StaffId);

    if (staff is null)
    {
      throw new KeyNotFoundException(
          "Staff not found."
      );
    }

    if (!staff.IsActive)
    {
      throw new ArgumentException(
          "Staff is inactive."
      );
    }

    // ========================================================
    // IMPORTANT TIMEZONE LOGIC
    //
    // request.StartTime comes from frontend as local
    // business time, e.g.
    //
    // 2026-09-16T09:30:00
    //
    // At this point we MUST NOT convert to UTC yet.
    //
    // First:
    //   local time -> validate schedule
    //
    // Then:
    //   local time -> UTC -> database
    // ========================================================

    var localStartTime = request.StartTime;

    if (localStartTime.Kind == DateTimeKind.Utc)
    {
      localStartTime =
          localStartTime.ToLocalTime();
    }
    else
    {
      localStartTime = DateTime.SpecifyKind(
          localStartTime,
          DateTimeKind.Unspecified
      );
    }

    // --------------------------------------------------------
    // Past booking validation
    // --------------------------------------------------------

    if (localStartTime <= DateTime.Now)
    {
      throw new ArgumentException(
          "Booking time cannot be in the past."
      );
    }

    // --------------------------------------------------------
    // Calculate local end time.
    // --------------------------------------------------------

    var localEndTime = localStartTime.AddMinutes(
        service.DurationMinutes
    );

    // --------------------------------------------------------
    // Extract local date/time for WorkSchedule validation.
    // --------------------------------------------------------

    var workDate = DateOnly.FromDateTime(
        localStartTime
    );

    var localStartOnly = TimeOnly.FromDateTime(
        localStartTime
    );

    var localEndOnly = TimeOnly.FromDateTime(
        localEndTime
    );

    // --------------------------------------------------------
    // Validate working schedule.
    //
    // Booking must be completely inside ONE schedule.
    //
    // Example:
    //
    // Schedule: 08:00 - 12:00
    // Booking:  09:30 - 10:30   -> valid
    //
    // Schedule: 08:00 - 12:00
    // Booking:  11:30 - 12:30   -> invalid
    // --------------------------------------------------------

    var schedule = await _dbContext.WorkSchedules
        .AsNoTracking()
        .FirstOrDefaultAsync(x =>
            x.StaffId == request.StaffId &&
            x.WorkDate == workDate &&
            x.StartTime <= localStartOnly &&
            x.EndTime >= localEndOnly
        );

    if (schedule is null)
    {
      throw new ArgumentException(
          "Booking time is outside staff working hours."
      );
    }

    // ========================================================
    // Convert LOCAL -> UTC only after all local business
    // time validations have passed.
    // ========================================================

    var startTimeUtc = LocalToUtc(localStartTime);
    var endTimeUtc = LocalToUtc(localEndTime);

    // --------------------------------------------------------
    // Check overlapping bookings.
    //
    // Cancelled bookings do NOT block the time.
    //
    // Overlap:
    //
    // NewStart < ExistingEnd
    // AND
    // NewEnd > ExistingStart
    // --------------------------------------------------------

    var hasConflict = await _dbContext.Bookings
        .AnyAsync(x =>
            x.StaffId == request.StaffId &&
            x.Status != BookingStatus.Cancelled &&
            startTimeUtc < x.EndTime &&
            endTimeUtc > x.StartTime
        );

    if (hasConflict)
    {
      throw new ConflictException(
          "The selected time conflicts with an existing booking."
      );
    }

    // --------------------------------------------------------
    // Create booking
    // --------------------------------------------------------

    var booking = new Booking
    {
      Id = Guid.NewGuid(),

      BookingCode = GenerateBookingCode(),

      CustomerId = customerId,
      ServiceId = service.Id,
      StaffId = staff.Id,

      StartTime = startTimeUtc,
      EndTime = endTimeUtc,

      Status = BookingStatus.Pending,

      CustomerNote =
            request.CustomerNote?.Trim(),

      CreatedAt = DateTime.UtcNow
    };

    _dbContext.Bookings.Add(booking);

    await _dbContext.SaveChangesAsync();

    return await GetBookingResponseAsync(
        booking.Id
    );
  }

  // ============================================================
  // CUSTOMER - MY BOOKINGS
  // ============================================================

  public async Task<List<BookingResponse>> GetMyBookingsAsync(
      Guid customerId)
  {
    return await _dbContext.Bookings
        .AsNoTracking()
        .Where(x =>
            x.CustomerId == customerId)
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
          CancellationReason =
                x.CancellationReason,

          CreatedAt = x.CreatedAt
        })
        .ToListAsync();
  }

  // ============================================================
  // CUSTOMER - CANCEL BOOKING
  // ============================================================

  public async Task<BookingResponse?> CancelAsync(
      Guid customerId,
      Guid bookingId,
      string cancellationReason)
  {
    if (string.IsNullOrWhiteSpace(
        cancellationReason))
    {
      throw new ArgumentException(
          "Cancellation reason is required."
      );
    }

    var booking = await _dbContext.Bookings
        .FirstOrDefaultAsync(x =>
            x.Id == bookingId &&
            x.CustomerId == customerId);

    if (booking is null)
    {
      return null;
    }

    if (booking.Status == BookingStatus.Completed)
    {
      throw new ArgumentException(
          "Completed booking cannot be cancelled."
      );
    }

    if (booking.Status == BookingStatus.Cancelled)
    {
      throw new ArgumentException(
          "Booking is already cancelled."
      );
    }

    // booking.StartTime is stored as UTC.
    if (booking.StartTime <= DateTime.UtcNow)
    {
      throw new ArgumentException(
          "A booking that has already started cannot be cancelled."
      );
    }

    booking.Status = BookingStatus.Cancelled;

    booking.CancellationReason =
        cancellationReason.Trim();

    await _dbContext.SaveChangesAsync();

    return await GetBookingResponseAsync(
        booking.Id
    );
  }

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
          CancellationReason =
                x.CancellationReason,

          CreatedAt = x.CreatedAt
        })
        .ToListAsync();
  }

  public async Task<BookingResponse?> UpdateStatusAsync(
      Guid bookingId,
      BookingStatus status)
  {
    var booking = await _dbContext.Bookings
        .FirstOrDefaultAsync(x =>
            x.Id == bookingId);

    if (booking is null)
    {
      return null;
    }

    if (!IsValidStatusTransition(booking.Status, status))
    {
      throw new ConflictException(
        $"Cannot change booking status from {booking.Status} to {status}.");
    }
    booking.Status = status;

    await _dbContext.SaveChangesAsync();

    return await GetBookingResponseAsync(
        booking.Id
    );
  }

  // ============================================================
  // HELPER - GET BOOKING RESPONSE
  // ============================================================

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
          CancellationReason =
                x.CancellationReason,

          CreatedAt = x.CreatedAt
        })
        .FirstAsync();

    return booking;
  }

  // ============================================================
  // HELPER - LOCAL -> UTC
  // ============================================================

  private static DateTime LocalToUtc(
      DateTime localDateTime)
  {
    var unspecified = DateTime.SpecifyKind(
        localDateTime,
        DateTimeKind.Unspecified
    );

    return TimeZoneInfo.ConvertTimeToUtc(
        unspecified,
        TimeZoneInfo.Local
    );
  }

  // ============================================================
  // HELPER - UTC -> LOCAL
  // ============================================================

  private static DateTime UtcToLocal(
      DateTime utcDateTime)
  {
    var utc = DateTime.SpecifyKind(
        utcDateTime,
        DateTimeKind.Utc
    );

    return TimeZoneInfo.ConvertTimeFromUtc(
        utc,
        TimeZoneInfo.Local
    );
  }

  // ============================================================
  // HELPER - ADD AVAILABLE RANGE
  // ============================================================

  private static void AddAvailableRange(
      List<AvailableTimeRangeResponse> ranges,
      TimeOnly start,
      TimeOnly end,
      int durationMinutes)
  {
    if (start >= end)
    {
      return;
    }

    var availableMinutes =
      (end.ToTimeSpan() -
        start.ToTimeSpan())
      .TotalMinutes;

    // Range must be long enough to contain
    // at least one complete service.
    if (availableMinutes < durationMinutes)
    {
      return;
    }

    ranges.Add(
        new AvailableTimeRangeResponse
        {
          StartTime = start,
          EndTime = end
        }
    );
  }

  private static string GenerateBookingCode()
  {
    return
        $"BK-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
  }

  private static bool IsValidStatusTransition(
    BookingStatus current,
    BookingStatus next)
  {
    return current switch
    {
      BookingStatus.Pending =>
        next is BookingStatus.Confirmed
          or BookingStatus.Cancelled,

      BookingStatus.Confirmed =>
        next is BookingStatus.Completed
          or BookingStatus.Cancelled,

      BookingStatus.Completed => false,

      BookingStatus.Cancelled => false,

      _ => false
    };
  }
}