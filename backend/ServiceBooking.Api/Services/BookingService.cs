using Microsoft.EntityFrameworkCore;
using ServiceBooking.Api.Data;
using ServiceBooking.Api.DTOs.Bookings;
using ServiceBooking.Api.Entities;
using ServiceBooking.Api.Enums;

namespace ServiceBooking.Api.Services;

public class BookingService(AppDbContext dbContext)
{
  private readonly AppDbContext _dbContext = dbContext;

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

    var bookings = await _dbContext.Bookings
        .AsNoTracking()
        .Where(x =>
            x.StaffId == staffId &&
            x.Status != BookingStatus.Cancelled &&
            x.StartTime < dayEndUtc &&
            x.EndTime > dayStartUtc)
        .OrderBy(x => x.StartTime)
        .ToListAsync();

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

  public async Task<BookingResponse> CreateAsync(
      Guid customerId,
      CreateBookingRequest request)
  {
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

    if (localStartTime <= DateTime.Now)
    {
      throw new ArgumentException(
          "Booking time cannot be in the past."
      );
    }

    var localEndTime = localStartTime.AddMinutes(
        service.DurationMinutes
    );

    var workDate = DateOnly.FromDateTime(
        localStartTime
    );

    var localStartOnly = TimeOnly.FromDateTime(
        localStartTime
    );

    var localEndOnly = TimeOnly.FromDateTime(
        localEndTime
    );

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

    var startTimeUtc = LocalToUtc(localStartTime);
    var endTimeUtc = LocalToUtc(localEndTime);

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

  public async Task<(List<BookingResponse> Items, int TotalCount)> GetMyBookingsAsync(
      Guid customerId,
      BookingStatus? status,
      int page,
      int pageSize)
  {
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var query = _dbContext.Bookings
        .AsNoTracking()
        .Where(x => x.CustomerId == customerId);

    if (status.HasValue)
    {
      query = query.Where(x => x.Status == status.Value);
    }

    var totalCount = await query.CountAsync();

    var items = await query
      .Where(x => x.CustomerId == customerId)
      .OrderBy(x => x.CreatedAt)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
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
      }).ToListAsync();


    return (items, totalCount);
  }

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

  public async Task<(List<BookingResponse> Items, int TotalCount)> GetAllAsync(
      DateOnly? date,
      BookingStatus? status,
      int page,
      int pageSize)
  {
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var query = _dbContext.Bookings
        .AsNoTracking()
        .AsQueryable();

    if (date.HasValue)
    {
      var localDayStart = date.Value.ToDateTime(TimeOnly.MinValue);
      var localDayEnd = date.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
      var dayStartUtc = LocalToUtc(localDayStart);
      var dayEndUtc = LocalToUtc(localDayEnd);

      query = query.Where(x =>
          x.StartTime < dayEndUtc &&
          x.EndTime > dayStartUtc);
    }

    if (status.HasValue)
    {
      query = query.Where(x => x.Status == status.Value);
    }

    var totalCount = await query.CountAsync();

    var items = await query
        .OrderByDescending(x => x.StartTime)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
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

    return (items, totalCount);
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