namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// One supplied action of the decision bar: the accepted shared action carrier plus its supplied
/// presentation classification.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.4.2.
/// <para>
/// The accepted <see cref="SharedActionPresentation"/> is reused <b>verbatim</b> — this type adds
/// exactly one presentation dimension (the supplied group) and duplicates no member of it, so the
/// accepted mandatory disabled-reason rule is inherited rather than re-implemented.
/// </para>
/// <para>
/// The component encodes no transition: it does not know, and must never infer, which supplied
/// action means what. Labels, availability, disabled reasons and pending labels are all supplied.
/// </para>
/// </remarks>
public sealed record DecisionBarActionPresentation
{
    private DecisionBarActionPresentation(SharedActionPresentation action, DecisionBarActionGroup group)
    {
        Action = action;
        Group = group;
    }

    /// <summary>The accepted supplied action carrier.</summary>
    public SharedActionPresentation Action { get; }

    /// <summary>The supplied presentation classification.</summary>
    public DecisionBarActionGroup Group { get; }

    /// <summary>The opaque supplied action key, echoed verbatim on invocation.</summary>
    public string Key => Action.Key;

    /// <summary>The supplied visible label.</summary>
    public string Label => Action.Label;

    /// <summary>The stable CSS token of the supplied group.</summary>
    public string GroupToken => Group switch
    {
        DecisionBarActionGroup.Primary => "primary",
        DecisionBarActionGroup.Secondary => "secondary",
        _ => "danger",
    };

    /// <summary>Whether the supplied action carries visible textual risk meaning in addition to styling.</summary>
    public bool RequiresTextualGroupMeaning => Group == DecisionBarActionGroup.Danger;

    /// <summary>Creates a supplied action with its mandatory presentation classification.</summary>
    /// <exception cref="ArgumentNullException">The supplied action is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The supplied group is not a defined value.</exception>
    public static DecisionBarActionPresentation Create(
        SharedActionPresentation action,
        DecisionBarActionGroup group)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!Enum.IsDefined(group))
        {
            throw new ArgumentOutOfRangeException(nameof(group), group, "Unknown action group.");
        }

        return new DecisionBarActionPresentation(action, group);
    }
}
