namespace DMO.Domain.Boquilhas;

/// <summary>
/// Canonical identity of one Boquilhas aggregate (one collective BQ external-repair trace).
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §5 and §15.1.
/// <para>
/// <c>boquilhas_id</c> is one collective BQ external-repair aggregate (one repair trace). It is
/// <b>not</b> <c>bq_id</c> (the frozen Job On BQ context), <b>not</b> <c>tool_id</c> (the canonical
/// Tool) and <b>not</b> a physical BQ piece: the aggregate anchors exclusively to exactly one of
/// <c>bq_id</c> (production-linked) or <c>tool_id</c> (standalone) — DB-enforced by
/// <c>boquilhas_anchor_exclusive_check</c>.
/// </para>
/// <para>
/// The id is backend-allocated inside the create transaction (never client-supplied, never
/// minted from display text); <c>Guid.NewGuid()</c> used here is only the .NET-side allocation the
/// create transaction persists — the foundation <c>gen_random_uuid()</c> default remains a column
/// default only, never the application source.
/// </para>
/// </remarks>
public readonly record struct BoquilhasId(Guid Value)
{
    /// <summary>Wraps an existing identifier value.</summary>
    public static BoquilhasId From(Guid value) => new(value);

    /// <summary>Allocates a new aggregate identity (backend-owned allocation).</summary>
    public static BoquilhasId New() => new(Guid.NewGuid());

    /// <summary>Whether the identity holds an allocatable value.</summary>
    public bool IsEmpty => Value == Guid.Empty;

    /// <summary>The canonical value.</summary>
    public Guid ToGuid() => Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}