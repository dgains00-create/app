namespace DMO.Application.JobOn;

/// <summary>
/// The single Job On service contract.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §12.3. The service holds no <c>HttpContext</c>, no route knowledge and
/// no presentation type; it never reads another module's tables and never references a
/// future-module type.
/// </remarks>
public interface IJobOnService
{
    /// <summary>Returns every matching occurrence for a reference (historical ones included).</summary>
    Task<JobOnResult> FindProductionsAsync(FindProductionsQuery query, CancellationToken cancellationToken);

    /// <summary>Reads the Job On ficha, or <c>NotFound</c>.</summary>
    Task<JobOnResult> GetAsync(Guid jobOnId, CancellationToken cancellationToken);

    /// <summary>Creates the occurrence and only the contexts the operator explicitly filled.</summary>
    Task<JobOnResult> CreateAsync(CreateJobOnCommand command, CancellationToken cancellationToken);

    /// <summary>Edits the four facts and the three Tool associations, gated by the date threshold.</summary>
    Task<JobOnResult> UpdateAsync(UpdateJobOnCommand command, CancellationToken cancellationToken);

    /// <summary>Reads the read-only duplication preview of an explicitly chosen source.</summary>
    Task<JobOnResult> PreviewDuplicateAsync(Guid sourceJobOnId, CancellationToken cancellationToken);

    /// <summary>Duplicates the chosen source into a new occurrence with new context identities.</summary>
    Task<JobOnResult> DuplicateAsync(DuplicateJobOnCommand command, CancellationToken cancellationToken);

    /// <summary>Deletes the occurrence only when no registered dependency probe objects.</summary>
    Task<JobOnResult> DeleteAsync(DeleteJobOnCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Lists every real <c>cm_contexts</c> row resolving to the supplied canonical Tool (the
    /// P2-T05 contract §20.4.2 additive read: one additive read-only member on the Job On
    /// application contract, accepted under Q-CAND). It is the §8.3 reverse read exposed to the
    /// consuming Controlo workflow for pending-association candidates; it modifies no other member
    /// and no Job On route.
    /// </summary>
    Task<JobOnResult> ListPesoAssociationCandidatesAsync(
        Guid toolId,
        CancellationToken cancellationToken);
}
