namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// A generic consumer-supplied action carried by a shared component.
/// </summary>
/// <remarks>
/// Authority: freeze §11 (availability actions: "generic actions with visible/enabled/disabled
/// reason/pending state") and §3 rule 5 ("A disabled control exposes a visible, programmatically
/// associated supplied reason").
/// <para>
/// The action key is <b>opaque</b>: it is consumer-controlled presentation data and is never a
/// canonical domain identity or an endpoint. The component raises only the frozen generic
/// event; it encodes no transition rule (freeze §14).
/// </para>
/// </remarks>
public sealed record SharedActionPresentation
{
    private SharedActionPresentation(
        string key,
        string label,
        bool visible,
        bool enabled,
        string? disabledReason,
        string? pendingLabel)
    {
        Key = key;
        Label = label;
        Visible = visible;
        Enabled = enabled;
        DisabledReason = disabledReason;
        PendingLabel = pendingLabel;
    }

    /// <summary>Opaque consumer action key. Never a canonical identity or endpoint.</summary>
    public string Key { get; }

    /// <summary>Visible action label.</summary>
    public string Label { get; }

    /// <summary>Whether the action is presented at all.</summary>
    public bool Visible { get; }

    /// <summary>Whether the action can be invoked.</summary>
    public bool Enabled { get; }

    /// <summary>The supplied reason shown when the action is disabled; <c>null</c> when enabled.</summary>
    public string? DisabledReason { get; }

    /// <summary>The supplied pending label shown while the action is in progress.</summary>
    public string? PendingLabel { get; }

    /// <summary>
    /// Creates an action, enforcing the frozen rule that a disabled action always carries a
    /// visible, supplied reason.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The key or label is blank, or the action is disabled without a supplied reason.
    /// </exception>
    public static SharedActionPresentation Create(
        string key,
        string label,
        bool enabled,
        string? disabledReason = null,
        string? pendingLabel = null,
        bool visible = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        if (!enabled && string.IsNullOrWhiteSpace(disabledReason))
        {
            throw new ArgumentException(
                "A disabled action must expose a supplied disabled reason.", nameof(disabledReason));
        }

        return new SharedActionPresentation(key, label, visible, enabled, disabledReason, pendingLabel);
    }

    /// <summary>Creates an enabled action.</summary>
    public static SharedActionPresentation CreateEnabled(string key, string label, string? pendingLabel = null) =>
        Create(key, label, enabled: true, disabledReason: null, pendingLabel: pendingLabel);

    /// <summary>Creates a disabled action with its mandatory associated reason.</summary>
    public static SharedActionPresentation CreateDisabled(string key, string label, string disabledReason) =>
        Create(key, label, enabled: false, disabledReason: disabledReason);

    /// <summary>Whether the action is disabled and therefore must expose its reason.</summary>
    public bool RequiresDisabledReason => !Enabled;

    /// <summary>The stable CSS token used for the action (kebab-case key is never used in CSS).</summary>
    public string StateToken => Enabled ? "enabled" : "disabled";
}
