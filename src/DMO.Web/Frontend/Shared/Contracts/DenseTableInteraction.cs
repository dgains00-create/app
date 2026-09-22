namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The deterministic, presentation-only selection/open arbitration for a <c>DenseDataTable</c>.
/// </summary>
/// <remarks>
/// Authority: P2-T02 contract §5.6 (FINAL PRESENTATION CONTRACT) and §5.7.
/// <para>
/// This type is the <b>normative statement</b> of the frozen interaction rules; the P2-T02
/// JavaScript adapter is a thin DOM adapter over the same rules, so the arbitration is
/// unit-testable without a browser (contract Q1 accepted default).
/// </para>
/// <para>
/// Frozen semantics:
/// </para>
/// <list type="bullet">
/// <item>a single pointer click selects the row and <b>never</b> opens it;</item>
/// <item><c>Space</c> selects the row and never opens it;</item>
/// <item>a double click selects the row (idempotent) and requests open once per gesture;</item>
/// <item><c>Enter</c> leaves selection unchanged and requests open once per activation burst;</item>
/// <item>the explicit open control behaves exactly as <c>Enter</c>;</item>
/// <item>two consecutive open activations for the <b>same</b> row collapse into a single open
/// invocation; a selection input or an interaction with a different row reopens the window;</item>
/// <item>open is consumer-gated: with <see cref="OpenEnabled"/> false no open is ever requested;</item>
/// <item>selection is consumer-gated: with <see cref="SelectionEnabled"/> false no input selects;</item>
/// <item>at most one row is selected, and selection produces only a
/// <see cref="DenseTableEventKind.RowSelected"/> presentation event.</item>
/// </list>
/// <para>
/// The model holds no service, no clock, no identity, no URL and no domain vocabulary, and
/// exposes no member returning a <see cref="Uri"/>, a route or a navigation target. Selection
/// and open never emit <see cref="DenseTableEventKind.ActionInvoked"/> and never mutate domain
/// state.
/// </para>
/// </remarks>
public sealed class DenseTableInteraction
{
    private string? _openWindowRowKey;

    /// <summary>Creates the interaction model for the supplied consumer gating.</summary>
    public DenseTableInteraction(bool selectionEnabled, bool openEnabled)
    {
        SelectionEnabled = selectionEnabled;
        OpenEnabled = openEnabled;
    }

    /// <summary>Whether selection is enabled by the consumer.</summary>
    public bool SelectionEnabled { get; }

    /// <summary>Whether open is enabled by the consumer.</summary>
    public bool OpenEnabled { get; }

    /// <summary>The controlled selected row key, or <c>null</c> when nothing is selected.</summary>
    public string? SelectedKey { get; private set; }

    /// <summary>Whether a row is selected.</summary>
    public bool HasSelectedRow => SelectedKey is not null;

    /// <summary>
    /// A single pointer click: selects the row, never opens it, and closes the duplicate-open
    /// window because it is a selection input.
    /// </summary>
    /// <exception cref="ArgumentException">The supplied row key is blank.</exception>
    public DenseTableOutcome Click(string rowKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rowKey);
        _openWindowRowKey = null;

        return Select(rowKey);
    }

    /// <summary>
    /// The <c>Space</c> activation: selects the row, never opens it, and closes the
    /// duplicate-open window because it is a selection input.
    /// </summary>
    /// <exception cref="ArgumentException">The supplied row key is blank.</exception>
    public DenseTableOutcome Space(string rowKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rowKey);
        _openWindowRowKey = null;

        return Select(rowKey);
    }

    /// <summary>
    /// A double click: selects the row (idempotent) and requests open once for this gesture.
    /// </summary>
    /// <exception cref="ArgumentException">The supplied row key is blank.</exception>
    public DenseTableOutcome DoubleClick(string rowKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rowKey);

        // The gesture's own selection input closes the duplicate-open window first, so the
        // gesture raises exactly one open request.
        _openWindowRowKey = null;
        if (SelectionEnabled)
        {
            SelectedKey = rowKey;
        }

        var openRequested = RequestOpen(rowKey);

        return new DenseTableOutcome(
            SelectionEnabled && SelectedKey == rowKey,
            openRequested,
            openRequested
                ? DenseTableEvent.OpenRequested(rowKey)
                : SelectionEnabled
                    ? DenseTableEvent.RowSelected(rowKey)
                    : null);
    }

    /// <summary>
    /// The <c>Enter</c> activation: leaves selection unchanged and requests open once per
    /// activation burst.
    /// </summary>
    /// <exception cref="ArgumentException">The supplied row key is blank.</exception>
    public DenseTableOutcome Enter(string rowKey) => ActivateOpen(rowKey);

    /// <summary>
    /// The explicit open control: behaves exactly as <see cref="Enter"/> and requests open once
    /// per activation burst.
    /// </summary>
    /// <exception cref="ArgumentException">The supplied row key is blank.</exception>
    public DenseTableOutcome ActivateOpen(string rowKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rowKey);

        var openRequested = RequestOpen(rowKey);

        return new DenseTableOutcome(
            SelectionEnabled && SelectedKey == rowKey,
            openRequested,
            openRequested ? DenseTableEvent.OpenRequested(rowKey) : null);
    }

    /// <summary>
    /// Resets the model to a controlled selection value and closes the duplicate-open window.
    /// </summary>
    /// <param name="selectedKey">The controlled selected row key, or <c>null</c> for none.</param>
    /// <exception cref="ArgumentException">The supplied selected key is blank.</exception>
    public void Reset(string? selectedKey = null)
    {
        if (selectedKey is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(selectedKey);
        }

        SelectedKey = selectedKey;
        _openWindowRowKey = null;
    }

    private DenseTableOutcome Select(string rowKey)
    {
        if (SelectionEnabled)
        {
            SelectedKey = rowKey;
        }

        var selected = SelectionEnabled && SelectedKey == rowKey;

        return new DenseTableOutcome(
            selected,
            false,
            selected ? DenseTableEvent.RowSelected(rowKey) : null);
    }

    private bool RequestOpen(string rowKey)
    {
        if (!OpenEnabled || _openWindowRowKey == rowKey)
        {
            return false;
        }

        _openWindowRowKey = rowKey;

        return true;
    }
}
