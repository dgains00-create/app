using DMO.Infrastructure;
using DMO.Infrastructure.Database;
using DMO.IntegrationTests.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DMO.IntegrationTests;

/// <summary>
/// Proposed P1-T01 test — host composition requires database configuration.
/// </summary>
/// <remarks>
/// <para>
/// PROPOSED — NOT EXECUTED. Awaiting Architect review before first execution.
/// See <c>docs/PROPOSED_TESTS_P1-T01.md</c> for the full test protocol record.
/// </para>
/// <para>
/// These tests assert the stable composition boundary
/// (<c>AddDmoInfrastructure</c> with missing/invalid configuration), not the exception type
/// that an in-process <c>WebApplicationFactory</c> happens to surface through its hosting
/// mechanics. <c>Program.cs</c> converts the failure into a clean process exit code, so the
/// original exception type is not the acceptance contract.
/// </para>
/// </remarks>
public sealed class StartupConfigurationTests
{
    private static IConfiguration ConfigurationWith(params (string Key, string? Value)[] settings)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(
                setting => setting.Key,
                setting => setting.Value))
            .Build();
    }

    [Fact]
    public void AddDmoInfrastructure_WhenConnectionStringAbsent_Throws()
    {
        // Preconditions: no Database:ConnectionString is supplied.
        var services = new ServiceCollection();
        var configuration = ConfigurationWith();

        // Action + assertion: composition refuses rather than defaulting.
        var exception = Assert.Throws<DatabaseConfigurationException>(
            () => services.AddDmoInfrastructure(configuration));

        // Required non-effect: the failure names the missing key rather than silently proceeding.
        Assert.Contains(DatabaseOptions.ConnectionStringKey, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a connection string")]
    [InlineData("Database=dmo")]
    public void AddDmoInfrastructure_WhenConnectionStringInvalid_Throws(string configured)
    {
        var services = new ServiceCollection();
        var configuration = ConfigurationWith(
            (DatabaseOptions.ConnectionStringKey, configured));

        // Required non-effect: no implicit or default database target is invented.
        Assert.Throws<DatabaseConfigurationException>(
            () => services.AddDmoInfrastructure(configuration));
    }

    [Fact]
    public void AddDmoInfrastructure_WhenConnectionStringSupplied_Succeeds()
    {
        // Required non-effect partner: valid syntactic configuration does not itself fail.
        var services = new ServiceCollection();
        var configuration = ConfigurationWith(
            (DatabaseOptions.ConnectionStringKey, DmoWebApplicationFactory.PlaceholderConnectionString));

        services.AddDmoInfrastructure(configuration);

        // Required non-effect: registering infrastructure does not open a database connection.
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(DatabaseConnectionResolver));
    }

    [Fact]
    public void Host_WhenConnectionStringSupplied_StartsSuccessfully()
    {
        // The host starts in-process when valid configuration is present, and starting it
        // does not force an immediate database connection.
        using var factory = new DmoWebApplicationFactory(supplyConnectionString: true);

        using var client = factory.CreateClient();

        Assert.NotNull(client);
    }
}
