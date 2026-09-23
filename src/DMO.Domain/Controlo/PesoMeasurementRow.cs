namespace DMO.Domain.Controlo;

/// <summary>
/// One measurement row of one Peso: the entered weight plus the backend-derived results.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §5.4 and §16.2.
/// <para>
/// <see cref="WaterWeightG"/> is the operator-entered "Peso de água" (g). <see cref="CapacityCm3"/>
/// and <see cref="GlassWeightG"/> are computed by the backend formulas (§5.3) at calculate/save time
/// and stored with the row — registered per-row results, never recomputed from current Tool/Job On
/// state (§6). The row carries no <c>version</c>: it is only written inside a Peso mutation
/// transaction and the Peso's <c>version</c> protects the aggregate.
/// </para>
/// <para>
/// Deliberately absent: any per-row <c>tool_id</c>/<c>cm_id</c>/processo fact (S16), any label
/// column (Q-ROWLBL) and any jsonb. The dense 1-based <see cref="RowPosition"/> is the positional
/// pairing key of <c>CONTROLO.md</c> §8.6.
/// </para>
/// </remarks>
public sealed record PesoMeasurementRow(
    PesoMeasurementRowId PesoMeasurementRowId,
    PesoId PesoId,
    int RowPosition,
    decimal WaterWeightG,
    decimal CapacityCm3,
    decimal GlassWeightG,
    DateTimeOffset CreatedAt);