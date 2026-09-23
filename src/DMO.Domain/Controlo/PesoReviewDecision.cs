namespace DMO.Domain.Controlo;

/// <summary>
/// One immutable Peso review decision event (approve / reject / reopen) on one <c>peso_id</c>.
/// </summary>
/// <remarks>
/// Authority: P2-T06 contract §6.1 (§6 <c>peso_review_decisions</c>). The event carries the Peso
/// Identity Rule's audit facts — who decided (<see cref="DecidedByUserId"/>), when
/// (<see cref="DecidedAt"/>), and which record version was decided
/// (<see cref="PesoVersionAtDecision"/> = the <c>pesos.version</c> observed by the actor) — plus
/// <see cref="PriorStatus"/> (the Peso status before the transition, for trail readability) and
/// the optional <see cref="Reason"/> (required for reject and reopen, always NULL for approve).
/// <para>
/// The event is <b>append-only and immutable after COMMIT</b>: there is no update/delete path.
/// It stores <b>no</b> Peso operational fact (no water temperature, no density, no rows, no
/// results) — the record remains the single source of truth; the version pointer is the
/// "which record version was decided" fact the Identity Rule requires.</para>
/// </remarks>
public sealed record PesoReviewDecision(
    PesoReviewDecisionId PesoReviewDecisionId,
    PesoId PesoId,
    PesoReviewDecisionKind Decision,
    Guid DecidedByUserId,
    DateTimeOffset DecidedAt,
    string? Reason,
    PesoStatus PriorStatus,
    int PesoVersionAtDecision,
    DateTimeOffset CreatedAt);