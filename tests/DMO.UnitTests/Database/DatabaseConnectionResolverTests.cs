using DMO.Infrastructure.Database;

namespace DMO.UnitTests.Database;

/// <summary>
/// Proposed P1-T01 test — database configuration fails loudly when absent.
/// </summary>
/// <remarks>
/// PROPOSED — NOT EXECUTED. Awaiting Architect review before first execution.
/// See <c>docs/PROPOSED_TESTS_P1-T01.md</c> in the repository root for the full test protocol record.
/// </remarks>
public sealed class DatabaseConnectionResolverTests
{
    [Fact]
    public void GetConnectionString_WhenUnset_Throws()
    {
        // Preconditions: a resolver over options with no connection string configured.
        var resolver = new DatabaseConnectionResolver(new DatabaseOptions { ConnectionString = null });

        // Action + assertions: the resolver refuses rather than returning a default.
        var exception = Assert.Throws<DatabaseConfigurationException>(() => resolver.GetConnectionString());

        // Required non-effect: no usable connection string is produced.
        Assert.Contains(DatabaseOptions.ConnectionStringKey, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetConnectionString_WhenBlank_Throws(string configured)
    {
        var resolver = new DatabaseConnectionResolver(new DatabaseOptions { ConnectionString = configured });

        Assert.Throws<DatabaseConfigurationException>(() => resolver.GetConnectionString());
    }

    [Fact]
    public void GetConnectionString_WhenNotParseable_Throws()
    {
        // "not a connection string" parses as a key with no '=' separator -> Npgsql rejects it.
        var resolver = new DatabaseConnectionResolver(
            new DatabaseOptions { ConnectionString = "this is not a connection string" });

        Assert.Throws<DatabaseConfigurationException>(() => resolver.GetConnectionString());
    }

    [Fact]
    public void GetConnectionString_WhenHostMissing_Throws()
    {
        var resolver = new DatabaseConnectionResolver(
            new DatabaseOptions { ConnectionString = "Database=dmo" });

        var exception = Assert.Throws<DatabaseConfigurationException>(() => resolver.GetConnectionString());

        Assert.Contains("Host", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetConnectionString_WhenDatabaseMissing_Throws()
    {
        var resolver = new DatabaseConnectionResolver(
            new DatabaseOptions { ConnectionString = "Host=localhost" });

        var exception = Assert.Throws<DatabaseConfigurationException>(() => resolver.GetConnectionString());

        Assert.Contains("Database", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetConnectionString_WhenValid_ReturnsValueWithoutInventingAnEnvironment()
    {
        const string configured = "Host=localhost;Port=5432;Database=dmo_test;Username=u;Password=p";
        var resolver = new DatabaseConnectionResolver(new DatabaseOptions { ConnectionString = configured });

        var result = resolver.GetConnectionString();

        // The resolver normalises through Npgsql but must not substitute a different target.
        var parsed = new Npgsql.NpgsqlConnectionStringBuilder(result);
        Assert.Equal("localhost", parsed.Host);
        Assert.Equal("dmo_test", parsed.Database);
    }
}
