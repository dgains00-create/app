namespace DMO.Domain.Controlo;

/// <summary>
/// Canonical identity of one immutable Peso review decision event (approve/reject/reopen).
/// </summary>
/// <remarks>
/// Authority: P2-T06 contract §5. <c>peso_review_decision_id</c> is the persistence identity of
/// one append-only decision event on one <c>peso_id</c> — it is <b>not</b> a Peso identity (a Peso
/// keeps its <c>peso_id</c> across every decision cycle), <b>not</b> a revision (the decision
/// trail is an event trail keyed by <c>peso_id</c>, not a record revision), and <b>not</b> a
/// document/send/production identity.
/// <para>
/// Allocation belongs to the backend application layer inside the decision transaction (the
/// database default <c>gen_random_uuid()</c> remains only the foundation column convention).
/// No <c>approval_peso_id</c>, no per-CM decision id and no send-request id exist in this
/// workstream.</para>
/// </remarks>
public readonly record struct PesoReviewDecisionId(Guid Value)
{
    /// <summary>Wraps an existing identifier value.</summary>
    public static PesoReviewDecisionId From(Guid value) => new(value);

    /// <summary>Allocates a new decision identity (backend-owned allocation, inside the decision transaction).</summary>
    public static PesoReviewDecisionId New() => new(Guid.NewGuid());

    /// <summary>Whether the identity holds an allocatable value.</summary>
    public bool IsEmpty => Value == Guid.Empty;

    /// <summary>The canonical value.</summary>
    public Guid ToGuid() => Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}