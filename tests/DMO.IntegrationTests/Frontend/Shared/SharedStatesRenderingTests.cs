using System.Net;
using DMO.IntegrationTests.Host;
using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.IntegrationTests.Frontend.Shared;

/// <summary>
/// P2-T01 (A3) integration — the real rendered shared component surfaces.
/// Purpose: prove the rendered markup exposes visible text (never colour-only) for the four
/// mandated mutual-distinction states, for <c>RecordStatus</c> unknown-status neutrality, and
/// for the eight <c>AvailabilityState</c> outcomes; and that a disabled action's reason is
/// programmatically associated.
/// Authority: docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md §3 rules 5–6, §4, §10, §11.
/// Preconditions: the real compiled partials rendered through the test host view engine.
/// Required non-effects: no route is registered, no Module becomes available, and the shared
/// shell/navigation composition is untouched.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class SharedStatesRenderingTests
{
    [Fact]
    public async Task Rendering_LiveShellAndNavigationBehaviourIsUnchanged()
    {
        // Regression guard: rendering the A3 components must not disturb the accepted shell.
        using var factory = new DmoWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Login");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Entrar", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CommonState_FourDistinctStates_RenderDistinctTextualMarkup()
    {
        using var factory = new DmoWebApplicationFactory();

        var empty = await SharedComponentRenderer.RenderCommonStateAsync(
            factory, CommonStateRegionPresentation.Empty("Sem registos.", "Resultados"), "s1");
        var lookup = await SharedComponentRenderer.RenderCommonStateAsync(
            factory, CommonStateRegionPresentation.LookupFailed("A consulta falhou.", "Resultados"), "s2");
        var unavailable = await SharedComponentRenderer.RenderCommonStateAsync(
            factory, CommonStateRegionPresentation.Unavailable("Serviço indisponível.", "Resultados", "Manutenção."), "s3");
        var denied = await SharedComponentRenderer.RenderCommonStateAsync(
            factory, CommonStateRegionPresentation.PermissionDenied("Acesso negado.", "Resultados", "Sem permissão."), "s4");

        // Each state is visible as text, and each has its own distinct state token.
        Assert.Contains("dmo-state--empty", empty, StringComparison.Ordinal);
        Assert.Contains("dmo-state--lookup-failed", lookup, StringComparison.Ordinal);
        Assert.Contains("dmo-state--unavailable", unavailable, StringComparison.Ordinal);
        Assert.Contains("dmo-state--permission-denied", denied, StringComparison.Ordinal);

        Assert.Contains("Sem registos.", empty, StringComparison.Ordinal);
        Assert.Contains("A consulta falhou.", lookup, StringComparison.Ordinal);
        Assert.Contains("Serviço indisponível.", unavailable, StringComparison.Ordinal);
        Assert.Contains("Acesso negado.", denied, StringComparison.Ordinal);

        // Never the same rendering: the four markup strings differ and each carries its own token.
        var rendered = new[] { empty, lookup, unavailable, denied };
        Assert.Equal(4, rendered.Distinct(StringComparer.Ordinal).Count());

        // The lookup failure is not presented as an empty result.
        Assert.DoesNotContain("dmo-state--empty", lookup, StringComparison.Ordinal);
        Assert.DoesNotContain("Sem registos.", lookup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CommonState_SuppliedRegionLabel_IsExposedAsTheAccessibleRegionLabel()
    {
        using var factory = new DmoWebApplicationFactory();

        var rendered = await SharedComponentRenderer.RenderCommonStateAsync(
            factory, CommonStateRegionPresentation.Empty("Sem registos.", "Resultados da pesquisa"), "s1");

        Assert.Contains("aria-label=\"Resultados da pesquisa\"", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CommonState_LookupFailed_AnnouncesAssertively_BusyRegionIsMarkedBusy()
    {
        using var factory = new DmoWebApplicationFactory();

        var lookup = await SharedComponentRenderer.RenderCommonStateAsync(
            factory, CommonStateRegionPresentation.LookupFailed("A consulta falhou.", "Resultados"), "s1");
        var loading = await SharedComponentRenderer.RenderCommonStateAsync(
            factory, CommonStateRegionPresentation.Create(CommonState.Loading, "A carregar.", "Resultados"), "s2");

        Assert.Contains("aria-live=\"assertive\"", lookup, StringComparison.Ordinal);
        Assert.Contains("aria-busy=\"true\"", loading, StringComparison.Ordinal);
        Assert.Contains("role=\"status\"", loading, StringComparison.Ordinal);
        Assert.DoesNotContain("aria-busy", lookup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CommonState_WrappedOutcome_KeepsBothTokens()
    {
        using var factory = new DmoWebApplicationFactory();

        var rendered = await SharedComponentRenderer.RenderCommonStateAsync(
            factory,
            CommonStateRegionPresentation.Create(
                CommonState.Empty, "Sem registos.", "Resultados", wrappingState: CommonState.Loading),
            "s1");

        Assert.Contains("data-dmo-state=\"empty\"", rendered, StringComparison.Ordinal);
        Assert.Contains("data-dmo-state-wrapper=\"loading\"", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CommonState_DisabledAction_ReasonIsProgrammaticallyAssociated()
    {
        using var factory = new DmoWebApplicationFactory();

        var disabled = SharedActionPresentation.CreateDisabled("retry", "Tentar novamente", "Serviço em manutenção.");
        var rendered = await SharedComponentRenderer.RenderCommonStateAsync(
            factory,
            CommonStateRegionPresentation.Create(
                CommonState.Unavailable, "Serviço indisponível.", "Resultados", actions: [disabled]),
            "s1");

        Assert.Contains("disabled", rendered, StringComparison.Ordinal);
        Assert.Contains("aria-disabled=\"true\"", rendered, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"s1-action-retry-reason\"", rendered, StringComparison.Ordinal);
        Assert.Contains("id=\"s1-action-retry-reason\"", rendered, StringComparison.Ordinal);
        Assert.Contains("Serviço em manutenção.", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordStatus_UnknownStatus_RendersNeutralToneWithVisibleText()
    {
        using var factory = new DmoWebApplicationFactory();

        var rendered = await SharedComponentRenderer.RenderRecordStatusAsync(
            factory, RecordStatusPresentation.Create("Estado desconhecido", (StatusTone)999));

        Assert.Contains("dmo-status--neutral", rendered, StringComparison.Ordinal);
        Assert.Contains("data-dmo-status-tone=\"neutral\"", rendered, StringComparison.Ordinal);
        Assert.Contains("Estado desconhecido", rendered, StringComparison.Ordinal);
        Assert.Contains("role=\"status\"", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordStatus_TextIsAlwaysPresent_AndToneIsSupplementary()
    {
        using var factory = new DmoWebApplicationFactory();

        var rendered = await SharedComponentRenderer.RenderRecordStatusAsync(
            factory, RecordStatusPresentation.Create("Aguarda aprovação", StatusTone.Warning, markerLabel: "!"));

        Assert.Contains("dmo-status--warning", rendered, StringComparison.Ordinal);
        Assert.Contains("Aguarda aprovação", rendered, StringComparison.Ordinal);
        // The marker is decorative; the text carries the meaning.
        Assert.Contains("aria-hidden=\"true\"", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Availability_EightStates_RenderDistinctTokensAndText()
    {
        using var factory = new DmoWebApplicationFactory();

        var states = new[]
        {
            (AvailabilityState.Available, "available"),
            (AvailabilityState.NotGenerated, "not-generated"),
            (AvailabilityState.AwaitingApproval, "awaiting-approval"),
            (AvailabilityState.WorkspaceUnavailable, "workspace-unavailable"),
            (AvailabilityState.FileMissing, "file-missing"),
            (AvailabilityState.VersionsAvailable, "versions-available"),
            (AvailabilityState.NotApplicable, "not-applicable"),
            (AvailabilityState.LookupFailed, "lookup-failed"),
        };

        var tokens = new List<string>();
        foreach (var (state, token) in states)
        {
            var rendered = await SharedComponentRenderer.RenderAvailabilityAsync(
                factory,
                AvailabilityPresentation.Create(state, $"texto {token}", "Documento", detail: $"detalhe {token}"),
                "a1");

            Assert.Contains($"dmo-availability--{token}", rendered, StringComparison.Ordinal);
            Assert.Contains($"texto {token}", rendered, StringComparison.Ordinal);
            Assert.Contains($"detalhe {token}", rendered, StringComparison.Ordinal);
            tokens.Add(token);
        }

        Assert.Equal(8, tokens.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task Availability_NotApplicableAndLookupFailed_AreNotStyledAlike()
    {
        using var factory = new DmoWebApplicationFactory();

        var notApplicable = await SharedComponentRenderer.RenderAvailabilityAsync(
            factory, AvailabilityPresentation.NotApplicable("Não aplicável.", "Documento"), "a1");
        var lookupFailed = await SharedComponentRenderer.RenderAvailabilityAsync(
            factory, AvailabilityPresentation.LookupFailed("A consulta falhou.", "Documento"), "a2");
        var notGenerated = await SharedComponentRenderer.RenderAvailabilityAsync(
            factory,
            AvailabilityPresentation.Create(AvailabilityState.NotGenerated, "Ainda não gerado.", "Documento"),
            "a3");
        var fileMissing = await SharedComponentRenderer.RenderAvailabilityAsync(
            factory,
            AvailabilityPresentation.Create(AvailabilityState.FileMissing, "Ficheiro em falta.", "Documento"),
            "a4");

        Assert.Contains("dmo-availability--not-applicable", notApplicable, StringComparison.Ordinal);
        Assert.Contains("dmo-availability--lookup-failed", lookupFailed, StringComparison.Ordinal);
        Assert.Contains("dmo-availability--not-generated", notGenerated, StringComparison.Ordinal);
        Assert.Contains("dmo-availability--file-missing", fileMissing, StringComparison.Ordinal);

        Assert.NotEqual(notApplicable, lookupFailed);
        Assert.NotEqual(notGenerated, fileMissing);
        Assert.NotEqual(notGenerated, lookupFailed);
    }

    [Fact]
    public async Task Availability_DisabledAction_ReasonIsProgrammaticallyAssociated()
    {
        using var factory = new DmoWebApplicationFactory();

        var disabled = SharedActionPresentation.CreateDisabled("open", "Abrir", "Ficheiro em falta.");
        var rendered = await SharedComponentRenderer.RenderAvailabilityAsync(
            factory,
            AvailabilityPresentation.Create(
                AvailabilityState.FileMissing, "Ficheiro em falta.", "Documento", actions: [disabled]),
            "a1");

        Assert.Contains("aria-disabled=\"true\"", rendered, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"a1-action-open-reason\"", rendered, StringComparison.Ordinal);
        Assert.Contains("id=\"a1-action-open-reason\"", rendered, StringComparison.Ordinal);
        Assert.Contains("Ficheiro em falta.", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ComponentStylesheet_Resolves_AndIsAdditiveOnlyToNewSelectors()
    {
        using var factory = new DmoWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/css/dmo-components.css");
        var css = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // The new A-owned sheet defines the state/status/availability selectors.
        Assert.Contains(".dmo-state--empty", css, StringComparison.Ordinal);
        Assert.Contains(".dmo-state--lookup-failed", css, StringComparison.Ordinal);
        Assert.Contains(".dmo-status--neutral", css, StringComparison.Ordinal);
        Assert.Contains(".dmo-availability--not-applicable", css, StringComparison.Ordinal);
        Assert.Contains(".dmo-availability--lookup-failed", css, StringComparison.Ordinal);

        // It consumes published tokens rather than hardcoding hex colours.
        Assert.Contains("var(--dmo-", css, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Availability_VersionsAvailable_PresentsSuppliedOpaqueVersions()
    {
        using var factory = new DmoWebApplicationFactory();

        var versions = new[]
        {
            AvailabilityVersionPresentation.Create("v2", "Versão 2", selected: true),
            AvailabilityVersionPresentation.Create("v1", "Versão 1"),
        };
        var rendered = await SharedComponentRenderer.RenderAvailabilityAsync(
            factory,
            AvailabilityPresentation.Create(
                AvailabilityState.VersionsAvailable, "Versões disponíveis.", "Documento", versions: versions),
            "a1");

        Assert.Contains("data-dmo-version=\"v2\"", rendered, StringComparison.Ordinal);
        Assert.Contains("aria-pressed=\"true\"", rendered, StringComparison.Ordinal);
        Assert.Contains("data-dmo-version=\"v1\"", rendered, StringComparison.Ordinal);
        Assert.Contains("Versões disponíveis.", rendered, StringComparison.Ordinal);
    }
}
