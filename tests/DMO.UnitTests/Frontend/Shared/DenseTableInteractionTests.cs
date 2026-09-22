using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.UnitTests.Frontend.Shared;

/// <summary>
/// P2-T02 (A4) unit tests — the frozen <c>DenseDataTable</c> selection/open arbitration.
/// Authority: <c>plans/contracts/P2-T02_DENSE_DATA_TABLE_AUDIT_TRAIL_CONTRACT.md</c> §5.6, §5.7
/// and AC-4 to AC-7. Contract rows covered: U4, U5, U6, U7, U8.
/// Purpose: prove selection and open are two separate behaviours, that open is
/// consumer-gated and duplicate-collapsed, and that at most one row is selected.
/// Preconditions: the deterministic presentation-only C# interaction model (contract Q1
/// accepted default — no browser is required for P2-T02).
/// Required non-effects: no domain mutation, no event other than the frozen generic
/// vocabulary, and no URL/route/navigation target anywhere.
/// </summary>
public sealed class DenseTableInteractionTests
{
    /// <summary>U4 — a single pointer click selects and never requests open (AC-4).</summary>
    [Fact]
    public void Click_SelectsTheRowWithoutEverRequestingOpen()
    {
        var model = new DenseTableInteraction(selectionEnabled: true, openEnabled: true);

        var outcome = model.Click("row-2");

        Assert.True(outcome.Selected);
        Assert.False(outcome.OpenRequested);
        Assert.Equal("row-2", model.SelectedKey);
        Assert.Equal(DenseTableEventKind.RowSelected, outcome.Event!.Kind);
        Assert.Equal("row-2", outcome.Event.RowKey);
    }

    /// <summary>U4 — Space selects and never requests open (AC-4).</summary>
    [Fact]
    public void Space_SelectsTheRowWithoutEverRequestingOpen()
    {
        var model = new DenseTableInteraction(selectionEnabled: true, openEnabled: true);

        var outcome = model.Space("row-3");

        Assert.True(outcome.Selected);
        Assert.False(outcome.OpenRequested);
        Assert.Equal("row-3", model.SelectedKey);
        Assert.Equal(DenseTableEventKind.RowSelected, outcome.Event!.Kind);
    }

    /// <summary>U4 — selection never raises an action-invoked or a mutation event (AC-4).</summary>
    [Fact]
    public void SelectionInputs_RaiseOnlyTheRowSelectedEvent()
    {
        var model = new DenseTableInteraction(selectionEnabled: true, openEnabled: true);

        Assert.Equal(DenseTableEventKind.RowSelected, model.Click("row-1").Event!.Kind);
        Assert.Equal(DenseTableEventKind.RowSelected, model.Space("row-1").Event!.Kind);

        // A double click is the only gesture that also requests open, and it never invokes an action.
        var doubleClick = model.DoubleClick("row-1");
        Assert.Equal(DenseTableEventKind.OpenRequested, doubleClick.Event!.Kind);
        Assert.NotEqual(DenseTableEventKind.ActionInvoked, doubleClick.Event.Kind);
    }

    /// <summary>U5 — a double click requests open exactly once and leaves the row selected (AC-5).</summary>
    [Fact]
    public void DoubleClick_RequestsOpenOnceAndLeavesTheRowSelected()
    {
        var model = new DenseTableInteraction(selectionEnabled: true, openEnabled: true);

        var outcome = model.DoubleClick("row-7");

        Assert.True(outcome.Selected);
        Assert.True(outcome.OpenRequested);
        Assert.Equal("row-7", model.SelectedKey);
        Assert.Equal(DenseTableEventKind.OpenRequested, outcome.Event!.Kind);
        Assert.Equal("row-7", outcome.Event.RowKey);
    }

    /// <summary>
    /// U6 — two consecutive open activations for the same row produce exactly one open request,
    /// and a different row or a selection input reopens the window (AC-5).
    /// </summary>
    [Fact]
    public void ConsecutiveOpenActivations_ForTheSameRow_CollapseIntoOneRequest()
    {
        var model = new DenseTableInteraction(selectionEnabled: true, openEnabled: true);

        Assert.True(model.Enter("row-1").OpenRequested);
        Assert.False(model.Enter("row-1").OpenRequested);
        Assert.False(model.ActivateOpen("row-1").OpenRequested);
        Assert.Null(model.ActivateOpen("row-1").Event);

        // A different row reopens the collapse window.
        Assert.True(model.ActivateOpen("row-2").OpenRequested);
        Assert.False(model.Enter("row-2").OpenRequested);

        // A selection input also reopens the window for the same row.
        model.Click("row-2");
        Assert.True(model.Enter("row-2").OpenRequested);

        model.Space("row-2");
        Assert.True(model.ActivateOpen("row-2").OpenRequested);
    }

