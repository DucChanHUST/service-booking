using Microsoft.EntityFrameworkCore;
using ServiceBooking.Api.Data;

namespace ServiceBooking.Api.Tests;

public static class TestDbContextFactory
{
  public static AppDbContext Create()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    return new AppDbContext(options);
  }
}