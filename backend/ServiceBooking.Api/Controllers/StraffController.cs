using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Api.DTOs.Schedules;
using ServiceBooking.Api.DTOs.Staffs;
using ServiceBooking.Api.Services;

namespace ServiceBooking.Api.Controllers;

[ApiController]
[Route("api/staffs")]
public class StaffsController(
  StaffManagementService staffService,
  ScheduleManagementService scheduleService) : ControllerBase
{
  private readonly StaffManagementService _staffService = staffService;
  private readonly ScheduleManagementService _scheduleService = scheduleService;

  [AllowAnonymous]
  [HttpGet]
  public async Task<ActionResult<List<StaffResponse>>> GetStaffs()
  {
    var staffs = await _staffService.GetStaffsAsync();

    return Ok(staffs);
  }

  [Authorize(Roles = "Admin")]
  [HttpPost]
  public async Task<ActionResult<StaffResponse>> Create(
      CreateStaffRequest request)
  {
    try
    {
      var result = await _staffService.CreateAsync(request);

      return Ok(result);
    }
    catch (ArgumentException ex)
    {
      return BadRequest(new { message = ex.Message });
    }
  }

  [Authorize(Roles = "Admin")]
  [HttpPut("{id:guid}")]
  public async Task<ActionResult<StaffResponse>> Update(
      Guid id,
      UpdateStaffRequest request)
  {
    try
    {
      var result =
          await _staffService.UpdateAsync(id, request);

      if (result is null)
      {
        return NotFound(new
        {
          message = "Staff not found."
        });
      }

      return Ok(result);
    }
    catch (ArgumentException ex)
    {
      return BadRequest(new
      {
        message = ex.Message
      });
    }
  }

  // Schedule

  [AllowAnonymous]
  [HttpGet("{staffId:guid}/schedules")]
  public async Task<ActionResult<List<ScheduleResponse>>> GetSchedulesById(
    Guid staffId,
    [FromQuery] DateOnly? from,
    [FromQuery] DateOnly? to)
  {
    var schedules = await _scheduleService.GetSchedulesAsync(staffId, from, to);
    return Ok(schedules);
  }

  [Authorize(Roles = "Admin")]
  [HttpPost("{staffId:guid}/schedules")]
  public async Task<ActionResult<ScheduleResponse>> CreateScheduleById(
    Guid staffId,
    CreateScheduleRequest request
  )
  {
    try
    {
      var result = await _scheduleService.CreateAsync(staffId, request);
      return Ok(result);
    }
    catch (KeyNotFoundException ex)
    {
      return NotFound(new { message = ex.Message });
    }
    catch (ArgumentException ex)
    {
      return BadRequest(new { message = ex.Message });
    }
  }

  [Authorize(Roles = "Admin")]
  [HttpPut("{scheduleId:guid}/schedules")]
  public async Task<ActionResult<ScheduleResponse>> UpdateScheduleById(
    Guid scheduleId,
    UpdateScheduleRequest request
  )
  {
    try
    {
      var result = await _scheduleService.UpdateAsync(scheduleId, request);

      if (result is null)
        return NotFound(new { message = "Schedule not found" });

      return Ok(result);
    }
    catch (ConflictException ex)
    {
      return Conflict(new { message = ex.Message });
    }
    catch (ArgumentException ex)
    {
      return BadRequest(new { message = ex.Message });
    }
  }

  [Authorize(Roles = "Admin")]
  [HttpDelete("{scheduleId:guid}/schedules")]
  public async Task<IActionResult> DeleteScheduleById(Guid scheduleId)
  {
    var deleted = await _scheduleService.DeleteAsync(scheduleId);

    if (!deleted)
    {
      return NotFound(new { message = "Schedule not found." });
    }

    return NoContent();
  }
}