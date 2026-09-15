using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Api.DTOs.Schedules;
using ServiceBooking.Api.Services;

namespace ServiceBooking.Api.Controllers;

[ApiController]
[Route("api/schedules")]
public class SchedulesController : ControllerBase
{
  private readonly ScheduleManagementService _scheduleService;

  public SchedulesController(
    ScheduleManagementService scheduleService)
  {
    _scheduleService = scheduleService;
  }

  [AllowAnonymous]
  [HttpGet]
  public async Task<ActionResult<List<ScheduleResponse>>> GetSchedules(
      [FromQuery] Guid? staffId,
      [FromQuery] DateOnly? from,
      [FromQuery] DateOnly? to)
  {
    var schedules =
      await _scheduleService.GetSchedulesAsync(
        staffId,
        from,
        to);

    return Ok(schedules);
  }

  [Authorize(Roles = "Admin")]
  [HttpPost]
  public async Task<ActionResult<ScheduleResponse>> Create(
      CreateScheduleRequest request)
  {
    try
    {
      var result =
          await _scheduleService.CreateAsync(request);

      return Ok(result);
    }
    catch (ConflictException ex)
    {
      return Conflict(new
      {
        message = ex.Message
      });
    }
    catch (KeyNotFoundException ex)
    {
      return NotFound(new
      {
        message = ex.Message
      });
    }
    catch (ArgumentException ex)
    {
      return BadRequest(new
      {
        message = ex.Message
      });
    }
  }

  [Authorize(Roles = "Admin")]
  [HttpPut("{id:guid}")]
  public async Task<ActionResult<ScheduleResponse>> Update(
      Guid id,
      UpdateScheduleRequest request)
  {
    try
    {
      var result =
        await _scheduleService.UpdateAsync(
          id,
          request);

      if (result is null)
      {
        return NotFound(new
        {
          message = "Schedule not found."
        });
      }

      return Ok(result);
    }
    catch (ConflictException ex)
    {
      return Conflict(new
      {
        message = ex.Message
      });
    }
    catch (ArgumentException ex)
    {
      return BadRequest(new
      {
        message = ex.Message
      });
    }
  }

  [Authorize(Roles = "Admin")]
  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> Delete(Guid id)
  {
    var deleted =
        await _scheduleService.DeleteAsync(id);

    if (!deleted)
    {
      return NotFound(new
      {
        message = "Schedule not found."
      });
    }

    return NoContent();
  }
}