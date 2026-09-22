using System.Text.RegularExpressions;
using DMO.IntegrationTests.Host;
using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.IntegrationTests.Frontend.Shared;

/// <summary>
/// P2-T03 (A6) integration — the real rendered action region.
/// Authority: <c>plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md</c> §3.4, §3.4.3,
/// §3.4.4, §5.4, §6.4 and the matrix rows RDB1–RDB6 (AC-35 … AC-42, AC-45, AC-51).
/// Purpose: prove in rendered markup that the supplied action sequence and its supplied
/// classification are preserved exactly, that a risk-classified action carries visible textual
/// meaning in addition to styling, that a disabled action exposes its supplied associated reason,
/// that a pending action is presented busy with its supplied label while keeping its accessible
/// name, and that no address, form target or canonical identity is emitted.
/// Preconditions: the real compiled P2-T03 partials rendered through the test host view engine.
/// Required non-effects: no action is relocated, reordered, stacked or disabled by the region state.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class DecisionBarRenderingTests
{
    private static readonly Regex FormTarget = new(@"[\s""']action\s*=|[\s""']method\s*=", RegexOptions.Singleline);

    [Fact]
    public async Task RDB1_RenderedOrderAndGroupEqualityWithTheSuppliedSequence()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderDecisionBarAsync(
            factory,
            P2T03Fixtures.Bar(
                CommonState.Ready,
                [
                    P2T03Fixtures.Action("alpha", DecisionBarActionGroup.Primary),
                    P2T03Fixtures.Action("beta", DecisionBarActionGroup.Secondary),
                    P2T03Fixtures.Action("gamma", DecisionBarActionGroup.Primary),
                    P2T03Fixtures.Action("delta", DecisionBarActionGroup.Danger),
                ]));

        Assert.Equal(["alpha", "beta", "gamma", "delta"], ActionKeys(html));
        Assert.Contains("data-dmo-action-group=\"primary\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-action-group=\"secondary\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-action-group=\"danger\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Ação alpha\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RDB2_RiskClassifiedActionCarriesVisibleTextualMeaning()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderDecisionBarAsync(
            factory,
            P2T03Fixtures.Bar(CommonState.Ready, [P2T03Fixtures.Action("delta", DecisionBarActionGroup.Danger)]));

        Assert.Contains("data-dmo-risk-marker=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-group-qualifier=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains(DecisionBarPresentation.RiskGroupQualifier, html, StringComparison.Ordinal);

        var plain = await P2T03ComponentRenderer.RenderDecisionBarAsync(
            factory,
            P2T03Fixtures.Bar(CommonState.Ready, [P2T03Fixtures.Action("alpha", DecisionBarActionGroup.Primary)]));

        Assert.DoesNotContain("data-dmo-risk-marker=\"true\"", plain, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RDB3_DisabledActionExposesItsSuppliedAssociatedReason()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderDecisionBarAsync(
            factory,
            P2T03Fixtures.Bar(
                CommonState.Ready,
                [P2T03Fixtures.Action("beta", DecisionBarActionGroup.Secondary, enabled: false)]));

        Assert.Contains("disabled", html, StringComparison.Ordinal);
        Assert.Contains("aria-disabled=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"d1-action-beta-reason\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"d1-action-beta-reason\"", html, StringComparison.Ordinal);
        Assert.Contains("Motivo fornecido pelo consumidor.", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-action-reason=\"beta\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RDB4_PendingActionIsPresentedBusy_AndKeepsItsAccessibleName()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderDecisionBarAsync(
            factory,
            P2T03Fixtures.Bar(
                CommonState.Ready,
                [P2T03Fixtures.Action("alpha", DecisionBarActionGroup.Primary, pendingLabel: "Em curso…")],
                pendingActionKey: "alpha"));

        Assert.Contains("aria-busy=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-action-pending-text=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("Em curso…", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-action-pending=\"Em curso…\"", html, StringComparison.Ordinal);

        // The accessible name is the supplied label: it is never replaced by the pending label.
        Assert.Contains("aria-label=\"Ação alpha\"", html, StringComparison.Ordinal);
        Assert.Contains("disabled", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RDB5_RegionIsLabelled_WithOneControlPerSuppliedVisibleAction()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderDecisionBarAsync(
            factory,
            P2T03Fixtures.Bar(
                CommonState.Ready,
                [
                    P2T03Fixtures.Action("alpha", DecisionBarActionGroup.Primary),
                    DecisionBarActionPresentation.Create(
                        SharedActionPresentation.Create("hidden", "Oculta", true, null, null, visible: false),
                        DecisionBarActionGroup.Secondary),
                    P2T03Fixtures.Action("gamma", DecisionBarActionGroup.Primary),
                ],
                regionLabel: "Ações do registo"));

        Assert.Contains("role=\"group\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Ações do registo\"", html, StringComparison.Ordinal);
        Assert.Equal(["alpha", "gamma"], ActionKeys(html));

        // A supplied status/help text renders verbatim in the labelled region.
        var withStatus = await P2T03ComponentRenderer.RenderDecisionBarAsync(
            factory,
            P2T03Fixtures.Bar(
                CommonState.Ready,
                [P2T03Fixtures.Action("alpha", DecisionBarActionGroup.Primary)],
                statusText: "Texto de apoio fornecido."));

        Assert.Contains("data-dmo-decision-status=\"true\"", withStatus, StringComparison.Ordinal);
        Assert.Contains("Texto de apoio fornecido.", withStatus, StringComparison.Ordinal);

        var defaultLabel = await P2T03ComponentRenderer.RenderDecisionBarAsync(
            factory,
            P2T03Fixtures.Bar(CommonState.Ready, [P2T03Fixtures.Action("alpha", DecisionBarActionGroup.Primary)]));

        Assert.Contains($"aria-label=\"{DecisionBarPresentation.GenericRegionLabel}\"", defaultLabel, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RDB6_NoAddressFormTargetOrCanonicalIdentityIsEmitted()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderDecisionBarAsync(
            factory,
            P2T03Fixtures.Bar(
                CommonState.Ready,
                [P2T03Fixtures.Action("alpha", DecisionBarActionGroup.Primary)]));

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

    // Additive evidence for Architect observation O3 (contract §3.4.2 supplied status/help text).
    [Fact]
    public async Task O3_SuppliedStatusTextRendersVerbatimInTheLabelledRegion()
    {
        using var factory = new DmoWebApplicationFactory();

        var withoutStatus = await P2T03ComponentRenderer.RenderDecisionBarAsync(
            factory,
            P2T03Fixtures.Bar(CommonState.Ready, [P2T03Fixtures.Action("alpha", DecisionBarActionGroup.Primary)]));

        Assert.DoesNotContain("data-dmo-decision-status=\"true\"", withoutStatus, StringComparison.Ordinal);

        var html = await P2T03ComponentRenderer.RenderDecisionBarAsync(
            factory,
            P2T03Fixtures.Bar(
                CommonState.Ready,
                [P2T03Fixtures.Action("alpha", DecisionBarActionGroup.Primary)],
                statusText: "Texto de apoio fornecido pelo consumidor."));

        Assert.Contains("data-dmo-decision-status=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("Texto de apoio fornecido pelo consumidor.", html, StringComparison.Ordinal);
        Assert.Contains("role=\"group\"", html, StringComparison.Ordinal);
    }

    private static List<string> ActionKeys(string html)
    {
        var matches = Regex.Matches(html, "data-dmo-action-key=\"(?<key>[^\"]+)\"", RegexOptions.Singleline);

        return matches.Select(match => match.Groups["key"].Value).ToList();
    }
}
