using DMO.Application.JobOn;
using DMO.Domain.Tools;
using DomainJobOn = DMO.Domain.JobOn.JobOn;

namespace DMO.Application.Repositories;

/// <summary>
/// The single Job On repository contract.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §12.2 (exact interface) and §10/§11 (transaction boundaries).
/// <para>
/// Every write that touches more than one row (create with contexts, edit with context changes,
/// duplication, delete with contexts) opens its own transaction and maps PostgreSQL constraint
/// violations onto typed application failures. Contextual Tool resolution (existence + type match)
/// happens <b>inside</b> the transaction so a race cannot bypass it.
/// </para>
/// </remarks>
public interface IJobOnRepository
{
    /// <summary>Reads one occurrence with its contexts, or <c>null</c>.</summary>
    Task<DomainJobOn?> GetByIdAsync(Guid jobOnId, CancellationToken cancellationToken);

    /// <summary>Finds the occurrence owning the production uniqueness pair, or <c>null</c>.</summary>
    Task<DomainJobOn?> FindByProductionAsync(
        string reference,
        string productionNumber,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads every matching occurrence for the reference, in the contracted deterministic technical
    /// order. Historical occurrences are included; nothing is excluded for age.
    /// </summary>
    Task<IReadOnlyList<JobOnProductionListItem>> ListByReferenceAsync(
        string reference,
        CancellationToken cancellationToken);

    /// <summary>
    /// Inserts the occurrence and only the supplied contexts in one transaction (0..3 context rows),
    /// or inserts nothing.
    /// </summary>
    /// <exception cref="DMO.Application.Persistence.JobOnPersistenceException">
    /// The production uniqueness pair is already owned, a supplied Tool does not exist, or a supplied
    /// Tool's type does not match its slot.
    /// </exception>
    Task<DomainJobOn> CreatedAsync(
        DomainJobOn jobOn,
        IReadOnlyList<ToolContext> contexts,
        CancellationToken cancellationToken);

    /// <summary>
    /// Applies the fact changes and every explicit Tool-association change in one transaction.
    /// </summary>
    /// <exception cref="DMO.Application.Persistence.ConcurrencyConflictException">
    /// The persisted version differs from the version the caller observed.
    /// </exception>
    /// <exception cref="DMO.Application.Persistence.JobOnPersistenceException">
    /// The production uniqueness pair is already owned by another occurrence, a supplied Tool does
    /// not exist, a supplied Tool's type does not match its slot, or a removed context is referenced
    /// by a dependent fact.
    /// </exception>
    Task<DomainJobOn> UpdatedAsync(
        DomainJobOn jobOn,
        IReadOnlyList<ToolContextChange> changes,
        CancellationToken cancellationToken);

    /// <summary>
    /// Inserts the duplicated occurrence with new context identities in one transaction. The source
    /// row is only read; the copied frozen triples come from the supplied duplicated contexts.
    /// </summary>
    /// <exception cref="DMO.Application.Persistence.ConcurrencyConflictException">
    /// The source version differs from the version the caller previewed.
    /// </exception>
    /// <exception cref="DMO.Application.Persistence.JobOnPersistenceException">
    /// The production uniqueness pair is already owned.
    /// </exception>
    Task<DomainJobOn> DuplicatedAsync(
        DomainJobOn duplicate,
        IReadOnlyList<ToolContext> duplicatedContexts,
        int expectedSourceVersion,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes this occurrence's context rows and then the occurrence itself in one transaction.
    /// </summary>
    /// <exception cref="DMO.Application.Persistence.ConcurrencyConflictException">
    /// The persisted version differs from the version the caller observed.
    /// </exception>
    /// <exception cref="DMO.Application.Persistence.JobOnPersistenceException">
    /// A dependent fact still references the occurrence or one of its contexts (fail closed).
    /// </exception>
    Task DeletedAsync(Guid jobOnId, int expectedVersion, CancellationToken cancellationToken);

    /// <summary>Reads the occurrences whose recorded duplication lineage points at this occurrence.</summary>
    Task<IReadOnlyList<JobOnDependency>> ListLineageDependentsAsync(
        Guid jobOnId,
        CancellationToken cancellationToken);
}

/// <summary>
/// One explicit Tool-association change applied inside a Job On edit transaction.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §11.2 step 6 and §7.4. The change list — never a nullable Tool id — is
/// the association mechanism, so "leave as is" and "remove" are never conflated.
/// </remarks>
public sealed record ToolContextChange(
    ToolContextType ContextType,
    ToolAssociationAction Action,
    Guid? ToolId);
