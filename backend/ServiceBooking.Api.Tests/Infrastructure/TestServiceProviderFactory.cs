using Microsoft.Extensions.DependencyInjection;
using ServiceBooking.Api.Data;
using ServiceBooking.Api.Services;

namespace ServiceBooking.Api.Tests.Infrastructure;

public static class TestServiceProviderFactory
{
  public static ServiceProvider Create(
      AppDbContext dbContext)
  {
    var services = new ServiceCollection();

    services.AddSingleton(dbContext);
    services.AddLogging();
    services.AddSignalR();
    services.AddScoped<BookingService>();

    return services.BuildServiceProvider();
  }
}