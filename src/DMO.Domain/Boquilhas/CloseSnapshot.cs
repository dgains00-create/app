namespace DMO.Domain.Boquilhas;

/// <summary>
/// The immutable close snapshot of one Boquilhas aggregate (one row per close event).
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §6.5 and §23.2.
/// <para>
/// The snapshot is written in the close transaction on the <b>same</b> <c>boquilhas_id</c> (no
/// replacement identity) with the backend closing user and the backend clock; the four balance
/// buckets are the replay-computed values at close and the utilisation still is copied as-is. It is
/// <b>immutable after COMMIT</b> — no UPDATE/DELETE path exists, and future repairer/line/
/// opening-fact/movement changes never modify it. A reopen → close cycle writes a <b>new</b>
/// snapshot row (history of closes per aggregate).
/// </para>
/// <para>
/// The snapshot is a frozen factual summary at close, <b>never a balance authority</b>: the live
/// balance continues to derive from movement facts and the snapshot never feeds any derivation.
/// </para>
/// </remarks>
public sealed record CloseSnapshot(
    Guid CloseSnapshotId,
    Guid BoquilhasId,
    Guid ClosedByUserId,
    DateTimeOffset ClosedAt,
    int InitialQuantity,
    DateOnly OpeningDate,
    int Disponivel,
    int EmReparacao,
    int Irreparavel,
    int EntradaExcecional,
    decimal? UtilisationPercent,
    DateTimeOffset CreatedAt);