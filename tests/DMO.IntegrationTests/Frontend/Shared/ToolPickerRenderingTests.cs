using System.Text.RegularExpressions;
using DMO.IntegrationTests.Host;
using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.IntegrationTests.Frontend.Shared;

/// <summary>
/// P2-T03 (A5) integration — the real rendered picker surface.
/// Authority: <c>plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md</c> §3.1, §3.1.6,
/// §3.1.9, §5.1, §6.4 and the matrix rows RTP1–RTP9 (AC-1 … AC-15, AC-45, AC-48).
/// Purpose: prove in rendered markup that a single candidate is never marked selected, that every
/// supplied candidate is presented separately in supplied order with its own explicit select
/// control, that the create affordance exists only when the consumer supplies it visible and
/// enabled, that cancel/return is preserved in every presented state, that the controlled query and
/// the opaque origin token are rendered verbatim, and that the non-ready surfaces stay distinct.
/// Preconditions: the real compiled P2-T03 partials rendered through the test host view engine.
/// Required non-effects: no form target, no address and no canonical identity is emitted.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class ToolPickerRenderingTests
{
    private static readonly Regex FormTarget = new(@"[\s""']action\s*=|[\s""']method\s*=", RegexOptions.Singleline);

    [Fact]
    public async Task RTP1_SingleCandidate_IsNeverMarkedSelected()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderPickerAsync(
            factory,
            P2T03Fixtures.Picker(CommonState.Ready, [P2T03Fixtures.Candidate("cand-1")]));

        Assert.Equal(1, Count(html, "data-dmo-candidate=\"true\""));
        Assert.Contains("data-dmo-select-candidate=\"cand-1\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-selected=\"false\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("aria-selected=\"true\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-candidate-marker=\"true\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RTP2_EveryCandidateIsPresentedSeparately_InSuppliedOrder()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderPickerAsync(
            factory,
            P2T03Fixtures.Picker(
                CommonState.Ready,
                [
                    P2T03Fixtures.Candidate("cand-2"),
                    P2T03Fixtures.Candidate("cand-1"),
                    P2T03Fixtures.Candidate("cand-3"),
                ]));

        Assert.Equal(3, Count(html, "data-dmo-candidate=\"true\""));
        Assert.Equal(3, Count(html, "data-dmo-select-candidate="));

        var second = html.IndexOf("data-dmo-candidate-key=\"cand-2\"", StringComparison.Ordinal);
        var first = html.IndexOf("data-dmo-candidate-key=\"cand-1\"", StringComparison.Ordinal);
        var third = html.IndexOf("data-dmo-candidate-key=\"cand-3\"", StringComparison.Ordinal);

        Assert.True(second >= 0 && first > second && third > first, "Candidates must keep supplied order.");

        // Supplied facts are labelled and rendered verbatim, one entry per supplied fact.
        Assert.Contains("data-dmo-candidate-fact=\"Lote\"", html, StringComparison.Ordinal);
        Assert.Contains("lote-cand-1", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RTP3_CreateControlRenders_IfAndOnlyIfSuppliedVisibleAndEnabled()
    {
        using var factory = new DmoWebApplicationFactory();

        var enabled = await P2T03ComponentRenderer.RenderPickerAsync(
            factory,
            P2T03Fixtures.Picker(
                CommonState.Ready,
                [P2T03Fixtures.Candidate("cand-1")],
                createAction: SharedActionPresentation.CreateEnabled("create", "Criar registo")));

        Assert.Contains("data-dmo-create=\"create\"", enabled, StringComparison.Ordinal);
        Assert.Contains("Criar registo", enabled, StringComparison.Ordinal);

        var disabled = await P2T03ComponentRenderer.RenderPickerAsync(
            factory,
            P2T03Fixtures.Picker(
                CommonState.Ready,
                [P2T03Fixtures.Candidate("cand-1")],
                createAction: SharedActionPresentation.CreateDisabled("create", "Criar registo", "Sem permissão.")));

        Assert.DoesNotContain("data-dmo-create=", disabled, StringComparison.Ordinal);
        Assert.DoesNotContain("Sem permissão.", disabled, StringComparison.Ordinal);

        var hidden = await P2T03ComponentRenderer.RenderPickerAsync(
            factory,
            P2T03Fixtures.Picker(
                CommonState.Ready,
                [P2T03Fixtures.Candidate("cand-1")],
                createAction: SharedActionPresentation.Create("create", "Criar registo", true, null, null, visible: false)));

        Assert.DoesNotContain("data-dmo-create=", hidden, StringComparison.Ordinal);

        var absent = await P2T03ComponentRenderer.RenderPickerAsync(
            factory,
            P2T03Fixtures.Picker(CommonState.Ready, [P2T03Fixtures.Candidate("cand-1")]));

        Assert.DoesNotContain("data-dmo-create=", absent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RTP4_CancelIsPreservedInEveryPresentedState_AndProhibitedDataIsNotRendered()
    {
        using var factory = new DmoWebApplicationFactory();

        foreach (var state in new[]
                 {
                     CommonState.Ready, CommonState.Empty, CommonState.LookupFailed, CommonState.Stale,
                     CommonState.Conflict, CommonState.Loading, CommonState.Unavailable,
                     CommonState.PermissionDenied, CommonState.Saving, CommonState.Submitting,
                 })
        {
            var html = await P2T03ComponentRenderer.RenderPickerAsync(
                factory,
                P2T03Fixtures.Picker(
                    state,
                    [P2T03Fixtures.Candidate("cand-1")],
                    createAction: SharedActionPresentation.CreateEnabled("create", "Criar registo"),
                    retryAction: SharedActionPresentation.CreateEnabled("retry", "Tentar novamente")));

            Assert.Contains("data-dmo-cancel=", html, StringComparison.Ordinal);

            if (state is CommonState.Unavailable or CommonState.PermissionDenied)
            {
                Assert.DoesNotContain("data-dmo-candidate=", html, StringComparison.Ordinal);
                Assert.DoesNotContain("data-dmo-create=", html, StringComparison.Ordinal);
            }

            if (state is CommonState.LookupFailed or CommonState.Loading or CommonState.Saving or CommonState.Submitting)
            {
                Assert.DoesNotContain("data-dmo-create=", html, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public async Task RTP5_ControlledQueryRendersVerbatim_AndNoFormTargetOrAddressIsEmitted()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderPickerAsync(
            factory,
            P2T03Fixtures.Picker(
                CommonState.Ready,
                [P2T03Fixtures.Candidate("cand-1")],
                query: "lote 42 ção"));

        Assert.Contains("data-dmo-search-input=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"lote 42 ção\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-search-label=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-search-request=\"true\"", html, StringComparison.Ordinal);

        Assert.DoesNotContain("<form", html, StringComparison.Ordinal);
        Assert.DoesNotContain("href=", html, StringComparison.Ordinal);
        Assert.DoesNotMatch(FormTarget, html);
        Assert.DoesNotMatch(@"\blocation\s*\.", html);
        Assert.DoesNotMatch(@"\bwindow\s*\.\s*open", html);
    }

    [Fact]
    public async Task RTP6_OpaqueOriginTokenIsRenderedVerbatim()
    {
        using var factory = new DmoWebApplicationFactory();
        var token = "origem ção 42";

        var html = await P2T03ComponentRenderer.RenderPickerAsync(
            factory,
            P2T03Fixtures.Picker(
                CommonState.Ready,
                [P2T03Fixtures.Candidate("cand-1")],
                originToken: token,
                createAction: SharedActionPresentation.CreateEnabled("create", "Criar registo")));

        Assert.Contains($"data-dmo-origin-token=\"{token}\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("href=", html, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"\blocation\s*\.", html);
    }

    [Fact]
    public async Task RTP7_NonReadySurfacesStayDistinct_AndAFailedLookupOffersNoCreate()
    {
        using var factory = new DmoWebApplicationFactory();

        var empty = await P2T03ComponentRenderer.RenderPickerAsync(
            factory, P2T03Fixtures.Picker(CommonState.Empty));

        var lookupFailed = await P2T03ComponentRenderer.RenderPickerAsync(
            factory,
            P2T03Fixtures.Picker(
                CommonState.LookupFailed,
                retryAction: SharedActionPresentation.CreateEnabled("retry", "Tentar novamente")));

        var unavailable = await P2T03ComponentRenderer.RenderPickerAsync(
            factory, P2T03Fixtures.Picker(CommonState.Unavailable, reason: "Indisponível agora."));

        var denied = await P2T03ComponentRenderer.RenderPickerAsync(
            factory, P2T03Fixtures.Picker(CommonState.PermissionDenied, reason: "Acesso negado."));

        Assert.Contains("dmo-state--empty", empty, StringComparison.Ordinal);
        Assert.DoesNotContain("dmo-state--lookup-failed", empty, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-candidate=", empty, StringComparison.Ordinal);

        Assert.Contains("dmo-state--lookup-failed", lookupFailed, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"assertive\"", lookupFailed, StringComparison.Ordinal);
        Assert.Contains("data-dmo-action=\"retry\"", lookupFailed, StringComparison.Ordinal);
        Assert.DoesNotContain("dmo-state--empty", lookupFailed, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-create=", lookupFailed, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-candidate=", lookupFailed, StringComparison.Ordinal);

        Assert.Contains("dmo-state--unavailable", unavailable, StringComparison.Ordinal);
        Assert.Contains("Indisponível agora.", unavailable, StringComparison.Ordinal);
        Assert.Contains("dmo-state--permission-denied", denied, StringComparison.Ordinal);
        Assert.Contains("Acesso negado.", denied, StringComparison.Ordinal);
        Assert.NotEqual(unavailable, denied);
    }

    [Fact]
    public async Task RTP8_PickerCarriesTheDocumentedAdapterHooks_ForTheAcceptedFocusPath()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderPickerAsync(
            factory,
            P2T03Fixtures.Picker(
                CommonState.Ready,
                [P2T03Fixtures.Candidate("cand-1")],
                createAction: SharedActionPresentation.CreateEnabled("create", "Criar registo")));

        // The documented hooks the thin adapter reads. The invoking-control focus-return hook itself
        // belongs to the consumer's own invoking control (contract §5.1.4): the picker is the
        // surface being closed, not the control that opened it, so it must not claim that hook.
        Assert.Contains("data-dmo-picker=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-picker-state=\"ready\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-search-input=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-select-candidate=", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-create=", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-cancel=", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-focus-return", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RTP9_ControlledSelectedCandidate_IsProgrammaticAndNonColourOnly()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderPickerAsync(
            factory,
            P2T03Fixtures.Picker(
                CommonState.Ready,
                [P2T03Fixtures.Candidate("cand-1"), P2T03Fixtures.Candidate("cand-2")],
                selectedCandidateKey: "cand-2"));

        Assert.Contains("aria-selected=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-candidate-marker=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("dmo-picker__candidate--selected", html, StringComparison.Ordinal);
        Assert.Equal(1, Count(html, "aria-selected=\"true\""));
    }

    // Additive evidence for Architect observation O1 (contract §3.1.1 carrier, no new carrier).
    [Fact]
    public async Task O1_SuppliedExpectedTypeAndOriginContextRenderWithVisibleLabels()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderPickerAsync(
            factory,
            P2T03Fixtures.Picker(
                CommonState.Ready,
                [P2T03Fixtures.Candidate("cand-1")],
                expectedTypeLabel: "Tipo fornecido pelo consumidor"));

        Assert.Contains("data-dmo-picker-origin-context=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-picker-expected-type=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains(ToolPickerPresentation.GenericExpectedTypeQualifier, html, StringComparison.Ordinal);
        Assert.Contains("Tipo fornecido pelo consumidor", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-origin-fact=\"Origem\"", html, StringComparison.Ordinal);
        Assert.Contains("contexto de origem", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-fact-label=\"true\"", html, StringComparison.Ordinal);
    }

    // Additive evidence for Architect observation O2 (contract §3.1.9, supplied `ResultSummary`).
    [Fact]
    public async Task O2_SuppliedResultSummaryRendersVerbatimInsideAPoliteLiveRegion()
    {
        using var factory = new DmoWebApplicationFactory();

        var withoutSummary = await P2T03ComponentRenderer.RenderPickerAsync(
            factory, P2T03Fixtures.Picker(CommonState.Ready, [P2T03Fixtures.Candidate("cand-1")]));

        Assert.DoesNotContain("data-dmo-picker-summary=\"true\"", withoutSummary, StringComparison.Ordinal);

        var html = await P2T03ComponentRenderer.RenderPickerAsync(
            factory,
            P2T03Fixtures.Picker(
                CommonState.Ready,
                [P2T03Fixtures.Candidate("cand-1")],
                resultSummary: "Resumo de resultados fornecido."));

        Assert.Contains("data-dmo-picker-summary=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("role=\"status\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"polite\"", html, StringComparison.Ordinal);
        Assert.Contains("Resumo de resultados fornecido.", html, StringComparison.Ordinal);
    }

    private static int Count(string value, string fragment) =>
        value.Split(fragment, StringSplitOptions.None).Length - 1;
}
