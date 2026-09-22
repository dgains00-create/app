namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// Domain-neutral presentation traits for the eight <see cref="AvailabilityState"/> outcomes.
/// </summary>
/// <remarks>
/// Authority: freeze §11. These traits encode the frozen distinctions
/// (<c>not-applicable</c> is not an error; <c>file-missing</c> is not <c>not-generated</c>;
/// <c>lookup-failed</c> is not no-file/no-record) without adding any domain rule.
/// </remarks>
public static class AvailabilityTraits
{
    /// <summary>The stable kebab-case CSS token for a state.</summary>
    public static string CssToken(AvailabilityState state) => state switch
    {
        AvailabilityState.Available => "available",
        AvailabilityState.NotGenerated => "not-generated",
        AvailabilityState.AwaitingApproval => "awaiting-approval",
        AvailabilityState.WorkspaceUnavailable => "workspace-unavailable",
        AvailabilityState.FileMissing => "file-missing",
        AvailabilityState.VersionsAvailable => "versions-available",
        AvailabilityState.NotApplicable => "not-applicable",
        AvailabilityState.LookupFailed => "lookup-failed",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown availability state."),
    };

    /// <summary>
    /// Whether the state is presented as an error. Only <see cref="AvailabilityState.LookupFailed"/>
    /// qualifies; <see cref="AvailabilityState.NotApplicable"/> is explicitly not an error, and
    /// neither is <see cref="AvailabilityState.NotGenerated"/> / <see cref="AvailabilityState.FileMissing"/>.
    /// </summary>
    public static bool IsError(AvailabilityState state) => state is AvailabilityState.LookupFailed;

    /// <summary>
    /// Whether the state is neutral/informational rather than an error or a normal success.
    /// </summary>
    public static bool IsNeutral(AvailabilityState state) =>
        state is AvailabilityState.NotGenerated or AvailabilityState.NotApplicable;

    /// <summary>Whether the state permits a supplied version choice.</summary>
    public static bool AllowsVersions(AvailabilityState state) =>
        state is AvailabilityState.VersionsAvailable or AvailabilityState.Available;

    /// <summary>Whether the state permits a supplied retry action.</summary>
    public static bool AllowsRetry(AvailabilityState state) =>
        state is AvailabilityState.LookupFailed or AvailabilityState.WorkspaceUnavailable;

    /// <summary>Whether a supplied label/detail is mandatory for the state to be intelligible.</summary>
    public static bool RequiresDetail(AvailabilityState state) =>
        state is AvailabilityState.WorkspaceUnavailable or AvailabilityState.FileMissing
            or AvailabilityState.AwaitingApproval or AvailabilityState.NotGenerated;

    /// <summary>The eight states that must remain mutually distinct (freeze §11).</summary>
    public static IReadOnlyList<AvailabilityState> All { get; } = Enum.GetValues<AvailabilityState>();

    /// <summary>
    /// Enforces the frozen carrier rules for supplied versions and actions on a given state.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Versions are supplied for a state that admits none, or <see cref="AvailabilityState.LookupFailed"/>
    /// is given versions (which would render a lookup failure as a no-file/no-record outcome).
    /// </exception>
    public static void ValidateSupplied(
        AvailabilityState state,
        IReadOnlyList<AvailabilityVersionPresentation>? versions,
        IReadOnlyList<SharedActionPresentation>? actions)
    {
        if (versions is { Count: > 0 } && !AllowsVersions(state))
        {
            throw new ArgumentException(
                $"Availability state '{CssToken(state)}' does not present versions.", nameof(versions));
        }

        if (actions is not null)
        {
            foreach (var action in actions)
            {
                if (!action.Enabled && string.IsNullOrWhiteSpace(action.DisabledReason))
                {
                    throw new ArgumentException(
                        "A disabled availability action must expose a supplied reason.", nameof(actions));
                }
            }
        }
    }
}
