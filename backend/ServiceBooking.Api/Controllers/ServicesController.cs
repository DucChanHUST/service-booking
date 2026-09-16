using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Api.DTOs.Services;
using ServiceBooking.Api.Services;

namespace ServiceBooking.Api.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController(
    ServiceManagementService serviceManagementService) : ControllerBase
{
  private readonly ServiceManagementService _serviceManagementService = serviceManagementService;

  [AllowAnonymous]
  [HttpGet]
  public async Task<ActionResult> GetServices(
      [FromQuery] string? search,
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 10)
  {
    var (items, totalCount) =
        await _serviceManagementService.GetServicesAsync(
            search,
            page,
            pageSize);

    return Ok(new
    {
      items,
      page,
      pageSize,
      totalCount,
      totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
    });
  }

  [Authorize(Roles = "Admin")]
  [HttpPost]
  public async Task<ActionResult<ServiceResponse>> Create(
      CreateServiceRequest request)
  {
    try
    {
      var result =
          await _serviceManagementService.CreateAsync(request);

      return CreatedAtAction(
          nameof(GetServices),
          new { id = result.Id },
          result);
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
  public async Task<ActionResult<ServiceResponse>> Update(
      Guid id,
      UpdateServiceRequest request)
  {
    try
    {
      var result =
          await _serviceManagementService.UpdateAsync(
              id,
              request);

      if (result is null)
      {
        return NotFound(new
        {
          message = "Service not found."
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

  [AllowAnonymous]
  [HttpGet("{id:guid}")]
  public async Task<ActionResult<ServiceResponse>> GetById(
    Guid id)
  {
    var result = await _serviceManagementService
      .GetByIdAsync(id);

    if (result is null)
    {
      return NotFound(new
      {
        message = "Service not found."
      });
    }

    return Ok(result);
  }
}