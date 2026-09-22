namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// Presentation model for the shared <c>RecordStatus</c> component.
/// </summary>
/// <remarks>
/// Authority: <c>docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md</c> §10 (FINAL PRESENTATION
/// CONTRACT) and <c>dmo-beta-master/contracts/SHARED_FRONTEND.md</c> "RecordStatus".
/// <para>
/// Frozen behavior implemented here:
/// </para>
/// <list type="bullet">
/// <item>the supplied status text is <b>always</b> shown;</item>
/// <item>the tone is supplementary and never colour-only;</item>
/// <item>an unknown supplied status uses the neutral tone and never guesses semantics;</item>
/// <item>status presentation grants no actions and infers no lifecycle.</item>
/// </list>
/// <para>
/// Explicit non-responsibilities (freeze §10): no domain status catalogue, lifecycle
/// transition, calculation, approval meaning, action availability or authorization.
/// </para>
/// </remarks>
public sealed record RecordStatusPresentation
{
    private RecordStatusPresentation(
        string text,
        StatusTone tone,
        string? markerLabel,
        string? assistiveDescription)
    {
        Text = text;
        Tone = tone;
        MarkerLabel = markerLabel;
        AssistiveDescription = assistiveDescription;
    }

    /// <summary>The supplied status text. Always rendered; never blank.</summary>
    public string Text { get; }

    /// <summary>
    /// The supplied tone. Supplementary only. An unknown/unsupplied consumer status resolves
    /// here to <see cref="StatusTone.Neutral"/>.
    /// </summary>
    public StatusTone Tone { get; }

    /// <summary>Optional supplied icon/marker label; <c>null</c> when none is supplied.</summary>
    public string? MarkerLabel { get; }

    /// <summary>Optional supplied assistive description; <c>null</c> when none is supplied.</summary>
    public string? AssistiveDescription { get; }

    /// <summary>Text is always present, so the component is never colour-only.</summary>
    public bool HasText => !string.IsNullOrWhiteSpace(Text);

    /// <summary>The stable CSS tone token (never the raw status text).</summary>
    public string ToneToken => Tone switch
    {
        StatusTone.Neutral => "neutral",
        StatusTone.Info => "info",
        StatusTone.Success => "success",
        StatusTone.Warning => "warning",
        StatusTone.Danger => "danger",
        _ => "neutral",
    };

    /// <summary>Whether a supplied marker/icon label accompanies the text.</summary>
    public bool HasMarker => !string.IsNullOrWhiteSpace(MarkerLabel);

    /// <summary>Whether a supplied assistive description accompanies the text.</summary>
    public bool HasAssistiveDescription => !string.IsNullOrWhiteSpace(AssistiveDescription);

    /// <summary>Creates a status presentation from supplied consumer meaning.</summary>
    /// <exception cref="ArgumentException">The supplied status text is blank.</exception>
    public static RecordStatusPresentation Create(
        string text,
        StatusTone tone = StatusTone.Neutral,
        string? markerLabel = null,
        string? assistiveDescription = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return new RecordStatusPresentation(text, Normalize(tone), markerLabel, assistiveDescription);
    }

    /// <summary>
    /// Creates a status presentation with <b>no</b> supplied tone: the neutral tone is used and
    /// no semantics are guessed (freeze §10).
    /// </summary>
    public static RecordStatusPresentation Neutral(string text, string? assistiveDescription = null) =>
        Create(text, StatusTone.Neutral, markerLabel: null, assistiveDescription);

    /// <summary>
    /// Resolves a consumer-supplied tone, treating an undefined enum value as neutral rather
    /// than guessing a tone.
    /// </summary>
    public static StatusTone Normalize(StatusTone tone) =>
        Enum.IsDefined(tone) ? tone : StatusTone.Neutral;
}
