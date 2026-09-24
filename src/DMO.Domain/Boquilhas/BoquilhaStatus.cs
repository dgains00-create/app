namespace DMO.Domain.Boquilhas;

/// <summary>
/// The closed aggregate status set of a Boquilhas trace: exactly two values.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §3.2.
/// <para>
/// <c>active</c> (Ativo) = the trace accepts movements/edits/opening-fact updates and appears in the
/// Registo lot grid; <c>closed</c> (Fechado) = the trace has a committed immutable close snapshot,
/// appears only in the archived projection / Histórico ("aggregate/file state" filter) and accepts
/// no mutation without a reopen. Status is aggregate <b>lifecycle</b> — never a movement type and
/// never a balance component. No third status exists (no <c>rascunho</c>, no <c>arquivado</c>, no
/// <c>cancelado</c> — "archived" is the derived file-state projection of a closed trace, not a
/// column).
/// </para>
/// </remarks>
public enum BoquilhaStatus
{
    /// <summary>Ativo — the trace accepts movements/edits/opening-fact updates.</summary>
    Active,

    /// <summary>Fechado — the trace has a committed immutable close snapshot.</summary>
    Closed,
}

/// <summary>
/// The stored ASCII tokens of the closed <see cref="BoquilhaStatus"/> set.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §3.2 (tokens <c>active</c>|<c>closed</c>) and §7.4
/// (<c>boquilhas_status_check</c>).
/// </remarks>
public static class BoquilhaStatusTokens
{
    /// <summary>The stored token of one status.</summary>
    public static string ToToken(BoquilhaStatus status) => status switch
    {
        BoquilhaStatus.Active => "active",
        BoquilhaStatus.Closed => "closed",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown aggregate status."),
    };

    /// <summary>Parses a stored status token, or <c>null</c> when it is not settled.</summary>
    public static BoquilhaStatus? Parse(string? token) => token switch
    {
        "active" => BoquilhaStatus.Active,
        "closed" => BoquilhaStatus.Closed,
        _ => null,
    };

    /// <summary>The canonical Portuguese label of one status (presentation only).</summary>
    public static string ToLabel(BoquilhaStatus status) => status switch
    {
        BoquilhaStatus.Active => "Ativo",
        BoquilhaStatus.Closed => "Fechado",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown aggregate status."),
    };
}