    /// <summary>U6 — Enter and the explicit open control behave identically (AC-5, AC-6).</summary>
    [Fact]
    public void Enter_And_TheExplicitOpenControl_BehaveIdentically()
    {
        var viaEnter = new DenseTableInteraction(selectionEnabled: true, openEnabled: true);
        var viaControl = new DenseTableInteraction(selectionEnabled: true, openEnabled: true);

        var enterOutcome = viaEnter.Enter("row-9");
        var controlOutcome = viaControl.ActivateOpen("row-9");

        Assert.Equal(enterOutcome.OpenRequested, controlOutcome.OpenRequested);
        Assert.Equal(enterOutcome.Event!.Kind, controlOutcome.Event!.Kind);
        Assert.Equal(enterOutcome.Event.RowKey, controlOutcome.Event.RowKey);

        // Enter leaves the selection unchanged.
        Assert.Null(viaEnter.SelectedKey);
        Assert.False(enterOutcome.Selected);
    }

    /// <summary>U7 — with open disabled no input ever requests open (AC-7).</summary>
    [Fact]
    public void OpenDisabled_NoInputEverRequestsOpen()
    {
        var model = new DenseTableInteraction(selectionEnabled: true, openEnabled: false);

        Assert.False(model.DoubleClick("row-1").OpenRequested);
        Assert.False(model.Enter("row-1").OpenRequested);
        Assert.False(model.ActivateOpen("row-1").OpenRequested);

        // The gesture still selects, and raises no open event.
        var outcome = model.DoubleClick("row-2");
        Assert.True(outcome.Selected);
        Assert.Equal(DenseTableEventKind.RowSelected, outcome.Event!.Kind);
        Assert.Null(model.Enter("row-2").Event);
    }

    /// <summary>U8 — with selection disabled no input selects (AC-4).</summary>
    [Fact]
    public void SelectionDisabled_NoInputSelects()
    {
        var model = new DenseTableInteraction(selectionEnabled: false, openEnabled: true);

        Assert.False(model.Click("row-1").Selected);
        Assert.False(model.Space("row-1").Selected);
        Assert.False(model.DoubleClick("row-1").Selected);
        Assert.Null(model.SelectedKey);
        Assert.False(model.HasSelectedRow);
        Assert.Null(model.Click("row-1").Event);
    }

    /// <summary>U8 — at most one row is selected and the selected key is the controlled value (AC-4).</summary>
    [Fact]
    public void Selection_IsSingleAndControlled()
    {
        var model = new DenseTableInteraction(selectionEnabled: true, openEnabled: false);

        model.Click("row-1");
        Assert.Equal("row-1", model.SelectedKey);

        model.Space("row-2");
        Assert.Equal("row-2", model.SelectedKey);

        model.Click("row-3");
        Assert.Equal("row-3", model.SelectedKey);

        // Reset applies the controlled value and closes the duplicate-open window.
        model.Reset("row-9");
        Assert.Equal("row-9", model.SelectedKey);

        model.Reset();
        Assert.Null(model.SelectedKey);
    }

    /// <summary>U8 — a blank row key is rejected rather than silently ignored.</summary>
    [Fact]
    public void Interaction_RejectsABlankRowKey()
    {
        var model = new DenseTableInteraction(selectionEnabled: true, openEnabled: true);

        Assert.Throws<ArgumentException>(() => model.Click(" "));
        Assert.Throws<ArgumentException>(() => model.Space(" "));
        Assert.Throws<ArgumentException>(() => model.DoubleClick(" "));
        Assert.Throws<ArgumentException>(() => model.Enter(" "));
        Assert.Throws<ArgumentException>(() => model.ActivateOpen(" "));
        Assert.Throws<ArgumentException>(() => model.Reset(" "));
    }
}
