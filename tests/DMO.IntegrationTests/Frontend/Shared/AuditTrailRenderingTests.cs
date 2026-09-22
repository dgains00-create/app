using System.Globalization;
using System.Text.RegularExpressions;
using DMO.IntegrationTests.Host;
using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.IntegrationTests.Frontend.Shared;

/// <summary>
/// P2-T02 (A4) integration — the real rendered <c>AuditTrail</c> surface.
/// Authority: <c>plans/contracts/P2_T02_DENSE_DATA_TABLE_AUDIT_TRAIL_CONTRACT.md</c> §6, §7.2,
/// §8.2, §9 and AC-18 to AC-26 (including the Q2 resolution that entry-level status is ABSENT).
/// Contract rows covered: R1, R10, R11, R12 plus the rendered halves of U14, U15, U17 and U19.
/// Purpose: prove the compiled partial renders supplied facts in the supplied order, never
/// synthesizes attribution, carries no entry-level status and exposes no mutation control.
/// Preconditions: the real compiled P2-T02 partials rendered through the test host view engine.
/// Required non-effects: no route is registered, no Module becomes available and the shared
/// shell/navigation composition is untouched.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class AuditTrailRenderingTests
{
    private static readonly Regex EntryRegex = new(
        "data-dmo-entry=\"(?<key>[^\"]+)\"",
        RegexOptions.None);

    [Fact]
    public async Task Rendering_EmptyAndLookupFailed_AreDistinctAndNeverAliased()
    {
        using var factory = new DmoWebApplicationFactory();

        var empty = await P2T02ComponentRenderer.RenderAuditTrailAsync(
            factory, AuditTrailPresentation.Empty("Histórico", "Sem histórico registado."));
        var lookupFailed = await P2T02ComponentRenderer.RenderAuditTrailAsync(
            factory,
            AuditTrailPresentation.LookupFailed(
                "Histórico", "A consulta falhou.",
                SharedActionPresentation.CreateEnabled("retry", "Tentar novamente")));

        Assert.Contains("dmo-state--empty", empty, StringComparison.Ordinal);
        Assert.Contains("Sem histórico registado.", empty, StringComparison.Ordinal);
        Assert.DoesNotContain("dmo-state--lookup-failed", empty, StringComparison.Ordinal);
        Assert.DoesNotContain("A consulta falhou.", empty, StringComparison.Ordinal);

        Assert.Contains("dmo-state--lookup-failed", lookupFailed, StringComparison.Ordinal);
        Assert.Contains("A consulta falhou.", lookupFailed, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"assertive\"", lookupFailed, StringComparison.Ordinal);
        Assert.Contains("data-dmo-action=\"retry\"", lookupFailed, StringComparison.Ordinal);
        Assert.DoesNotContain("dmo-state--empty", lookupFailed, StringComparison.Ordinal);

        Assert.NotEqual(empty, lookupFailed);
    }

    [Fact]
    public async Task Rendering_Entries_KeepExactlyTheSuppliedOrderInAPlainList()
    {
        using var factory = new DmoWebApplicationFactory();

        var newestFirst = new[]
        {
            AuditEntryPresentation.Create("e3", "Ação 3"),
            AuditEntryPresentation.Create("e2", "Ação 2"),
            AuditEntryPresentation.Create("e1", "Ação 1"),
        };
        var oldestFirst = newestFirst.Reverse().ToArray();

        var renderedNewestFirst = await P2T02ComponentRenderer.RenderAuditTrailAsync(
            factory, AuditTrailPresentation.Ready("Histórico", newestFirst));
        var renderedOldestFirst = await P2T02ComponentRenderer.RenderAuditTrailAsync(
            factory, AuditTrailPresentation.Ready("Histórico", oldestFirst));

        Assert.Equal(new[] { "e3", "e2", "e1" }, EntryKeys(renderedNewestFirst));
        Assert.Equal(new[] { "e1", "e2", "e3" }, EntryKeys(renderedOldestFirst));

        // No chronology claim is asserted by the component in either direction.
        Assert.DoesNotContain("mais recente", renderedNewestFirst, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mais antigo", renderedNewestFirst, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<ul class=\"dmo-audit__entries\"", renderedNewestFirst, StringComparison.Ordinal);
        Assert.DoesNotContain("<ol", renderedNewestFirst, StringComparison.Ordinal);
        Assert.DoesNotContain("role=\"timeline\"", renderedNewestFirst, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rendering_EntryFacts_RenderWithVisibleLabelsAndHooks()
    {
        using var factory = new DmoWebApplicationFactory();

        var semantic = new DateTimeOffset(2026, 3, 14, 9, 26, 53, TimeSpan.Zero);
        var entries = new[]
        {
            AuditEntryPresentation.Create(
                "e1",
                "Aprovou o registo",
                actor: "Ana Silva",
                timestampText: "14/03/2026 09:26",
                timestampValue: semantic,
                detail: "Aprovação registada no sistema",
                beforeDetail: "Pendente",
                afterDetail: "Aprovado",
                detailAction: SharedActionPresentation.CreateEnabled("detail", "Ver detalhe")),
        };

        var rendered = await P2T02ComponentRenderer.RenderAuditTrailAsync(
            factory, AuditTrailPresentation.Ready("Histórico", entries, contextText: "Página 1 de 3"));

        Assert.Contains("data-dmo-audit=\"true\"", rendered, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Histórico\"", rendered, StringComparison.Ordinal);
        Assert.Contains("data-dmo-audit-context=\"true\"", rendered, StringComparison.Ordinal);
        Assert.Contains("Página 1 de 3", rendered, StringComparison.Ordinal);

        Assert.Contains("data-dmo-entry=\"e1\"", rendered, StringComparison.Ordinal);
        Assert.Contains("data-dmo-audit-action=\"true\"", rendered, StringComparison.Ordinal);
        Assert.Contains("Aprovou o registo", rendered, StringComparison.Ordinal);

        // Actor and timestamp are labelled facts on the compact meta line.
        Assert.Contains(">Ator</span>", rendered, StringComparison.Ordinal);
        Assert.Contains("data-dmo-audit-actor=\"true\"", rendered, StringComparison.Ordinal);
        Assert.Contains("Ana Silva", rendered, StringComparison.Ordinal);
        Assert.Contains(">Data/hora</span>", rendered, StringComparison.Ordinal);
        Assert.Contains("14/03/2026 09:26", rendered, StringComparison.Ordinal);

        // The supplied display text is visible and the semantic value is the machine value only.
        Assert.Contains("data-dmo-audit-timestamp=\"true\"", rendered, StringComparison.Ordinal);
        Assert.Contains(
            $"datetime=\"{semantic.ToString("O", CultureInfo.InvariantCulture)}\"",
            rendered,
            StringComparison.Ordinal);

        Assert.Contains(">Detalhe</span>", rendered, StringComparison.Ordinal);
        Assert.Contains("data-dmo-audit-detail=\"true\"", rendered, StringComparison.Ordinal);
        Assert.Contains("Aprovação registada no sistema", rendered, StringComparison.Ordinal);
        Assert.Contains(">Antes</span>", rendered, StringComparison.Ordinal);
        Assert.Contains("data-dmo-audit-before=\"true\"", rendered, StringComparison.Ordinal);
        Assert.Contains(">Depois</span>", rendered, StringComparison.Ordinal);
        Assert.Contains("data-dmo-audit-after=\"true\"", rendered, StringComparison.Ordinal);

        // The optional generic detail action keeps its focus-return hook for the consumer surface.
        Assert.Contains("data-dmo-detail=\"e1\"", rendered, StringComparison.Ordinal);
        Assert.Contains("data-dmo-focus-return=\"true\"", rendered, StringComparison.Ordinal);
        Assert.Contains("Ver detalhe", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rendering_MissingActorOrTimestamp_ProducesNoAttributionText()
    {
        using var factory = new DmoWebApplicationFactory();

        var bare = await P2T02ComponentRenderer.RenderAuditTrailAsync(
            factory,
            AuditTrailPresentation.Ready("Histórico", [AuditEntryPresentation.Create("e1", "Ação registada")]));

        Assert.Contains("Ação registada", bare, StringComparison.Ordinal);
        Assert.DoesNotContain(">Ator</span>", bare, StringComparison.Ordinal);
        Assert.DoesNotContain(">Data/hora</span>", bare, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-audit-actor", bare, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-audit-timestamp", bare, StringComparison.Ordinal);
        Assert.DoesNotContain("dmo-audit__meta", bare, StringComparison.Ordinal);

        // The supplied explicit unavailable text is rendered only when the actor is absent.
        var noActor = await P2T02ComponentRenderer.RenderAuditTrailAsync(
            factory,
            AuditTrailPresentation.Ready(
                "Histórico",
                [AuditEntryPresentation.Create("e1", "Ação registada", actorUnavailableText: "Atribuição indisponível")]));

        Assert.Contains("Atribuição indisponível", noActor, StringComparison.Ordinal);

        var withActor = await P2T02ComponentRenderer.RenderAuditTrailAsync(
            factory,
            AuditTrailPresentation.Ready(
                "Histórico",
                [
                    AuditEntryPresentation.Create(
                        "e1", "Ação registada", actor: "Ana", actorUnavailableText: "Atribuição indisponível"),
                ]));

        Assert.Contains("Ana", withActor, StringComparison.Ordinal);
        Assert.DoesNotContain("Atribuição indisponível", withActor, StringComparison.Ordinal);

        // A supplied semantic value without display text is never turned into visible attribution.
        var machineOnly = await P2T02ComponentRenderer.RenderAuditTrailAsync(
            factory,
            AuditTrailPresentation.Ready(
                "Histórico",
                [
                    AuditEntryPresentation.Create(
                        "e1", "Ação registada", timestampValue: new DateTimeOffset(2026, 3, 14, 9, 26, 53, TimeSpan.Zero)),
                ]));

        Assert.DoesNotContain(">Data/hora</span>", machineOnly, StringComparison.Ordinal);
        Assert.DoesNotContain("datetime=", machineOnly, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rendering_UnavailableAndPermissionDenied_RenderNoPartialEntryFacts()
    {
        using var factory = new DmoWebApplicationFactory();

        var entries = new[]
        {
            AuditEntryPresentation.Create("e1", "Aprovou o registo", actor: "Ana Silva", detail: "Detalhe sensível"),
        };

        var unavailable = await P2T02ComponentRenderer.RenderAuditTrailAsync(
            factory,
            AuditTrailPresentation.Create(
                CommonState.Unavailable, "Histórico", entries,
                message: "Serviço indisponível.", reason: "Manutenção programada."));
        var denied = await P2T02ComponentRenderer.RenderAuditTrailAsync(
            factory,
            AuditTrailPresentation.Create(
                CommonState.PermissionDenied, "Histórico", entries,
                message: "Acesso negado.", reason: "Sem permissão de leitura."));

        foreach (var rendered in new[] { unavailable, denied })
        {
            Assert.Contains("dmo-state--", rendered, StringComparison.Ordinal);
            Assert.DoesNotContain("data-dmo-entry", rendered, StringComparison.Ordinal);
            Assert.DoesNotContain("dmo-audit__entries", rendered, StringComparison.Ordinal);
            Assert.DoesNotContain("dmo-audit__entry-action", rendered, StringComparison.Ordinal);
            Assert.DoesNotContain("Ana Silva", rendered, StringComparison.Ordinal);
            Assert.DoesNotContain("Aprovou o registo", rendered, StringComparison.Ordinal);
            Assert.DoesNotContain("Detalhe sensível", rendered, StringComparison.Ordinal);
        }

        Assert.Contains("dmo-state--unavailable", unavailable, StringComparison.Ordinal);
        Assert.Contains("dmo-state--permission-denied", denied, StringComparison.Ordinal);
        Assert.Contains("Manutenção programada.", unavailable, StringComparison.Ordinal);
        Assert.Contains("Sem permissão de leitura.", denied, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"h1-state-reason\"", unavailable, StringComparison.Ordinal);
        Assert.Contains("id=\"h1-state-reason\"", unavailable, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rendering_LoadingIsABusyLabelledHistoryRegion_AndStaleRetainsEntries()
    {
        using var factory = new DmoWebApplicationFactory();

        var entries = new[] { AuditEntryPresentation.Create("e1", "Ação registada", actor: "Ana") };

        var loading = await P2T02ComponentRenderer.RenderAuditTrailAsync(
            factory, AuditTrailPresentation.Loading("Histórico", "A carregar o histórico."));

        Assert.Contains("dmo-state--loading", loading, StringComparison.Ordinal);
        Assert.Contains("aria-busy=\"true\"", loading, StringComparison.Ordinal);
        Assert.Contains("A carregar o histórico.", loading, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-entry", loading, StringComparison.Ordinal);

        var stale = await P2T02ComponentRenderer.RenderAuditTrailAsync(
            factory,
            AuditTrailPresentation.Stale(
                "Histórico", "Informação desatualizada.", entries,
                [SharedActionPresentation.CreateEnabled("refresh", "Atualizar")]));

        Assert.Contains("dmo-state--stale", stale, StringComparison.Ordinal);
        Assert.Contains("Informação desatualizada.", stale, StringComparison.Ordinal);
        Assert.Contains("data-dmo-action=\"refresh\"", stale, StringComparison.Ordinal);
        Assert.Equal(new[] { "e1" }, EntryKeys(stale));
    }

    [Fact]
    public async Task Rendering_IsReadOnly_WithNoEditDeleteOrRestoreControl()
    {
        using var factory = new DmoWebApplicationFactory();

        var entries = new[]
        {
            AuditEntryPresentation.Create(
                "e1", "Aprovou o registo", actor: "Ana Silva",
                detailAction: SharedActionPresentation.CreateEnabled("detail", "Ver detalhe")),
            AuditEntryPresentation.Create("e2", "Criou o registo"),
        };

        var ready = await P2T02ComponentRenderer.RenderAuditTrailAsync(
            factory, AuditTrailPresentation.Ready("Histórico", entries));

        // The only interactive element per entry is the supplied generic detail action.
        Assert.Equal(1, Count(ready, "<button"));

        var states = new[]
        {
            ready,
            await P2T02ComponentRenderer.RenderAuditTrailAsync(
                factory, AuditTrailPresentation.Stale("Histórico", "Desatualizado.", entries)),
            await P2T02ComponentRenderer.RenderAuditTrailAsync(
                factory, AuditTrailPresentation.Empty("Histórico", "Sem histórico.")),
            await P2T02ComponentRenderer.RenderAuditTrailAsync(
                factory, AuditTrailPresentation.LookupFailed("Histórico", "Falhou.")),
        };

        foreach (var rendered in states)
        {
            Assert.DoesNotContain(">Editar<", rendered, StringComparison.Ordinal);
            Assert.DoesNotContain(">Eliminar<", rendered, StringComparison.Ordinal);
            Assert.DoesNotContain(">Restaurar<", rendered, StringComparison.Ordinal);
            Assert.DoesNotContain(">Corrigir<", rendered, StringComparison.Ordinal);
            Assert.DoesNotContain(">Anular<", rendered, StringComparison.Ordinal);
            Assert.DoesNotContain("data-dmo-edit", rendered, StringComparison.Ordinal);
            Assert.DoesNotContain("data-dmo-delete", rendered, StringComparison.Ordinal);
            Assert.DoesNotContain("data-dmo-restore", rendered, StringComparison.Ordinal);
            Assert.DoesNotContain("<form", rendered, StringComparison.Ordinal);
            Assert.DoesNotContain("href", rendered, StringComparison.Ordinal);
            Assert.DoesNotMatch(@"[\s""']action\s*=", rendered);
            Assert.DoesNotMatch(@"[\s""']method\s*=", rendered);
        }
    }

    [Fact]
    public async Task Rendering_EntryCarriesNoEntryLevelStatus()
    {
        using var factory = new DmoWebApplicationFactory();

        var entries = new[]
        {
            AuditEntryPresentation.Create(
                "e1", "Aprovou o registo", actor: "Ana Silva", timestampText: "14/03/2026 09:26",
                detail: "Detalhe", beforeDetail: "Pendente", afterDetail: "Aprovado"),
        };

        var rendered = await P2T02ComponentRenderer.RenderAuditTrailAsync(
            factory, AuditTrailPresentation.Ready("Histórico", entries));

        // Q2 resolved ABSENT: the AuditTrail carrier and its rendering carry no entry status.
        Assert.DoesNotContain("dmo-status", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-status", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("role=\"status\"", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rendering_EntryList_IsACompactFixedDesktopListWithNoCards()
    {
        using var factory = new DmoWebApplicationFactory();

        var entries = new[] { AuditEntryPresentation.Create("e1", "Aprovou o registo", actor: "Ana") };
        var rendered = await P2T02ComponentRenderer.RenderAuditTrailAsync(
            factory, AuditTrailPresentation.Ready("Histórico", entries));

        Assert.Contains("<ul class=\"dmo-audit__entries\"", rendered, StringComparison.Ordinal);
        Assert.Contains("<li class=\"dmo-audit__entry\"", rendered, StringComparison.Ordinal);
        Assert.Contains("dmo-audit__entry-action", rendered, StringComparison.Ordinal);

        // No oversized or decorative card structure, and no breakpoint-driven stacked variant.
        Assert.DoesNotContain("dmo-audit__card", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("dmo-audit__timeline", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("display:none", rendered, StringComparison.OrdinalIgnoreCase);

        // Entries are not in the tab order unless they contain a supplied action.
        Assert.DoesNotContain("tabindex", rendered, StringComparison.Ordinal);
    }

    private static int Count(string value, string fragment) =>
        value.Split(fragment, StringSplitOptions.None).Length - 1;

    private static IReadOnlyList<string> EntryKeys(string html) =>
        EntryRegex.Matches(html).Select(match => match.Groups["key"].Value).ToList();
}
