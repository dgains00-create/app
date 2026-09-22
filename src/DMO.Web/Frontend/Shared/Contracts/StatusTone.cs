namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The generic, optional tone a consumer may supply alongside status/availability text.
/// </summary>
/// <remarks>
/// Authority: <c>docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md</c> §10 and §11 ("optional
/// generic tone"). Ownership: A owns generic presentation; the status <b>vocabulary</b> is
/// owned by the consumer/domain.
/// <para>
/// Tone is deliberately generic and small, and reuses the existing design tokens already
/// published in <c>wwwroot/css/dmo-tokens.css</c> (<c>--dmo-success</c>, <c>--dmo-warning</c>,
/// <c>--dmo-danger</c>, plus the neutral ink/muted and accent tokens). Tone is
/// <b>supplementary only</b>: text is always present and no meaning may be conveyed by tone or
/// colour alone (freeze §3 rule 6, §10, §11).
/// </para>
/// </remarks>
public enum StatusTone
{
    /// <summary>No tone supplied, or the supplied status is unknown to the consumer.</summary>
    Neutral,

    /// <summary>Informational emphasis.</summary>
    Info,

    /// <summary>Positive/complete emphasis.</summary>
    Success,

    /// <summary>Attention emphasis; not an error.</summary>
    Warning,

    /// <summary>Error emphasis.</summary>
    Danger,
}
