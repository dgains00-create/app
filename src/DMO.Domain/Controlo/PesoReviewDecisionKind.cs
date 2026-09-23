namespace DMO.Domain.Controlo;

/// <summary>
/// The closed three-value P2-T06 decision-event vocabulary.
/// </summary>
/// <remarks>
/// Authority: P2-T06 contract §3.2. <c>aprovado</c>, <c>nao_aprovado</c> and <c>reaberto</c> are
/// the <b>decision events</b> of the append-only review trail — they are <b>not</b> Peso statuses
/// (the Peso status vocabulary stays the closed <c>pendente | aprovado | nao_aprovado</c> of
/// P2-T05 §3.1; no fourth status exists). The <c>reaberto</c> token derives directly from the
/// authority verb <c>reabrir</c> (master CONTROLO.md §15). No invented decision word exists, and
/// no event is ever produced automatically.
/// </remarks>
public enum PesoReviewDecisionKind
{
    /// <summary>Aprovado — the human approved the submitted Peso.</summary>
    Aprovado,

    /// <summary>Não aprovado — the human rejected the submitted Peso (reason required).</summary>
    NaoAprovado,

    /// <summary>Reaberto — the record returned to the draft handoff (reason required).</summary>
    Reaberto,
}

/// <summary>
/// The exact stored ASCII tokens and canonical Portuguese display labels of the decision events.
/// </summary>
/// <remarks>
/// Authority: P2-T06 contract §3.2. The mapping is explicit and ordinal: no case folding and no
/// fuzzy matching is invented. The display label is presentation-only and never carries
/// authorization or lifecycle meaning.</remarks>
public static class PesoReviewDecisionKindTokens
{
    /// <summary>The stored <c>decision</c> token of a decision kind.</summary>
    public static string ToToken(PesoReviewDecisionKind kind) => kind switch
    {
        PesoReviewDecisionKind.Aprovado => "aprovado",
        PesoReviewDecisionKind.NaoAprovado => "nao_aprovado",
        PesoReviewDecisionKind.Reaberto => "reaberto",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown decision kind."),
    };

    /// <summary>Parses a stored <c>decision</c> token, or <c>null</c> when it is not settled.</summary>
    public static PesoReviewDecisionKind? Parse(string? token) => token switch
    {
        "aprovado" => PesoReviewDecisionKind.Aprovado,
        "nao_aprovado" => PesoReviewDecisionKind.NaoAprovado,
        "reaberto" => PesoReviewDecisionKind.Reaberto,
        _ => null,
    };

    /// <summary>The canonical Portuguese display label of a decision kind.</summary>
    public static string DisplayLabel(PesoReviewDecisionKind kind) => kind switch
    {
        PesoReviewDecisionKind.Aprovado => "Aprovado",
        PesoReviewDecisionKind.NaoAprovado => "Não aprovado",
        PesoReviewDecisionKind.Reaberto => "Reaberto",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown decision kind."),
    };
}