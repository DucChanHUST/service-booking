using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Respawn.Graph;
using ServiceBooking.Api.Data;

namespace ServiceBooking.Api.Tests.Infrastructure;

public class DatabaseFixture : IAsyncLifetime
{
  private const string TestConnectionString =
    "Host=localhost;" +
    "Port=5433;" +
    "Database=service_booking_test;" +
    "Username=postgres;" +
    "Password=postgres";

  private Respawner _respawner = null!;

  public string ConnectionString => TestConnectionString;

  public async Task InitializeAsync()
  {
    await using (var db = CreateDbContext())
    {
      await db.Database.MigrateAsync();
    }

    await using var connection =
      new NpgsqlConnection(TestConnectionString);

    await connection.OpenAsync();

    _respawner = await Respawner.CreateAsync(
      connection,
      new RespawnerOptions
      {
        DbAdapter = DbAdapter.Postgres,

        SchemasToInclude = ["public"],

        TablesToIgnore = [new Table("__EFMigrationsHistory")]
      });
  }

  public async Task ResetAsync()
  {
    await using var connection =
      new NpgsqlConnection(TestConnectionString);

    await connection.OpenAsync();

    await _respawner.ResetAsync(connection);
  }

  public AppDbContext CreateDbContext()
  {
    var options =
      new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(TestConnectionString)
        .Options;

    return new AppDbContext(options);
  }

  public Task DisposeAsync()
  {
    return Task.CompletedTask;
  }
}