namespace DMO.Domain.Controlo;

/// <summary>
/// One Peso control/result fact: the persisted authoritative record of one measurement.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §3, §5.2, §5.7, §6 and §16.1.
/// <para>
/// The Peso always anchors to <b>exactly one</b> of two direct relations: <see cref="CmId"/>
/// (production anchor; then <see cref="ToolId"/> is null) or <see cref="ToolId"/> (truthful pending
/// anchor, <c>Job On por associar</c>; then <see cref="CmId"/> is null). The database CHECK
/// <c>(cm_id IS NULL)::int + (tool_id IS NULL)::int = 1</c> enforces the exclusive anchor.
/// </para>
/// <para>
/// Deliberately absent: <c>jobon_id</c>, <c>reference</c>, <c>production_number</c>, <c>machine</c>
/// (all reachable through <c>cm_id</c> — §6.2 traversal), <c>previous_peso_id</c> (Comparação is
/// not this contract), approval columns (P2-T06), any nominal/mold-state/notes column (S2) and any
/// jsonb blob. The frozen calculation-configuration facts actually used
/// (<see cref="GlassDensityGCm3"/>) and the per-row results (in
/// <see cref="PesoMeasurementRow"/>) are the historical evidence (AC-H3).
/// </para>
/// </remarks>
public sealed record Peso(
    PesoId PesoId,
    Guid? CmId,
    Guid? ToolId,
    PesoStatus Status,
    DateTimeOffset? SubmittedAt,
    Guid? SubmittedByUserId,
    decimal WaterTemperature,
    decimal? VolumeMarisaBq,
    decimal? VolumePuncaoPu,
    decimal? GlassDensityGCm3,
    string? PreviousProductionEndReference,
    string? PreviousAverageWeightReference,
    int Version,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<PesoMeasurementRow> Rows)
{
    /// <summary>Whether the Peso is the truthful pending case (<c>tool_id</c> anchor).</summary>
    public bool IsPending => ToolId is not null && CmId is null;

    /// <summary>Whether the Peso is production-bound (<c>cm_id</c> anchor).</summary>
    public bool IsProductionBound => CmId is not null && ToolId is null;

    /// <summary>
    /// Whether the Peso is reviewable by P2-T06: <c>submitted_at IS NOT NULL</c> and status
    /// <c>pendente</c> (§3.1, §5.7).
    /// </summary>
    public bool IsReviewable => SubmittedAt is not null && Status == PesoStatus.Pendente;

    /// <summary>The row water weights in dense positional order (re-validation carrier).</summary>
    public IReadOnlyList<decimal> RowWaterWeightsG =>
        Rows.OrderBy(row => row.RowPosition).Select(row => row.WaterWeightG).ToList();
}