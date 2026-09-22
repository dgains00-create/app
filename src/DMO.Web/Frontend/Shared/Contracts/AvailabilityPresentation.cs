namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// Presentation model for the shared <c>AvailabilityState</c> component.
/// </summary>
/// <remarks>
/// Authority: freeze §11 (FINAL PRESENTATION CONTRACT); Beta summary
/// <c>contracts/SHARED_FRONTEND.md</c> "AvailabilityState"; meanings in
/// <c>contracts/DOCUMENTS_AND_FILES.md</c> §5.
/// <para>
/// The component renders a supplied availability fact. It never checks storage, never infers
/// whether a record exists, never generates output, never defines versions, never exposes
/// paths and never decides authorization (freeze §11 "Explicit non-responsibilities").
/// </para>
/// </remarks>
public sealed record AvailabilityPresentation
{
    private AvailabilityPresentation(
        AvailabilityState state,
        string text,
        string? detail,
        IReadOnlyList<AvailabilityVersionPresentation> versions,
        IReadOnlyList<SharedActionPresentation> actions,
        string regionLabel)
    {
        State = state;
        Text = text;
        Detail = detail;
        Versions = versions;
        Actions = actions;
        RegionLabel = regionLabel;
    }

    /// <summary>One of the eight distinct availability outcomes.</summary>
    public AvailabilityState State { get; }

    /// <summary>The explicit supplied state text. Always rendered.</summary>
    public string Text { get; }

    /// <summary>Optional supplied detail; <c>null</c> when none is supplied.</summary>
    public string? Detail { get; }

    /// <summary>Optional supplied opaque versions; empty when none are supplied.</summary>
    public IReadOnlyList<AvailabilityVersionPresentation> Versions { get; }

    /// <summary>Supplied consumer actions (open/regenerate/retry) with their disabled reasons.</summary>
    public IReadOnlyList<SharedActionPresentation> Actions { get; }

    /// <summary>The supplied accessible region label.</summary>
    public string RegionLabel { get; }

    /// <summary>The stable CSS token for the eight-state vocabulary (kebab-case).</summary>
    public string StateToken => AvailabilityTraits.CssToken(State);

    /// <summary>Whether the state must be presented as an error surface.</summary>
    /// <remarks>Only <see cref="AvailabilityState.LookupFailed"/> is an error;
    /// <see cref="AvailabilityState.NotApplicable"/> is explicitly not one (freeze §11).</remarks>
    public bool IsError => AvailabilityTraits.IsError(State);

    /// <summary>Whether the state is neutral/informational (not generated, not applicable).</summary>
    public bool IsNeutral => AvailabilityTraits.IsNeutral(State);

    /// <summary>Whether a supplied version choice is presented.</summary>
    public bool HasVersions => Versions.Count > 0;

    /// <summary>Whether supplied detail accompanies the state text.</summary>
    public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);

    /// <summary>Whether supplied actions are presented.</summary>
    public bool HasActions => Actions.Count > 0;

    /// <summary>
    /// Creates an availability presentation.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The state text or region label is blank, or an undefined enum value is supplied.
    /// </exception>
    public static AvailabilityPresentation Create(
        AvailabilityState state,
        string text,
        string regionLabel,
        string? detail = null,
        IReadOnlyList<AvailabilityVersionPresentation>? versions = null,
        IReadOnlyList<SharedActionPresentation>? actions = null)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown availability state.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(regionLabel);
        AvailabilityTraits.ValidateSupplied(state, versions, actions);

        return new AvailabilityPresentation(
            state,
            text,
            detail,
            versions ?? [],
            actions ?? [],
            regionLabel);
    }

    /// <summary>Creates the neutral <see cref="AvailabilityState.NotApplicable"/> outcome (not an error).</summary>
    public static AvailabilityPresentation NotApplicable(string text, string regionLabel, string? detail = null) =>
        Create(AvailabilityState.NotApplicable, text, regionLabel, detail);

    /// <summary>Creates the <see cref="AvailabilityState.LookupFailed"/> outcome, optionally with a retry action.</summary>
    public static AvailabilityPresentation LookupFailed(
        string text,
        string regionLabel,
        SharedActionPresentation? retry = null,
        string? detail = null) =>
        Create(AvailabilityState.LookupFailed, text, regionLabel, detail, null, retry is null ? null : [retry]);
}
