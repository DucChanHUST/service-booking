using Microsoft.EntityFrameworkCore;
using ServiceBooking.Api.Data;
using ServiceBooking.Api.DTOs.Schedules;
using ServiceBooking.Api.Entities;

namespace ServiceBooking.Api.Services;

public class ScheduleManagementService(AppDbContext dbContext)
{
  private readonly AppDbContext _dbContext = dbContext;

  public async Task<List<ScheduleResponse>> GetSchedulesAsync(
    Guid? staffId,
    DateOnly? from,
    DateOnly? to)
  {
    var query = _dbContext.WorkSchedules
      .AsNoTracking()
      .Include(x => x.Staff)
      .AsQueryable();

    if (staffId.HasValue)
    {
      query = query.Where(x =>
        x.StaffId == staffId.Value);
    }

    if (from.HasValue)
    {
      query = query.Where(x =>
        x.WorkDate >= from.Value);
    }

    if (to.HasValue)
    {
      query = query.Where(x =>
        x.WorkDate <= to.Value);
    }

    return await query
      .OrderBy(x => x.WorkDate)
      .ThenBy(x => x.StartTime)
      .Select(x => new ScheduleResponse
      {
        Id = x.Id,
        StaffId = x.StaffId,
        StaffName = x.Staff.FullName,
        WorkDate = x.WorkDate,
        StartTime = x.StartTime,
        EndTime = x.EndTime
      })
      .ToListAsync();
  }

  public async Task<ScheduleResponse> CreateAsync(
    Guid staffId,  
    CreateScheduleRequest request)
  {
    ValidateTime(
      request.StartTime,
      request.EndTime);

    var staff = await _dbContext.Staffs
      .FirstOrDefaultAsync(x => x.Id == staffId);

    if (staff is null)
      throw new KeyNotFoundException("Staff not found.");

    if (!staff.IsActive)
      throw new ArgumentException("Cannot create schedule for inactive staff.");

    var overlaps = await _dbContext.WorkSchedules
      .AnyAsync(x =>
        x.StaffId == staffId &&
        x.WorkDate == request.WorkDate &&
        request.StartTime < x.EndTime &&
        request.EndTime > x.StartTime);

    if (overlaps)
      throw new ConflictException("Schedule overlaps with an existing schedule.");

    var schedule = new WorkSchedule
    {
      Id = Guid.NewGuid(),
      StaffId = staffId,
      WorkDate = request.WorkDate,
      StartTime = request.StartTime,
      EndTime = request.EndTime
    };

    _dbContext.WorkSchedules.Add(schedule);

    await _dbContext.SaveChangesAsync();

    return MapToResponse(schedule, staff.FullName);
  }

  public async Task<ScheduleResponse?> UpdateAsync(
      Guid id,
      UpdateScheduleRequest request)
  {
    ValidateTime(
      request.StartTime,
      request.EndTime);

    var schedule = await _dbContext.WorkSchedules
      .Include(x => x.Staff)
      .FirstOrDefaultAsync(x => x.Id == id);

    if (schedule is null)
      return null;

    var overlaps = await _dbContext.WorkSchedules
      .AnyAsync(x =>
        x.Id != id &&
        x.StaffId == schedule.StaffId &&
        x.WorkDate == request.WorkDate &&
        request.StartTime < x.EndTime &&
        request.EndTime > x.StartTime);

    if (overlaps)
      throw new ConflictException("Schedule overlaps with an existing schedule.");

    schedule.WorkDate = request.WorkDate;
    schedule.StartTime = request.StartTime;
    schedule.EndTime = request.EndTime;

    await _dbContext.SaveChangesAsync();

    return MapToResponse(
      schedule,
      schedule.Staff.FullName);
  }

  public async Task<bool> DeleteAsync(Guid id)
  {
    var schedule = await _dbContext.WorkSchedules
      .FirstOrDefaultAsync(x => x.Id == id);

    if (schedule is null)
      return false;

    _dbContext.WorkSchedules.Remove(schedule);

    await _dbContext.SaveChangesAsync();

    return true;
  }

  private static void ValidateTime(
    TimeOnly startTime,
    TimeOnly endTime)
  {
    if (startTime >= endTime)
    {
      throw new ArgumentException("Start time must be earlier than end time.");
    }
  }

  private static ScheduleResponse MapToResponse(
    WorkSchedule schedule,
    string staffName)
  {
    return new ScheduleResponse
    {
      Id = schedule.Id,
      StaffId = schedule.StaffId,
      StaffName = staffName,
      WorkDate = schedule.WorkDate,
      StartTime = schedule.StartTime,
      EndTime = schedule.EndTime
    };
  }
}