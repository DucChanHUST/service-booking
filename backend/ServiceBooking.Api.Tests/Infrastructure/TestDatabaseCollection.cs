using Xunit;

namespace ServiceBooking.Api.Tests.Infrastructure;

[CollectionDefinition("Database collection")]
public class TestDatabaseCollection
  : ICollectionFixture<DatabaseFixture>
{
}