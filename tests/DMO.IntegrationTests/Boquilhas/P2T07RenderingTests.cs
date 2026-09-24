using System.Net;
using System.Text.RegularExpressions;
using DMO.Domain.Tools;
using DMO.IntegrationTests.JobOn;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DMO.IntegrationTests.Boquilhas;

/// <summary>
/// P2-T07 rendered-surface (<c>R</c>) proofs: the REAL Razor surfaces of the Boquilhas area,
/// rendered by the REAL host over the controlled store, at the canonical fixed-desktop
/// composition — rows V2, U3, N5, H3, T2 (rendered facet), L1/L2/L3 of contract §29.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §25 (regions + fixed desktop), §16/§17 (Tool orchestration +
/// movement vocabulary) and §24.3 (table interaction).
/// </remarks>
public sealed class P2T07RenderingTests
{
    private const string RegistoPath = "/boquilhas";
    private const string NovoPath = "/boquilhas/novo";
    private const string HistoricoPath = "/boquilhas/historico";

    private static async Task<(P2T07TestStore Store, Guid BoquilhasId)> SeedRegistoAsync(
        string token,
        bool closed = false)
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(ToolType.Bq, $"BQ-{token}", "L1", machines: "B1");
        var repairer = store.SeedRepairer("Reparador A");
        store.SeedAssignment("B1", repairer.RepairerId.Value);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        var created = await P2T07TestHost.SendJsonAsync(
            client, HttpMethod.Post, "/boquilhas/aggregates", P2T07TestHost.Json(new
            {
                toolId = tool.ToolId.Value,
                machines = new[] { "B1" },
                initialQuantity = 10,
                openingDate = "2026-09-10",
                utilisationPercent = 30m,
            }));

        var body = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(
            await created.Content.ReadAsStringAsync());
        var id = body.GetProperty("boquilhasId").GetGuid();

        if (closed)
        {
            await P2T07TestHost.SendJsonAsync(
                client, HttpMethod.Post, $"/boquilhas/aggregates/{id}/close", P2T07TestHost.Json(new
                {
                    expectedVersion = 1,
                }));
        }

