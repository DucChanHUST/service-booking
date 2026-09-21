using Microsoft.EntityFrameworkCore;
using ServiceBooking.Api.Data;
using ServiceBooking.Api.DTOs.Bookings;
using ServiceBooking.Api.Entities;
using ServiceBooking.Api.Enums;
using Microsoft.AspNetCore.SignalR;
using ServiceBooking.Api.Hubs;

namespace ServiceBooking.Api.Services;

public class BookingService(
  AppDbContext dbContext,
  IHubContext<BookingHub> hubContext)
{
  private readonly AppDbContext _dbContext = dbContext;
  private readonly IHubContext<BookingHub> _hubContext = hubContext;

  public async Task<AvailableSlotsResponse> GetAvailableSlotsAsync(
      Guid serviceId,
      Guid staffId,
      DateOnly date)
  {
    var today = DateOnly.FromDateTime(DateTime.Now);
    var now = TimeOnly.FromDateTime(DateTime.Now);

    var service = await _dbContext.Services
      .AsNoTracking()
      .Where(x => x.Id == serviceId)
      .Select(x => new
      {
        x.Id,
        x.IsActive,
        x.DurationMinutes
      })
      .FirstOrDefaultAsync();

    if (service is null)
    {
      throw new KeyNotFoundException("Service not found.");
    }

    if (!service.IsActive)
    {
      throw new ArgumentException("Service is inactive.");
    }

    var staff = await _dbContext.Staffs
        .AsNoTracking()
        .Where(x => x.Id == staffId)
        .Select(x => new
        {
          x.Id,
          x.IsActive
        })
        .FirstOrDefaultAsync();

    if (staff is null)
    {
      throw new KeyNotFoundException("Staff not found.");
    }

    if (!staff.IsActive)
    {
      throw new InvalidOperationException("Staff is inactive.");
    }

    var response = new AvailableSlotsResponse
    {
      ServiceId = serviceId,
      StaffId = staffId,
      Date = date,
      DurationMinutes = service.DurationMinutes,
      AvailableRanges = []
    };

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
      .Select(x => new
      {
        x.StartTime,
        x.EndTime
      })
      .ToListAsync();

    if (schedules.Count == 0)
    {
      return response;
    }

    var localDayStart = date.ToDateTime(TimeOnly.MinValue);
    var localDayEnd = date.AddDays(1).ToDateTime(TimeOnly.MinValue);

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
      .Select(x => new
      {
        x.StartTime,
        x.EndTime
      })
      .ToListAsync();

    var localBookings = bookings
      .Select(x => new
      {
        Start = TimeOnly.FromDateTime(UtcToLocal(x.StartTime)),
        End = TimeOnly.FromDateTime(UtcToLocal(x.EndTime))
      })
      .ToList();

    foreach (var schedule in schedules)
    {
      var rangeStart = schedule.StartTime;
      var rangeEnd = schedule.EndTime;

      if (date == today && now > rangeStart)
        rangeStart = now;

      if (rangeStart >= rangeEnd)
        continue;

      var current = rangeStart;

      foreach (var booking in localBookings)
      {
        if (booking.End <= rangeStart)
        {
          continue;
        }

        if (booking.Start >= rangeEnd)
        {
          break;
        }

        if (current < booking.Start)
        {
          var freeEnd = booking.Start < rangeEnd
            ? booking.Start
            : rangeEnd;

          AddAvailableRange(
            response.AvailableRanges,
            current,
            freeEnd,
            service.DurationMinutes
          );
        }

        if (booking.End > current)
          current = booking.End;

        if (current >= rangeEnd)
          break;
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
    await using var transaction =
      await _dbContext.Database.BeginTransactionAsync();

    try
    {
      var lockKey = GetStaffLockKey(request.StaffId);

      await _dbContext.Database.ExecuteSqlInterpolatedAsync(
        $"SELECT pg_advisory_xact_lock({lockKey})"
      );

      var service = await _dbContext.Services
        .FirstOrDefaultAsync(x => x.Id == request.ServiceId);

      if (service is null)
      {
        throw new KeyNotFoundException("Service not found.");
      }

      if (!service.IsActive)
      {
        throw new ArgumentException("Service is inactive.");
      }

      var staff = await _dbContext.Staffs
        .FirstOrDefaultAsync(x => x.Id == request.StaffId);

      if (staff is null)
      {
        throw new KeyNotFoundException("Staff not found.");
      }

      if (!staff.IsActive)
      {
        throw new ArgumentException("Staff is inactive.");
      }

      var localStartTime = request.StartTime;

      if (localStartTime.Kind == DateTimeKind.Utc)
      {
        localStartTime = localStartTime.ToLocalTime();
      }
      else
      {
        localStartTime = DateTime.SpecifyKind(
          localStartTime, DateTimeKind.Unspecified
        );
      }

      if (localStartTime <= DateTime.Now)
      {
        throw new ArgumentException("Booking time cannot be in the past.");
      }

      var localEndTime = localStartTime.AddMinutes(service.DurationMinutes);
      var workDate = DateOnly.FromDateTime(localStartTime);
      var localStartOnly = TimeOnly.FromDateTime(localStartTime);
      var localEndOnly = TimeOnly.FromDateTime(localEndTime);

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
        CustomerNote = request.CustomerNote?.Trim(),
        CreatedAt = DateTime.UtcNow
      };

      _dbContext.Bookings.Add(booking);

      await _dbContext.SaveChangesAsync();

      await transaction.CommitAsync();

      var response = await GetBookingResponseAsync(booking.Id);

      await _hubContext.Clients
        .Group(BookingHub.AdminGroup)
        .SendAsync("BookingCreated", response);

      await _hubContext.Clients
        .Group(BookingHub.CustomerGroup(customerId))
        .SendAsync("BookingCreated", response);

      return response;
    }
    catch
    {
      await transaction.RollbackAsync();
      throw;
    }
  }

  public async Task<(List<BookingResponse> Items, int TotalCount)> GetMyBookingsAsync(
      Guid customerId,
      BookingStatus? status,
      DateOnly? from,
      DateOnly? to,
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

    if (from.HasValue)
    {
      var fromUtc = LocalToUtc(
        from.Value.ToDateTime(TimeOnly.MinValue));

      query = query.Where(x => x.EndTime > fromUtc);
    }

    if (to.HasValue)
    {
      var toUtc = LocalToUtc(
        to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));

      query = query.Where(x => x.StartTime < toUtc);
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
      throw new ArgumentException("Completed booking cannot be cancelled.");
    }

    if (booking.Status == BookingStatus.Cancelled)
    {
      throw new ArgumentException("Booking is already cancelled.");
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

    var response = await GetBookingResponseAsync(booking.Id);

    await _hubContext.Clients
      .Group(BookingHub.AdminGroup)
      .SendAsync("BookingCancelled", response);

    await _hubContext.Clients
      .Group(BookingHub.CustomerGroup(customerId))
      .SendAsync("BookingCancelled", response);

    return response;
  }

  public async Task<(List<BookingResponse> Items, int TotalCount)> GetAllAsync(
    DateOnly? date,
    DateOnly? from,
    DateOnly? to,
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

    if (from.HasValue)
    {
      var fromUtc = LocalToUtc(
        from.Value.ToDateTime(TimeOnly.MinValue));

      query = query.Where(x => x.EndTime > fromUtc);
    }

    if (to.HasValue)
    {
      var toUtc = LocalToUtc(
        to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));

      query = query.Where(x => x.StartTime < toUtc);
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
        CancellationReason = x.CancellationReason,

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

    var response = await GetBookingResponseAsync(booking.Id);

    await _hubContext.Clients
      .Group(BookingHub.AdminGroup)
      .SendAsync("BookingStatusUpdated", response);

    await _hubContext.Clients
      .Group(BookingHub.CustomerGroup(booking.CustomerId))
      .SendAsync("BookingStatusUpdated", response);

    return response;
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

  private static DateTime LocalToUtc(DateTime localDateTime)
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

  private static DateTime UtcToLocal(DateTime utcDateTime)
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

  public async Task CompleteExpiredBookingAsync(Guid bookingId)
  {
    var booking = await _dbContext.Bookings
      .FirstOrDefaultAsync(x => x.Id == bookingId);

    if (booking is null)
      return;

    if (booking.Status != BookingStatus.Pending &&
      booking.Status != BookingStatus.Confirmed)
    {
      return;
    }

    if (booking.EndTime > DateTime.UtcNow)
      return;

    booking.Status = BookingStatus.Completed;

    await _dbContext.SaveChangesAsync();

    var response = await GetBookingResponseAsync(booking.Id);

    await _hubContext.Clients
      .Group(BookingHub.AdminGroup)
      .SendAsync("BookingStatusUpdated", response);

    await _hubContext.Clients
      .Group(BookingHub.CustomerGroup(booking.CustomerId))
      .SendAsync("BookingStatusUpdated", response);
  }

  private static long GetStaffLockKey(Guid staffId)
  {
    return BitConverter.ToInt64(staffId.ToByteArray(), 0);
  }
}