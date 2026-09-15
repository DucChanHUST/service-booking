using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Api.DTOs.Staffs;
using ServiceBooking.Api.Services;

namespace ServiceBooking.Api.Controllers;

[ApiController]
[Route("api/staffs")]
public class StaffsController : ControllerBase
{
  private readonly StaffManagementService _staffService;

  public StaffsController(
      StaffManagementService staffService)
  {
    _staffService = staffService;
  }

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
      return BadRequest(new
      {
        message = ex.Message
      });
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
}