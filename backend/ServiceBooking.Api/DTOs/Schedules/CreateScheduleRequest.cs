namespace ServiceBooking.Api.DTOs.Schedules;

public class CreateScheduleRequest
{
  public Guid StaffId { get; set; }
  public DateOnly WorkDate { get; set; }
  public TimeOnly StartTime { get; set; }
  public TimeOnly EndTime { get; set; }
}