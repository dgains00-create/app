namespace DMO.Application.Migrations;

/// <summary>
/// Outcome of a migration run.
/// </summary>
/// <param name="AppliedCount">Number of migrations applied by this run.</param>
/// <param name="PendingCount">Number of migrations that remain pending after this run.</param>
/// <param name="AppliedMigrations">
/// Identifiers of the migrations applied by this run, in application order.
/// Empty when the database was already up to date.
/// </param>
/// <remarks>
/// This is a purely technical result. It carries no product meaning and exposes no
/// Phase 1 domain concept.
/// </remarks>
public sealed record MigrationResult(int AppliedCount, int PendingCount, IReadOnlyList<string> AppliedMigrations)
{
    /// <summary>A run that found nothing to apply.</summary>
    public static MigrationResult None { get; } = new(0, 0, []);
}

/// <summary>
/// Execution boundary for database migrations.
/// </summary>
/// <remarks>
/// The runtime host depends on this contract only, so the migration mechanism itself
/// (currently EF Core / PostgreSQL, see <c>DMO.Infrastructure</c>) can be replaced
/// without the host or the application layer knowing.
/// </remarks>
public interface IMigrationRunner
{
    /// <summary>
    /// Applies every pending migration to the target database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The technical result of the run.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the database configuration is absent or invalid. The runner must fail
    /// loudly rather than fall back to an implicit or production-looking default.
    /// </exception>
    Task<MigrationResult> ApplyPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports the migrations that are pending against the target database, without applying them.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Identifiers of the pending migrations, in application order.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the database configuration is absent or invalid.
    /// </exception>
    Task<IReadOnlyList<string>> ListPendingAsync(CancellationToken cancellationToken = default);
}
