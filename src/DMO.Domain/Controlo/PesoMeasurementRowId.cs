namespace DMO.Domain.Controlo;

/// <summary>
/// Stable persistence identity of one Peso measurement row.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §5.4. The persistence row id is the stable identity of one
/// measurement row of one Peso. It is <b>never</b> a CM identity, a tool number or a production
/// fact; frontend row keys stay opaque P2-T03 mechanics and are never canonical ids.
/// </remarks>
public readonly record struct PesoMeasurementRowId(Guid Value)
{
    /// <summary>Wraps an existing identifier value.</summary>
    public static PesoMeasurementRowId From(Guid value) => new(value);

    /// <summary>Allocates a new row identity (backend-owned allocation).</summary>
    public static PesoMeasurementRowId New() => new(Guid.NewGuid());

    /// <summary>Whether the identity holds an allocatable value.</summary>
    public bool IsEmpty => Value == Guid.Empty;

    /// <summary>The canonical value.</summary>
    public Guid ToGuid() => Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}