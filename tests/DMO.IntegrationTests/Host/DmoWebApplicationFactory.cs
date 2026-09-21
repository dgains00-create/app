using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace DMO.IntegrationTests.Host;

/// <summary>
/// Test host for the DMO web application.
/// </summary>
/// <remarks>
/// <para>
/// The application is a top-level-statement <c>Program</c> in <c>DMO.Web</c>. The generated
/// entry point class is <c>Program</c>; this factory targets it explicitly.
/// </para>
/// <para>
/// A connection string is injected by default because the host validates database
/// configuration eagerly and would otherwise fail startup. It points at a local,
/// non-existent PostgreSQL instance and is never connected to by the proposed tests —
/// the technical endpoint deliberately performs no database access.
/// </para>
/// <para>
/// PROPOSED — NOT EXECUTED. Awaiting Architect review before first execution.
/// </para>
/// </remarks>
public sealed class DmoWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Placeholder connection string used to satisfy eager configuration validation.
    /// Deliberately local and credential-free; no test connects to it.
    /// </summary>
    public const string PlaceholderConnectionString =
        "Host=localhost;Port=5432;Database=dmo_placeholder;Username=placeholder;Password=placeholder";

    private readonly bool _supplyConnectionString;

    /// <summary>Creates the factory.</summary>
    /// <param name="supplyConnectionString">
    /// When <c>true</c> (default) a placeholder connection string is provided so the host
    /// starts. When <c>false</c> none is provided, to observe the fail-fast behaviour.
    /// </param>
    public DmoWebApplicationFactory(bool supplyConnectionString = true)
    {
        _supplyConnectionString = supplyConnectionString;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(Environments.Development);

        if (_supplyConnectionString)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:ConnectionString"] = PlaceholderConnectionString,
                });
            });
        }
    }
}
