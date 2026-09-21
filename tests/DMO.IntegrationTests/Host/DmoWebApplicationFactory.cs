using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace DMO.IntegrationTests.Host;

/// <summary>
/// Test host for the DMO web application.
/// </summary>
/// <remarks>
/// <para>
/// The application is a top-level-statement <c>Program</c> in <c>DMO.Web</c>; the generated
/// entry point class is <c>Program</c>, and this factory targets it explicitly.
/// </para>
/// <para>
/// <b>Parameterless by design.</b> xUnit constructs a class fixture itself and cannot supply
/// a constructor argument, so the factory exposes no constructor parameters. It previously
/// took a <c>bool supplyConnectionString</c> parameter, which xUnit reported as an unresolved
/// fixture dependency; the "missing database configuration" case is instead covered directly
/// at the composition boundary by <c>StartupConfigurationTests</c>.
/// </para>
/// <para>
/// <b>Why an environment variable rather than <c>ConfigureAppConfiguration</c>.</b>
/// <c>DMO.Web</c>'s entry point validates database configuration eagerly, during
/// composition, inside <c>AddDmoInfrastructure(builder.Configuration)</c>. The
/// <c>WebApplicationFactory</c> configuration hook runs <i>after</i> the entry point has
/// already executed, so a value supplied through that hook arrives too late and the host
/// exits before an <c>IHost</c> is built. Setting the process-scoped environment variable
/// before the host is created means the real entry point's configuration builder sees it in
/// time.
/// </para>
/// <para>
/// The injected value is a local, credential-free placeholder pointing at a non-existent
/// PostgreSQL instance. No test connects to it; the technical endpoint performs no database
/// access, and merely starting the host opens no connection.
/// </para>
/// <para>
/// The environment variable is process-scoped only. User- and machine-scoped values are never
/// read or modified, and any pre-existing process-scoped value is preserved and restored when
/// the factory is disposed.
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

    /// <summary>
    /// Environment variable that carries the connection string to the host's configuration
    /// builder, as the double-underscore form of <c>Database:ConnectionString</c>.
    /// </summary>
    public const string ConnectionStringEnvironmentVariable = "Database__ConnectionString";

    private readonly string? _previousConnectionString;
    private readonly bool _hadPreviousConnectionString;
    private bool _disposed;

    /// <summary>
    /// Creates the test host and makes the placeholder connection string visible to the real
    /// application entry point.
    /// </summary>
    public DmoWebApplicationFactory()
    {
        // Capture any pre-existing process-scoped value so it can be restored on dispose.
        _previousConnectionString = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable, EnvironmentVariableTarget.Process);
        _hadPreviousConnectionString = _previousConnectionString is not null;

        // Process scope only: User and Machine scopes are never touched.
        Environment.SetEnvironmentVariable(
            ConnectionStringEnvironmentVariable,
            PlaceholderConnectionString,
            EnvironmentVariableTarget.Process);
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(Environments.Development);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;

            // Restore the previous process-scoped value, including restoring its absence.
            Environment.SetEnvironmentVariable(
                ConnectionStringEnvironmentVariable,
                _hadPreviousConnectionString ? _previousConnectionString : null,
                EnvironmentVariableTarget.Process);
        }

        base.Dispose(disposing);
    }
}
