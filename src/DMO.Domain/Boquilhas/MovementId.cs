namespace DMO.Domain.Boquilhas;

/// <summary>
/// Canonical identity of one Boquilhas movement/quantity event on one aggregate.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §5 and §15.1.
/// <para>
/// <c>movement_id</c> identifies one quantity movement/event on exactly one aggregate
/// (<c>movement_id → boquilhas_id</c> in both the production-linked and the standalone flow).
/// Movement rows never carry <c>tool_id</c>/<c>bq_id</c>/<c>jobon_id</c> (reachable through the
/// aggregate only).
/// </para>
/// <para>
/// The id is backend-allocated inside the append transaction; it is never client-supplied, and no
/// edit-event id ever re-identifies a quantity event (the audit id identifies audit rows only).
/// </para>
/// </remarks>
public readonly record struct MovementId(Guid Value)
{
    /// <summary>Wraps an existing identifier value.</summary>
    public static MovementId From(Guid value) => new(value);

    /// <summary>Allocates a new movement identity (backend-owned allocation).</summary>
    public static MovementId New() => new(Guid.NewGuid());

    /// <summary>Whether the identity holds an allocatable value.</summary>
    public bool IsEmpty => Value == Guid.Empty;

    /// <summary>The canonical value.</summary>
    public Guid ToGuid() => Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}