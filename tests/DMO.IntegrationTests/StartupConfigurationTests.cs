using DMO.Infrastructure.Database;
using DMO.IntegrationTests.Host;
using Microsoft.AspNetCore.Hosting;

namespace DMO.IntegrationTests;

/// <summary>
/// Proposed P1-T01 test — the host fails startup when database configuration is absent.
/// </summary>
/// <remarks>
/// PROPOSED — NOT EXECUTED. Awaiting Architect review before first execution.
/// See <c>docs/PROPOSED_TESTS_P1-T01.md</c> for the full test protocol record.
/// </remarks>
public sealed class StartupConfigurationTests
{
    [Fact]
    public void Host_WhenConnectionStringAbsent_FailsStartup()
    {
        // Preconditions: no Database:ConnectionString is supplied.
        using var factory = new DmoWebApplicationFactory(supplyConnectionString: false);

        // Action + assertion: the host refuses to start rather than defaulting.
        var exception = Assert.Throws<DatabaseConfigurationException>(() => factory.CreateClient());

        // Required non-effect: the failure names the missing key rather than silently proceeding.
        Assert.Contains(DatabaseOptions.ConnectionStringKey, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_WhenConnectionStringSupplied_StartsSuccessfully()
    {
        // Required non-effect for the previous test: a supplied value does start the host.
        using var factory = new DmoWebApplicationFactory(supplyConnectionString: true);

        using var client = factory.CreateClient();

        Assert.NotNull(client);
    }
}
