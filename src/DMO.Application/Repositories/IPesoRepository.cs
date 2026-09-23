using DMO.Domain.Controlo;

namespace DMO.Application.Repositories;

/// <summary>
/// The single Peso repository contract (P2-T05 contract §20.2, exact).
/// </summary>
/// <remarks>
/// <para>
/// Reads load the Peso with its full row set; writes open their own transaction and map PostgreSQL
/// constraint violations onto typed application failures (§20.2 binding rules):
/// <c>FK_pesos_cm_contexts_cm_id</c> → <c>CM_CONTEXT_NOT_FOUND</c>,
/// <c>FK_pesos_tools_tool_id</c> → <c>TOOL_NOT_FOUND</c>, and the SQLSTATE <c>23514</c> CHECK
/// backstop of <c>peso_measurement_rows_capacity_check</c>/<c>peso_measurement_rows_glass_check</c>
/// → the same <c>ResultNonPositive</c> refusal the validator raises (C2) — a validator-passing
/// combination can never surface as a 500.</para>
/// </remarks>
public interface IPesoRepository
{
    /// <summary>Reads one Peso with its full row set, or <c>null</c>.</summary>
    Task<Peso?> GetByIdAsync(Guid pesoId, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts the Peso and every measurement row in ONE transaction (version = 1), or inserts
    /// nothing.
    /// </summary>
    Task<Peso> CreatedAsync(
        Peso peso,
        IReadOnlyList<PesoMeasurementRow> rows,
        CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the whole row set and the input facts in ONE transaction (version += 1), or writes
    /// nothing.
    /// </summary>
    Task<Peso> UpdatedAsync(
        Peso peso,
        IReadOnlyList<PesoMeasurementRow> rows,
        CancellationToken cancellationToken);

    /// <summary>Applies the submitted handoff (attribution + version += 1) in one transaction.</summary>
    Task<Peso> SubmittedAsync(Peso peso, CancellationToken cancellationToken);

    /// <summary>Applies the pending → production anchor swap (version += 1) in one transaction.</summary>
    Task<Peso> AssociatedAsync(Peso peso, CancellationToken cancellationToken);
}