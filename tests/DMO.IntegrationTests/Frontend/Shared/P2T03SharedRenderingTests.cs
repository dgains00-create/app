using DMO.IntegrationTests.Host;
using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.IntegrationTests.Frontend.Shared;

/// <summary>
/// P2-T03 (A5/A6) integration — shared composition and fixed desktop behaviour of all four
/// components.
/// Authority: <c>plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md</c> §3.5, §6.4,
/// §6.5 and the matrix rows RTS1–RTS3 (AC-43, AC-48, AC-49).
/// Purpose: prove that every non-ready surface of every component is rendered by the accepted
/// P2-T01 state partial with its own tokens and the four mandated distinctions intact, that each
/// component root carries its stable structural hook with the controls the fixed composition
/// requires present, and that an over-width region scrolls locally inside a keyboard-reachable
/// labelled overflow container without reflowing.
/// Preconditions: the real compiled P2-T03 and P2-T01 partials through the test host view engine.
/// Required non-effects: no component re-implements a state surface and no component moves a control
/// between structural regions.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class P2T03SharedRenderingTests
{
    [Fact]
    public async Task RTS1_EveryNonReadySurfaceIsDelegatedToTheAcceptedStatePartial()
    {
        using var factory = new DmoWebApplicationFactory();

        var pickerEmpty = await P2T03ComponentRenderer.RenderPickerAsync(
            factory, P2T03Fixtures.Picker(CommonState.Empty));
        var pickerFailed = await P2T03ComponentRenderer.RenderPickerAsync(
            factory, P2T03Fixtures.Picker(CommonState.LookupFailed));
        var pickerUnavailable = await P2T03ComponentRenderer.RenderPickerAsync(
            factory, P2T03Fixtures.Picker(CommonState.Unavailable));
        var pickerDenied = await P2T03ComponentRenderer.RenderPickerAsync(
            factory, P2T03Fixtures.Picker(CommonState.PermissionDenied));

        AssertStateSurface(pickerEmpty, "empty");
        AssertStateSurface(pickerFailed, "lookup-failed");
        AssertStateSurface(pickerUnavailable, "unavailable");
        AssertStateSurface(pickerDenied, "permission-denied");

        // The four mandated distinctions stay pairwise distinct renderings.
        Assert.NotEqual(pickerEmpty, pickerFailed);
        Assert.NotEqual(pickerEmpty, pickerUnavailable);
        Assert.NotEqual(pickerEmpty, pickerDenied);
        Assert.NotEqual(pickerFailed, pickerUnavailable);
        Assert.NotEqual(pickerUnavailable, pickerDenied);

        var summary = await P2T03ComponentRenderer.RenderSummaryRowAsync(
            factory, P2T03Fixtures.Summary(CommonState.LookupFailed));
        var rows = await P2T03ComponentRenderer.RenderRowsAsync(
            factory, P2T03Fixtures.Rows(CommonState.Unavailable, 0));
        var bar = await P2T03ComponentRenderer.RenderDecisionBarAsync(
            factory, P2T03Fixtures.Bar(CommonState.PermissionDenied, []));

        AssertStateSurface(summary, "lookup-failed");
        AssertStateSurface(rows, "unavailable");
        AssertStateSurface(bar, "permission-denied");

        // No component re-implements a state heading of its own.
        foreach (var html in new[] { pickerEmpty, summary, rows, bar })
        {
            Assert.Contains("class=\"dmo-state dmo-state--", html, StringComparison.Ordinal);
            Assert.DoesNotContain("dmo-picker__state", html, StringComparison.Ordinal);
            Assert.DoesNotContain("dmo-rows__state", html, StringComparison.Ordinal);
            Assert.DoesNotContain("dmo-decision__state", html, StringComparison.Ordinal);
            Assert.DoesNotContain("dmo-summary__state", html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task RTS2_EachComponentRendersItsStableHook_WithTheRequiredControlsPresent()
    {
        using var factory = new DmoWebApplicationFactory();

        var picker = await P2T03ComponentRenderer.RenderPickerAsync(
            factory,
            P2T03Fixtures.Picker(
                CommonState.Ready,
                [P2T03Fixtures.Candidate("cand-1")],
                createAction: SharedActionPresentation.CreateEnabled("create", "Criar registo")));

        Assert.Contains("data-dmo-picker=\"true\"", picker, StringComparison.Ordinal);
        Assert.Contains("data-dmo-search-input=\"true\"", picker, StringComparison.Ordinal);
        Assert.Contains("data-dmo-select-candidate=\"cand-1\"", picker, StringComparison.Ordinal);
        Assert.Contains("data-dmo-create=\"create\"", picker, StringComparison.Ordinal);
        Assert.Contains("data-dmo-cancel=", picker, StringComparison.Ordinal);

        var summary = await P2T03ComponentRenderer.RenderSummaryRowAsync(
            factory, P2T03Fixtures.Summary(CommonState.Ready, actions: [SharedActionPresentation.CreateEnabled("open", "Abrir ficha")]));

        Assert.Contains("data-dmo-summary-row=\"true\"", summary, StringComparison.Ordinal);
        Assert.Contains("data-dmo-summary-facts=\"true\"", summary, StringComparison.Ordinal);
        Assert.Contains("data-dmo-summary-actions=\"true\"", summary, StringComparison.Ordinal);

        var rows = await P2T03ComponentRenderer.RenderRowsAsync(
            factory, P2T03Fixtures.Rows(CommonState.Ready, 1, [P2T03Fixtures.Row("row-1")]));

        Assert.Contains("data-dmo-measurement-rows=\"true\"", rows, StringComparison.Ordinal);
        Assert.Contains("data-dmo-rows-scroll=\"true\"", rows, StringComparison.Ordinal);
        Assert.Contains("data-dmo-remove=\"row-1\"", rows, StringComparison.Ordinal);
        Assert.Contains("data-dmo-add=\"true\"", rows, StringComparison.Ordinal);
        Assert.Contains("data-dmo-field=\"row-1-a\"", rows, StringComparison.Ordinal);

        var bar = await P2T03ComponentRenderer.RenderDecisionBarAsync(
            factory, P2T03Fixtures.Bar(CommonState.Ready, [P2T03Fixtures.Action("alpha", DecisionBarActionGroup.Primary)]));

        Assert.Contains("data-dmo-decision-bar=\"true\"", bar, StringComparison.Ordinal);
        Assert.Contains("data-dmo-action-key=\"alpha\"", bar, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RTS3_OverWidthRegionsScrollLocally_AndNoStructuralVariantIsEmitted()
    {
        using var factory = new DmoWebApplicationFactory();

        var picker = await P2T03ComponentRenderer.RenderPickerAsync(
            factory, P2T03Fixtures.Picker(CommonState.Ready, [P2T03Fixtures.Candidate("cand-1")]));
        var rows = await P2T03ComponentRenderer.RenderRowsAsync(
            factory, P2T03Fixtures.Rows(CommonState.Ready, 1, [P2T03Fixtures.Row("row-1")]));
        var bar = await P2T03ComponentRenderer.RenderDecisionBarAsync(
            factory, P2T03Fixtures.Bar(CommonState.Ready, [P2T03Fixtures.Action("alpha", DecisionBarActionGroup.Primary)]));

        // Keyboard-reachable, labelled local overflow containers.
        Assert.Contains("data-dmo-picker-candidates-scroll=\"true\"", picker, StringComparison.Ordinal);
        Assert.Contains("data-dmo-rows-scroll=\"true\"", rows, StringComparison.Ordinal);

        foreach (var html in new[] { picker, rows, bar })
        {
            Assert.DoesNotContain("matchMedia", html, StringComparison.Ordinal);
            Assert.DoesNotContain("@media", html, StringComparison.Ordinal);
        }

        var css = P2T03ProductionScan.WithoutCssComments(
            P2T03ProductionScan.Read("src/DMO.Web/wwwroot/css/dmo-components.css"));

        Assert.Contains(".dmo-rows__scroll", css, StringComparison.Ordinal);
        Assert.Contains("overflow-x: auto", css, StringComparison.Ordinal);
        Assert.Contains(".dmo-decision__actions", css, StringComparison.Ordinal);
    }

    private static void AssertStateSurface(string html, string token)
    {
        Assert.Contains($"dmo-state--{token}", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-state-message=", html, StringComparison.Ordinal);
        Assert.Contains("Mensagem fornecida pelo consumidor.", html, StringComparison.Ordinal);
    }
}