        return (store, id);
    }

    private static WebApplicationFactory<Program> FactoryFor(P2T07TestStore store) =>
        P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);

    /// <summary>
    /// H3 (AC-H3) — the Registo page renders the lot grid with the accepted selection/open
    /// arbitration (single click selects, double click opens) and NO per-row action button grid:
    /// actions live outside the tables (DecisionBar/movement entry).
    /// </summary>
    [Fact]
    public async Task H3_RegistoRendersSelectionAndOpenWithoutPerRowActionGrids()
    {
        var (store, _) = await SeedRegistoAsync("H3A");

        using var factory = FactoryFor(store);
        using var client = factory.CreateClient();

        using var response = await P2T07TestHost.GetAsync(client, RegistoPath);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("data-dmo-dense-table=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-selection-enabled=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-open-enabled=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-open-route-map=\"true\"", html, StringComparison.Ordinal);

        // No per-row action button grid on any Boquilhas table.
        Assert.DoesNotContain("data-dmo-action-scope=\"row\"", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// V2 (AC-V2) — the movement selector exposes EXACTLY Início/Saída/Entrada/Irreparável and no
    /// <c>Editar</c> entry exists in it (it is an action, never a type).
    /// </summary>
    [Fact]
    public async Task V2_TheMovementSelectorExposesExactlyTheFourTypes()
    {
        var (store, id) = await SeedRegistoAsync("V2A");

        using var factory = FactoryFor(store);
        using var client = factory.CreateClient();

        using var response = await P2T07TestHost.GetAsync(client, $"{RegistoPath}?boquilhasId={id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        var selector = Regex.Match(html, @"data-dmo-movement-field=""type""[\s\S]*?</select>").Value;

        Assert.Contains("value=\"inicio\"", selector, StringComparison.Ordinal);
        Assert.Contains("value=\"saida\"", selector, StringComparison.Ordinal);
        Assert.Contains("value=\"entrada\"", selector, StringComparison.Ordinal);
        Assert.Contains("value=\"irreparavel\"", selector, StringComparison.Ordinal);
        Assert.DoesNotContain("Editar", selector, StringComparison.Ordinal);

        // No fifth type anywhere on the surface.
        Assert.DoesNotContain("value=\"contagem\"", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// U3 (AC-U3) — % utilização renders as a NUMBER, never a progress bar; a closed aggregate
    /// shows the snapshot still.
    /// </summary>
    [Fact]
    public async Task U3_UtilisationRendersAsANumberNeverAProgressBar()
    {
        var (store, id) = await SeedRegistoAsync("U3A", closed: true);

        using var factory = FactoryFor(store);
        using var client = factory.CreateClient();

        using var response = await P2T07TestHost.GetAsync(client, $"{RegistoPath}?boquilhasId={id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("data-dmo-utilisation=\"true\">30%</dd>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("progress", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// N5 (AC-N5) — no machine/reference sidebar simulating Job On state: the contextual panel of
    /// a STANDALONE aggregate renders only the registered machine set.
    /// </summary>
    [Fact]
    public async Task N5_TheContextualPanelRendersRealOrRegisteredFactsOnly()
    {
        var (store, standaloneId) = await SeedRegistoAsync("N5A");

        using var factory = FactoryFor(store);
        using var client = factory.CreateClient();

        using var response = await P2T07TestHost.GetAsync(client, $"{RegistoPath}?boquilhasId={standaloneId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var standaloneHtml = await response.Content.ReadAsStringAsync();

        Assert.Contains("data-dmo-panel-standalone=\"true\"", standaloneHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-sidebar", standaloneHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-simulated", standaloneHtml, StringComparison.Ordinal);
    }

    /// <summary>
    /// H3 (AC-H3 facet) — the Histórico page renders the history table + the outside-table detail
    /// actions and no per-row action grid.
    /// </summary>
    [Fact]
    public async Task H3_HistoricoRendersTheTableWithOutsideTableActions()
    {
        var (store, id) = await SeedRegistoAsync("H3B", closed: true);

        using var factory = FactoryFor(store);
        using var client = factory.CreateClient();

        using var response = await P2T07TestHost.GetAsync(client, $"{HistoricoPath}?boquilhasId={id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("data-dmo-dense-table=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-open-route-map=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-reopen-submit=\"true\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-action-scope=\"row\"", html, StringComparison.Ordinal);

        // The closed aggregate detail renders the immutable close snapshot facts.
        Assert.Contains("data-dmo-close-facts=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-closed-at=\"true\"", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// T2 (AC-T2 rendered facet) — the picker on the Novo page starts with ZERO candidates and the
    /// search/create affordances; nothing is ever auto-selected (a single candidate arriving later
    /// can only be selected by an EXPLICIT human activation — the node-harness scenarios prove it).
    /// </summary>
    [Fact]
    public async Task T2_TheNovoPickerNeverAutoSelects()
    {
        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted());
        using var client = factory.CreateClient();

        using var response = await P2T07TestHost.GetAsync(client, NovoPath);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("data-dmo-picker=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-search-input=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-create=\"boquilhas-create-tool\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-origin-token=\"boquilhas-novo\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-candidate=", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// L1/L2 (AC-L1/L2) — the P2-T07-owned assets contain no breakpoint-driven structure: no
    /// structural <c>@media</c>/<c>@container</c>/<c>@supports</c> rule, no width listener, no
    /// table→card conversion, no required-column hiding and no action relocation. Comments are
    /// stripped first: documentation of the absence is not the absence itself (the accepted scan
    /// discipline of the shared P2-T04 scan helper).
    /// </summary>
    [Fact]
    public void L1L2_NoBreakpointStructuralRuleOrWidthListenerExists()
    {
        var css = P2T04ProductionScan.WithoutCssComments(
            P2T04ProductionScan.Read("src/DMO.Web/wwwroot/css/dmo-boquilhas.css"));
        var js = P2T04ProductionScan.Read("src/DMO.Web/wwwroot/js/dmo-boquilhas.js");

        foreach (var token in new[] { "@media", "@container", "@supports", "resize", "innerWidth", "matchMedia" })
        {
            Assert.DoesNotContain(token, css, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(token, js, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// L3 (AC-L3) — the pages render inside the existing shared shell: the shared shell hooks
    /// (header/frame/brand) are present on every Boquilhas page.
    /// </summary>
    [Fact]
    public async Task L3_ThePagesRenderInsideTheSharedShell()
    {
        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted());
        using var client = factory.CreateClient();

        foreach (var path in new[] { RegistoPath, NovoPath, HistoricoPath })
        {
            using var response = await P2T07TestHost.GetAsync(client, path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("dmo-app-header", html, StringComparison.Ordinal);
            Assert.Contains("dmo-page-frame", html, StringComparison.Ordinal);
        }
    }
}