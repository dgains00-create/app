namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// Where focus belongs after a picker transition.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §4.1 (transition table) and §5.1.4.
/// <para>
/// The target is a deterministic presentation fact so the frozen focus rules are unit-testable
/// without a browser (contract §8.4, accepted verification strategy). The model never moves focus
/// itself and never traps it.
/// </para>
/// </remarks>
public enum ToolPickerFocusTarget
{
    /// <summary>The transition does not move focus (no focus theft).</summary>
    Unchanged,

    /// <summary>Focus the search input (accepted opening pattern).</summary>
    SearchInput,

    /// <summary>Focus the picker region heading when no search region is presented.</summary>
    RegionHeading,

    /// <summary>Restore focus to the control that invoked the picker (close/cancel/return).</summary>
    InvokingControl,
}
