namespace ServiceBooking.Api.DTOs.Schedules;

public class ScheduleResponse
{
  public Guid Id { get; set; }
  public Guid StaffId { get; set; }
  public string StaffName { get; set; } = null!;
  public DateOnly WorkDate { get; set; }
  public TimeOnly StartTime { get; set; }
  public TimeOnly EndTime { get; set; }
}