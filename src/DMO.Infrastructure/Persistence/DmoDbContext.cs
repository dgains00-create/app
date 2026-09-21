using Microsoft.EntityFrameworkCore;

namespace DMO.Infrastructure.Persistence;

/// <summary>
/// The single application persistence context.
/// </summary>
/// <remarks>
/// <para>
/// P1-T01 deliberately declares <b>no</b> entity set. The task forbids creating Phase 1
/// product tables (users, admin_accounts, templates, template_modules, permissions,
/// capabilities, roles, audit, settings or any industrial schema), so this context models
/// nothing yet.
/// </para>
/// <para>
/// There is exactly one database context by design. The accepted plan forbids multiple
/// database contexts; phases share the same backend.
/// </para>
/// <para>
/// Persisted entity types are added by the task that genuinely owns them (starting with
/// P1-T03 for the account and Template foundation).
/// </para>
/// </remarks>
public sealed class DmoDbContext : DbContext
{
    /// <summary>Creates the context.</summary>
    public DmoDbContext(DbContextOptions<DmoDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // No Phase 1 entity configuration is applied in P1-T01.
        // Entity type configurations are introduced by the tasks that own the entities.
        base.OnModelCreating(modelBuilder);
    }
}
