namespace DMO.Domain.Controlo;

/// <summary>
/// The canonical three-value Peso status vocabulary.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §3.1 and <c>RECORD_LIFECYCLES.md</c> §4. The vocabulary is exactly
/// <c>pendente</c> (Pendente), <c>aprovado</c> (Aprovado) and <c>nao_aprovado</c> (Não aprovado),
/// stored as ASCII domain tokens and displayed with the canonical Portuguese labels.
/// <para>
/// P2-T05 writes only <c>pendente</c>. <c>aprovado</c>/<c>nao_aprovado</c> are written by P2-T06's
/// approved/rejected transitions on the same row. Status grants no action and infers no lifecycle
/// (<c>RecordStatus</c> rule). <c>rascunho</c> is superseded and must never appear.
/// </para>
/// </remarks>
public enum PesoStatus
{
    /// <summary>Pendente — the Create-owned state; reviewable once submitted.</summary>
    Pendente,

    /// <summary>Aprovado — written by P2-T06 on the same row (NOT authored by P2-T05).</summary>
    Aprovado,

    /// <summary>Não aprovado — written by P2-T06 on the same row (NOT authored by P2-T05).</summary>
    NaoAprovado,
}

/// <summary>
/// The exact stored ASCII tokens and canonical Portuguese display labels of the Peso statuses.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §3.1. The mapping is explicit and ordinal: no case folding and no
/// fuzzy matching is invented. The display label is presentation-only and never carries
/// authorization or lifecycle meaning.
/// </remarks>
public static class PesoStatusTokens
{
    /// <summary>The stored <c>status</c> token of a Peso status.</summary>
    public static string ToToken(PesoStatus status) => status switch
    {
        PesoStatus.Pendente => "pendente",
        PesoStatus.Aprovado => "aprovado",
        PesoStatus.NaoAprovado => "nao_aprovado",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown Peso status."),
    };

    /// <summary>Parses a stored <c>status</c> token, or <c>null</c> when it is not settled.</summary>
    public static PesoStatus? Parse(string? token) => token switch
    {
        "pendente" => PesoStatus.Pendente,
        "aprovado" => PesoStatus.Aprovado,
        "nao_aprovado" => PesoStatus.NaoAprovado,
        _ => null,
    };

    /// <summary>The canonical Portuguese display label of a Peso status.</summary>
    public static string DisplayLabel(PesoStatus status) => status switch
    {
        PesoStatus.Pendente => "Pendente",
        PesoStatus.Aprovado => "Aprovado",
        PesoStatus.NaoAprovado => "Não aprovado",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown Peso status."),
    };
}