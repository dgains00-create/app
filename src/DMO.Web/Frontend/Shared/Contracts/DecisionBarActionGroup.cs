namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The supplied presentation classification of an action.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.4.2 and §3.4.3. The group is a supplied <b>classification</b> that
/// never reorders, relocates or filters an action: the supplied action sequence remains
/// authoritative. The component never guesses an action's emphasis, so the group is mandatory.
/// </remarks>
public enum DecisionBarActionGroup
{
    /// <summary>The supplied primary classification.</summary>
    Primary,

    /// <summary>The supplied secondary classification.</summary>
    Secondary,

    /// <summary>The supplied risk classification; presented with visible textual meaning too.</summary>
    Danger,
}
