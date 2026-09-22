using System.Text.RegularExpressions;
using DMO.IntegrationTests.Host;
using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.IntegrationTests.Frontend.Shared;

/// <summary>
/// P2-T03 (A5) integration — the real rendered compact summary row.
/// Authority: <c>plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md</c> §3.2, §3.2.3,
/// §5.2 and the matrix rows RTR1–RTR5 (AC-17 … AC-22, AC-45).
/// Purpose: prove in rendered markup that only supplied facts are presented with visible labels in
/// the fixed slot order, that an absent optional slot produces no placeholder at all, that the
/// accepted status partial renders the supplied status, that supplied actions keep their order and
/// their associated disabled reason, and that no address or canonical identity is emitted.
/// Preconditions: the real compiled P2-T03 partials rendered through the test host view engine.
/// Required non-effects: no synthesized value and no lookup or inference in the rendering.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class ToolSummaryRowRenderingTests
{
    private static readonly Regex FormTarget = new(@"[\s""']action\s*=|[\s""']method\s*=", RegexOptions.Singleline);

    [Fact]
    public async Task RTR1_OnlySuppliedFactsRender_WithVisibleLabels()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderSummaryRowAsync(
            factory,
            P2T03Fixtures.Summary(CommonState.Ready));

        Assert.Contains("data-dmo-summary-row=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-summary-fact=\"Tipo\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-fact-label=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-fact-value=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("MF", html, StringComparison.Ordinal);
        Assert.Contains("REF-1", html, StringComparison.Ordinal);

        // The fixed slot order is preserved and no fact is collapsed into one unlabelled string.
        var type = html.IndexOf("data-dmo-summary-fact=\"Tipo\"", StringComparison.Ordinal);
        var reference = html.IndexOf("data-dmo-summary-fact=\"Referência\"", StringComparison.Ordinal);
        var lot = html.IndexOf("data-dmo-summary-fact=\"Lote\"", StringComparison.Ordinal);

        Assert.True(type >= 0 && reference > type && lot > reference, "Facts must keep the fixed slot order.");
    }

    [Fact]
    public async Task RTR2_OnlySuppliedActionsAreInteractive_InSuppliedOrder()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderSummaryRowAsync(
            factory,
            P2T03Fixtures.Summary(
                CommonState.Ready,
                actions:
                [
                    SharedActionPresentation.CreateEnabled("open", "Abrir ficha"),
                    SharedActionPresentation.CreateDisabled("edit", "Editar", "Sem permissão para editar."),
                    SharedActionPresentation.Create("hidden", "Oculta", true, null, null, visible: false),
                ]));

        Assert.Contains("data-dmo-action=\"open\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-item-key=\"item-1\"", html, StringComparison.Ordinal);
        Assert.Contains("Abrir ficha — contexto item-1", html, StringComparison.Ordinal);

        Assert.Equal(2, Count(html, "data-dmo-action=\""));
        Assert.DoesNotContain("data-dmo-action=\"hidden\"", html, StringComparison.Ordinal);

        // A disabled action renders disabled, aria-disabled and an associated rendered reason.
        var reasonId = ReasonIdOf(html, "edit");

        Assert.Contains("aria-disabled=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"" + reasonId + "\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"" + reasonId + "\"", html, StringComparison.Ordinal);
        Assert.Contains("Sem permissão para editar.", html, StringComparison.Ordinal);

        var open = html.IndexOf("data-dmo-action=\"open\"", StringComparison.Ordinal);
        var edit = html.IndexOf("data-dmo-action=\"edit\"", StringComparison.Ordinal);

        Assert.True(open >= 0 && edit > open, "Supplied action order must be preserved.");
    }

    [Fact]
    public async Task RTR3_AbsentOptionalFact_ProducesNoPlaceholder()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderSummaryRowAsync(
            factory,
            P2T03Fixtures.Summary(CommonState.Ready));

        Assert.DoesNotContain("data-dmo-summary-fact=\"Quantidade\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain(">0<", html, StringComparison.Ordinal);
        Assert.DoesNotContain("—", html, StringComparison.Ordinal);
        Assert.DoesNotContain(">false<", html, StringComparison.Ordinal);

        var withZero = await P2T03ComponentRenderer.RenderSummaryRowAsync(
            factory,
            P2T03Fixtures.Summary(CommonState.Ready, quantity: P2T03Fixtures.SummaryFact("Quantidade", "0")));

        Assert.Contains("data-dmo-summary-fact=\"Quantidade\"", withZero, StringComparison.Ordinal);
        Assert.Contains(">0<", withZero, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RTR4_StatusRendersThroughTheAcceptedStatusPartial()
    {
        using var factory = new DmoWebApplicationFactory();

        var withoutStatus = await P2T03ComponentRenderer.RenderSummaryRowAsync(
            factory, P2T03Fixtures.Summary(CommonState.Ready));

        Assert.DoesNotContain("data-dmo-summary-status=\"true\"", withoutStatus, StringComparison.Ordinal);
        Assert.DoesNotContain("dmo-status--", withoutStatus, StringComparison.Ordinal);

        var withStatus = await P2T03ComponentRenderer.RenderSummaryRowAsync(
            factory,
            P2T03Fixtures.Summary(
                CommonState.Ready,
                status: RecordStatusPresentation.Create("Estado fornecido", StatusTone.Warning)));

        Assert.Contains("data-dmo-summary-status=\"true\"", withStatus, StringComparison.Ordinal);
        Assert.Contains("dmo-status--warning", withStatus, StringComparison.Ordinal);
        Assert.Contains("data-dmo-status-text=\"Estado fornecido\"", withStatus, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RTR5_NoAddressFormTargetOrCanonicalIdentityIsEmitted()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderSummaryRowAsync(
            factory,
            P2T03Fixtures.Summary(
                CommonState.Ready,
                status: RecordStatusPresentation.Create("Estado fornecido"),
                actions: [SharedActionPresentation.CreateEnabled("open", "Abrir ficha")]));

        Assert.DoesNotContain("href=", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<form", html, StringComparison.Ordinal);
        Assert.DoesNotMatch(FormTarget, html);
        Assert.DoesNotMatch(@"\blocation\s*\.", html);
        Assert.DoesNotMatch(@"\bwindow\s*\.\s*open", html);

        foreach (var token in P2T03ProductionScan.CanonicalIdentityTokens)
        {
            Assert.DoesNotContain(token, html, StringComparison.OrdinalIgnoreCase);
        }
    }

    // Additive evidence for Architect observation O4 (contract §3.2.3 rule 7, supplied visibility).
    [Fact]
    public async Task O4_SuppliedActionVisibilityIsHonoured_WithNoInventedControl()
    {
        using var factory = new DmoWebApplicationFactory();

        var allHidden = await P2T03ComponentRenderer.RenderSummaryRowAsync(
            factory,
            P2T03Fixtures.Summary(
                CommonState.Ready,
                actions:
                [
                    SharedActionPresentation.Create("hidden-a", "Oculta A", true, null, null, visible: false),
                    SharedActionPresentation.Create("hidden-b", "Oculta B", false, "Motivo que não deve aparecer.", null, false),
                ]));

        Assert.DoesNotContain("data-dmo-action=", allHidden, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-summary-actions=\"true\"", allHidden, StringComparison.Ordinal);
        Assert.DoesNotContain("Motivo que não deve aparecer.", allHidden, StringComparison.Ordinal);
        Assert.DoesNotContain("Oculta A", allHidden, StringComparison.Ordinal);

        // A visible supplied action still renders, so the honouring is not a blanket suppression.
        var withVisible = await P2T03ComponentRenderer.RenderSummaryRowAsync(
            factory,
            P2T03Fixtures.Summary(
                CommonState.Ready,
                actions: [SharedActionPresentation.CreateEnabled("open", "Abrir ficha")]));

        Assert.Contains("data-dmo-action=\"open\"", withVisible, StringComparison.Ordinal);
    }

    private static string ReasonIdOf(string html, string actionKey)
    {
        var marker = "data-dmo-action=\"" + actionKey + "\"";
        var start = html.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Action '{actionKey}' must be rendered.");

        var described = html.IndexOf("aria-describedby=\"", start, StringComparison.Ordinal);
        Assert.True(described >= 0, $"Action '{actionKey}' must associate a reason.");

        var valueStart = described + "aria-describedby=\"".Length;
        var valueEnd = html.IndexOf('"', valueStart);

        return html[valueStart..valueEnd];
    }

    private static int Count(string value, string fragment) =>
        value.Split(fragment, StringSplitOptions.None).Length - 1;
}
