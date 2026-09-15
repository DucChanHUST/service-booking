using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Api.DTOs.Bookings;
using ServiceBooking.Api.Enums;
using ServiceBooking.Api.Services;

namespace ServiceBooking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
  private readonly BookingService _bookingService;

  public BookingsController(
      BookingService bookingService)
  {
    _bookingService = bookingService;
  }

  [AllowAnonymous]
  [HttpGet("available-slots")]
  public async Task<ActionResult<AvailableSlotsResponse>>
    GetAvailableSlots(
      [FromQuery] Guid serviceId,
      [FromQuery] Guid staffId,
      [FromQuery] DateOnly date)
  {
    try
    {
      var result =
        await _bookingService.GetAvailableSlotsAsync(
          serviceId,
          staffId,
          date);

      return Ok(result);
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

  [Authorize(Roles = "Customer")]
  [HttpPost]
  public async Task<ActionResult<BookingResponse>> Create(
      CreateBookingRequest request)
  {
    try
    {
      var customerId = GetCurrentUserId();

      var result =
        await _bookingService.CreateAsync(
          customerId,
          request);

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

  [Authorize(Roles = "Customer")]
  [HttpGet("my")]
  public async Task<ActionResult<List<BookingResponse>>>
      GetMyBookings()
  {
    var customerId = GetCurrentUserId();

    var result =
      await _bookingService.GetMyBookingsAsync(
        customerId);

    return Ok(result);
  }


  [Authorize(Roles = "Customer")]
  [HttpPatch("{id:guid}/cancel")]
  public async Task<ActionResult<BookingResponse>> Cancel(
      Guid id,
      CancelBookingRequest request)
  {
    try
    {
      var customerId = GetCurrentUserId();

      var result =
        await _bookingService.CancelAsync(
          customerId,
          id,
          request.CancellationReason);

      if (result is null)
      {
        return NotFound(new
        {
          message = "Booking not found."
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

  // ADMIN: GET ALL

  [Authorize(Roles = "Admin")]
  [HttpGet]
  public async Task<ActionResult<List<BookingResponse>>>
      GetAll()
  {
    var result =
        await _bookingService.GetAllAsync();

    return Ok(result);
  }


  [Authorize(Roles = "Admin")]
  [HttpPatch("{id:guid}/status")]
  public async Task<ActionResult<BookingResponse>>
    UpdateStatus(
      Guid id,
      UpdateBookingStatusRequest request)
  {
    try
    {
      var result =
          await _bookingService.UpdateStatusAsync(
              id,
              request.Status);

      if (result is null)
      {
        return NotFound(new
        {
          message = "Booking not found."
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

  private Guid GetCurrentUserId()
  {
    var userId =
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (!Guid.TryParse(userId, out var id))
    {
      throw new UnauthorizedAccessException("Invalid user identity.");
    }

    return id;
  }
}