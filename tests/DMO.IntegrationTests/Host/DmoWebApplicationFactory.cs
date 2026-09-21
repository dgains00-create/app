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
/// <b>Three credential-free placeholders are injected through process-scoped environment
/// configuration before the real entry point runs</b>: the database connection string, the
/// Supabase project URL and the Supabase publishable key. Since P1-T02 the application entry
/// point validates Supabase configuration eagerly during composition (like the database
/// configuration), so the test host supplies placeholders for both. The Supabase placeholder
/// URL is deliberately local/non-existent and is never contacted: no P1-T02 test makes a
/// real Supabase call (the live test is separately environment-gated and skipped by
/// default).
/// </para>
/// <para>
/// <b>Why an environment variable rather than <c>ConfigureAppConfiguration</c>.</b>
/// <c>DMO.Web</c>'s entry point validates configuration eagerly, during composition, inside
/// <c>AddDmoInfrastructure(builder.Configuration)</c> and the Supabase options validation.
/// The <c>WebApplicationFactory</c> configuration hook runs <i>after</i> the entry point has
/// already executed, so a value supplied through that hook arrives too late and the host
/// exits before an <c>IHost</c> is built. Setting the process-scoped environment variable
/// before the host is created means the real entry point's configuration builder sees it in
/// time.
/// </para>
/// <para>
/// User- and machine-scoped values are never read or modified; any pre-existing
/// process-scoped values are preserved and restored when the factory is disposed.
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

    /// <summary>
    /// Placeholder Supabase project URL used to satisfy eager Supabase configuration
    /// validation. Deliberately local, credential-free and never contacted.
    /// </summary>
    public const string PlaceholderSupabaseProjectUrl = "https://dmo-test-placeholder.invalid/";

    /// <summary>
    /// Placeholder Supabase publishable key used to satisfy eager Supabase configuration
    /// validation. Deliberately not a real key.
    /// </summary>
    public const string PlaceholderSupabasePublishableKey = "sb_publishable_placeholder_test_key";

    /// <summary>
    /// Environment variable that carries the Supabase project URL, as the double-underscore
    /// form of <c>Supabase:ProjectUrl</c>.
    /// </summary>
    public const string SupabaseProjectUrlEnvironmentVariable = "Supabase__ProjectUrl";

    /// <summary>
    /// Environment variable that carries the Supabase publishable key, as the
    /// double-underscore form of <c>Supabase:PublishableKey</c>.
    /// </summary>
    public const string SupabasePublishableKeyEnvironmentVariable = "Supabase__PublishableKey";

    private readonly string? _previousConnectionString;
    private readonly bool _hadPreviousConnectionString;
    private readonly string? _previousSupabaseProjectUrl;
    private readonly bool _hadPreviousSupabaseProjectUrl;
    private readonly string? _previousSupabasePublishableKey;
    private readonly bool _hadPreviousSupabasePublishableKey;
    private bool _disposed;

    /// <summary>
    /// Creates the test host and makes the placeholder configuration visible to the real
    /// application entry point.
    /// </summary>
    public DmoWebApplicationFactory()
    {
        // Capture any pre-existing process-scoped values so they can be restored on dispose.
        _previousConnectionString = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable, EnvironmentVariableTarget.Process);
        _hadPreviousConnectionString = _previousConnectionString is not null;

        _previousSupabaseProjectUrl = Environment.GetEnvironmentVariable(
            SupabaseProjectUrlEnvironmentVariable, EnvironmentVariableTarget.Process);
        _hadPreviousSupabaseProjectUrl = _previousSupabaseProjectUrl is not null;

        _previousSupabasePublishableKey = Environment.GetEnvironmentVariable(
            SupabasePublishableKeyEnvironmentVariable, EnvironmentVariableTarget.Process);
        _hadPreviousSupabasePublishableKey = _previousSupabasePublishableKey is not null;

        // Process scope only: User and Machine scopes are never touched.
        Environment.SetEnvironmentVariable(
            ConnectionStringEnvironmentVariable,
            PlaceholderConnectionString,
            EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(
            SupabaseProjectUrlEnvironmentVariable,
            PlaceholderSupabaseProjectUrl,
            EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(
            SupabasePublishableKeyEnvironmentVariable,
            PlaceholderSupabasePublishableKey,
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

            // Restore the previous process-scoped values, including restoring their absence.
            Environment.SetEnvironmentVariable(
                ConnectionStringEnvironmentVariable,
                _hadPreviousConnectionString ? _previousConnectionString : null,
                EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(
                SupabaseProjectUrlEnvironmentVariable,
                _hadPreviousSupabaseProjectUrl ? _previousSupabaseProjectUrl : null,
                EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(
                SupabasePublishableKeyEnvironmentVariable,
                _hadPreviousSupabasePublishableKey ? _previousSupabasePublishableKey : null,
                EnvironmentVariableTarget.Process);
        }

        base.Dispose(disposing);
    }
}