using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Api.DTOs.Bookings;
using ServiceBooking.Api.Enums;
using ServiceBooking.Api.Services;

namespace ServiceBooking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController(
    BookingService bookingService) : ControllerBase
{
  private readonly BookingService _bookingService = bookingService;

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
  [HttpGet("my-bookings")]
  public async Task<ActionResult<List<BookingResponse>>> GetMyBookings(
    [FromQuery] BookingStatus? status,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10
  )
  {
    var customerId = GetCurrentUserId();

    var (items, totalCount) =
      await _bookingService.GetMyBookingsAsync(
        customerId,
        status,
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
  public async Task<ActionResult> GetAll(
      [FromQuery] DateOnly? date,
      [FromQuery] BookingStatus? status,
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 10)
  {
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var (items, totalCount) =
      await _bookingService.GetAllAsync(
        date,
        status,
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