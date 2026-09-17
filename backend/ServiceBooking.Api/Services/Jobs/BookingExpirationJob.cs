using Microsoft.EntityFrameworkCore;
using ServiceBooking.Api.Data;
using ServiceBooking.Api.Enums;

namespace ServiceBooking.Api.Services.Jobs;

public class BookingExpirationJob(
    AppDbContext dbContext,
    BookingService bookingService)
{
  public async Task ExecuteAsync()
  {
    var now = DateTime.UtcNow;

    var bookingIds = await dbContext.Bookings
      .AsNoTracking()
      .Where(x =>
        (x.Status == BookingStatus.Pending ||
          x.Status == BookingStatus.Confirmed) &&
        x.EndTime <= now)
      .Select(x => x.Id)
      .ToListAsync();

    foreach (var bookingId in bookingIds)
    {
      await bookingService.CompleteExpiredBookingAsync(bookingId);
    }
  }
}