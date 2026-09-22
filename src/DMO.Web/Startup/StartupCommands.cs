using DMO.Application.Migrations;

namespace DMO.Web.Startup;

/// <summary>
/// Technical startup entry points selectable from the command line.
/// </summary>
/// <remarks>
/// These are runtime mechanics only. They carry no product behaviour and no Phase 1
/// business meaning.
/// </remarks>
public static class StartupCommands
{
    /// <summary>Command token that selects the migration entry point.</summary>
    public const string MigrateCommand = "migrate";

    /// <summary>Command token that selects the single-ADMIN bootstrap entry point.</summary>
    public const string BootstrapAdminCommand = "bootstrap-admin";

    /// <summary>Exit code returned when the migration entry point succeeds.</summary>
    public const int SuccessExitCode = 0;

    /// <summary>Exit code returned when the migration entry point fails.</summary>
    public const int FailureExitCode = 1;

    /// <summary>
    /// Determines whether the supplied arguments select the first meaningful entry point.
    /// </summary>
    /// <param name="args">Process arguments.</param>
    /// <param name="command">Command token to match.</param>
    /// <returns><c>true</c> when the first meaningful argument is <c>command</c> or <c>--command</c>.</returns>
    /// <remarks>
    /// Matched on the first argument only, so an ASP.NET Core switch such as
    /// <c>--environment Production</c> is never mistaken for a command.
    /// </remarks>
    private static bool IsCommand(string[]? args, string command)
    {
        if (args is null || args.Length == 0)
        {
            return false;
        }

        var first = args[0];

        return string.Equals(first, command, StringComparison.OrdinalIgnoreCase)
            || string.Equals(first, $"--{command}", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the supplied arguments select the migration entry point.
    /// </summary>
    /// <param name="args">Process arguments.</param>
    /// <returns><c>true</c> when the first meaningful argument is <c>migrate</c> or <c>--migrate</c>.</returns>
    public static bool IsMigrationCommand(string[]? args) => IsCommand(args, MigrateCommand);

    /// <summary>
    /// Determines whether the supplied arguments select the single-ADMIN bootstrap entry point.
    /// </summary>
    /// <param name="args">Process arguments.</param>
    /// <returns><c>true</c> when the first meaningful argument is <c>bootstrap-admin</c> or <c>--bootstrap-admin</c>.</returns>
    public static bool IsBootstrapAdminCommand(string[]? args) => IsCommand(args, BootstrapAdminCommand);

    /// <summary>
    /// Applies pending migrations and returns a process exit code.
    /// </summary>
    /// <param name="services">The application service provider.</param>
    /// <param name="logger">Logger used to report the technical outcome.</param>
    /// <returns><see cref="SuccessExitCode"/> on success, <see cref="FailureExitCode"/> otherwise.</returns>
    /// <remarks>
    /// A configuration failure is reported and returned as a non-zero exit code. The host
    /// never starts against a missing or invalid database configuration.
    /// </remarks>
    public static async Task<int> RunMigrateAsync(IServiceProvider services, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        await using var scope = services.CreateAsyncScope();

        try
        {
            // Resolution itself can fail: database configuration is validated eagerly and a
            // DatabaseConfigurationException surfaces here. It is reported like any other
            // migration failure rather than escaping as an unhandled exception.
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

            var pendingBefore = await runner.ListPendingAsync();
            logger.LogInformation(
                "Migration runner started. Pending migrations: {PendingCount}.", pendingBefore.Count);

            var result = await runner.ApplyPendingAsync();

            if (result.AppliedCount == 0)
            {
                logger.LogInformation(
                    "Migration runner completed. No pending migrations were applied; the database schema was not changed.");
            }
            else
            {
                logger.LogInformation(
                    "Migration runner completed. Applied {AppliedCount} migration(s): {Applied}.",
                    result.AppliedCount,
                    string.Join(", ", result.AppliedMigrations));
            }

            return SuccessExitCode;
        }
        catch (Exception ex)
        {
            // Includes DatabaseConfigurationException: fail loudly, never start on a default.
            logger.LogError(ex, "Migration runner failed: {Message}", ex.Message);
            return FailureExitCode;
        }
    }

    /// <summary>
    /// Runs the single-ADMIN bootstrap entry point and returns a process exit code.
    /// </summary>
    /// <param name="services">The application service provider.</param>
    /// <param name="logger">Logger used to report the technical outcome.</param>
    /// <returns><see cref="SuccessExitCode"/> on success, <see cref="FailureExitCode"/> otherwise.</returns>
    /// <remarks>
    /// The bootstrap is deployment-only (DEV/TEST) and idempotent; it performs no provider
    /// call and holds no service credentials. Any failure (incomplete configuration or an
    /// existing conflicting mapping) returns the failure code without modifying the row.
    /// </remarks>
    public static async Task<int> RunBootstrapAdminAsync(IServiceProvider services, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        await using var scope = services.CreateAsyncScope();

        try
        {
            var command = scope.ServiceProvider.GetRequiredService<AdminBootstrapCommand>();
            return await command.ExecuteAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ADMIN bootstrap failed: {Message}", ex.Message);
            return FailureExitCode;
        }
    }

    /// <summary>
    /// Runs the web host to completion.
    /// </summary>
    /// <param name="app">The configured application.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> RunHostAsync(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        await app.RunAsync();
        return SuccessExitCode;
    }
}
