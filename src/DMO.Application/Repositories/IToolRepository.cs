using DMO.Domain.Tools;

namespace DMO.Application.Repositories;

/// <summary>
/// The single canonical Tool repository contract.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §12.2 (exact interface).
/// <para>
/// <c>tools</c> is the only Tool registry in the system. No second Tool table, module-private Tool
/// store, Tool cache or "provisional Tool" entity exists, and the Job On area owns no Tool master
/// fact.
/// </para>
/// <para>
/// Every member takes a <see cref="CancellationToken"/> as its mandatory last parameter, single
/// reads return <c>Task&lt;T?&gt;</c>, lists return <c>Task&lt;IReadOnlyList&lt;T&gt;&gt;</c>, and
/// every write that touches more than one row opens its own transaction. There is no ambient
/// unit-of-work.
/// </para>
/// </remarks>
public interface IToolRepository
{
    /// <summary>Reads one canonical Tool by identity, or <c>null</c> when it does not exist.</summary>
    Task<Tool?> GetByIdAsync(Guid toolId, CancellationToken cancellationToken);

    /// <summary>Reads the bounded, deterministic canonical Tool search result.</summary>
    Task<IReadOnlyList<Tool>> SearchAsync(ToolSearchCriteria criteria, CancellationToken cancellationToken);

    /// <summary>Finds the canonical Tool owning the exact identity tuple, or <c>null</c>.</summary>
    Task<Tool?> FindByIdentityAsync(
        ToolType type,
        string reference,
        string lot,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists the Tool and its full machine-compatibility set in one transaction, or persists
    /// nothing.
    /// </summary>
    /// <exception cref="DMO.Application.Persistence.ToolPersistenceException">
    /// A canonical Tool with the same identity tuple exists (unique-index race).
    /// </exception>
    Task<Tool> CreatedAsync(
        Tool tool,
        IReadOnlyList<MachineCode> compatibleMachines,
        CancellationToken cancellationToken);

    /// <summary>Reads the Job On occurrences using this Tool (a query, never a stored array).</summary>
    Task<IReadOnlyList<ToolUsageOccurrence>> ListUsageOccurrencesAsync(
        Guid toolId,
        CancellationToken cancellationToken);
}

/// <summary>
/// The repository-level Tool search criteria: one query shape, projected by the service from the
/// validated <c>ToolSearchQuery</c> so no second validation layer exists.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §12.2 rule 0 and §8.2.
/// </remarks>
public sealed record ToolSearchCriteria(
    string? Query,
    ToolType? Type,
    string? Reference,
    string? Lot,
    MachineCode? Machine,
    int Limit);

/// <summary>One Job On occurrence using a canonical Tool (the §8.3 reverse read item).</summary>
public sealed record ToolUsageOccurrence(
    Guid JobOnId,
    string Reference,
    string ProductionNumber,
    string Machine);
