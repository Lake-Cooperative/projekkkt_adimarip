using Adimarip.Infrastructure.Persistence;
using Xunit;

namespace Adimarip.Tests.Infrastructure;

public sealed class PostgresConnectionTests
{
    [Fact]
    public void Database_url_is_converted_to_npgsql_format()
    {
        var connection = PostgresConnection.FromUrl("postgres://app:p%40ss%3Bword@db.example.org:6543/registry");

        Assert.Contains("Host=db.example.org", connection);
        Assert.Contains("Port=6543", connection);
        Assert.Contains("Database=registry", connection);
        Assert.Contains("Username=app", connection);
        Assert.Contains("Password=\"p@ss;word\"", connection);
    }

    [Fact]
    public void Default_port_is_5432() =>
        Assert.Contains("Port=5432", PostgresConnection.FromUrl("postgresql://app:secret@db/registry"));

    [Fact]
    public void Non_postgres_url_is_rejected() =>
        Assert.Throws<InvalidOperationException>(() => PostgresConnection.FromUrl("mysql://app:secret@db/registry"));
}
