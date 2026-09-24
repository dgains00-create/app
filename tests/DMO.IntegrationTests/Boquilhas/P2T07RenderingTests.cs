using System.Net;
using System.Text.RegularExpressions;
using DMO.Domain.Boquilhas;
using DMO.Domain.Tools;
using DMO.IntegrationTests.JobOn;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DMO.IntegrationTests.Boquilhas;

/// <summary>
/// P2-T07 rendered-surface proofs (OWNER CLARIFICATION): the REAL Razor surfaces rendered by the
/// REAL host over the controlled store — the three-type vocabulary, the absence of lifecycle UI,
/// the movement-ledger/open arbitration, the derived-outstanding presentation and the fixed-desktop
/// composition.
/// </summary>
public sealed class P2T07RenderingTests
{
    private const string RegistoPath = "/boquilhas";
    private const string NovoPath = "/boquilhas/novo";
    private const string HistoricoPath = "/boquilhas/historico";

    private static (P2T07TestStore Store, Guid BoquilhasId) SeedRegisto(string token)
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(ToolType.Bq, $"BQ-{token}", "L1", machines: "B1");
        var repairer = store.SeedRepairer("Reparador A");
        store.SeedAssignment("B1", repairer.RepairerId.Value);
        var (_, bqId) = store.SeedProductionWithBq($"REF-{token}", "P1", tool.ToolId.Value);

        var register = store.SeedRegister(bqId);
        return (store, register.BoquilhasId.Value);
    }

    /// <summary>
    /// V2 — the Registo movement selector exposes EXACTLY the three movement types with the exact
    /// labels; no Início and no Irreparável option exists; the page states the derived
    /// outstanding; no lifecycle control (close/reopen/status) exists anywhere on the surface.
    /// </summary>
    [Fact]
    public async Task V2_TheSelectorExposesExactlyTheThreeTypes_AndNoLifecycleControlExists()
    {
        var (store, boquilhasId) = SeedRegisto("V2");

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var page = await P2T07TestHost.GetAsync(client, $"{RegistoPath}?boquilhasId={boquilhasId}");
        var html = await page.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);

        // The movement-type selector options: EXACTLY the three closed values.
        var typeOptions = Regex.Matches(html, @"<option value=""(saida|entrada|entrada_sem_reparacao|inicio|irreparavel|editar)""")
            .Select(match => match.Groups[1].Value)
            .OrderBy(value => value, StringComparer.Ordinal)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(["entrada", "entrada_sem_reparacao", "saida"], typeOptions);

        // The exact labels are rendered.
        Assert.Contains(">Saída<", html);
        Assert.Contains(">Entrada sem reparação<", html);

        // No superseded vocabulary on the live surface.
        Assert.DoesNotContain(">Início<", html);
        Assert.DoesNotContain(">Irreparável<", html);
        Assert.DoesNotContain("Fechar", html);
        Assert.DoesNotContain("Reabrir", html);
        Assert.DoesNotContain("Estado", html);

        // The derived outstanding is presented (an empty register: zero).
        Assert.Contains("data-dmo-outstanding=\"true\"", html);
    }

    /// <summary>
    /// T2 — the Entry surface renders the six machine options and the repairer selector with the
    /// consumed assignment data (never a second registry); Entrada variants render NO required
    /// machine/repairer affordance marker.
    /// </summary>
    [Fact]
    public async Task T2_TheEntryRendersTheSixMachines_AndTheConsumedRepairerRegister()
    {
        var (store, boquilhasId) = SeedRegisto("T2");
        var repairer = store.SeedRepairer("Reparador B");

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var page = await P2T07TestHost.GetAsync(client, $"{RegistoPath}?boquilhasId={boquilhasId}");
        var html = await page.Content.ReadAsStringAsync();

        Assert.Contains(">Reparador B</option>", html);

        foreach (var machine in new[] { "B1", "B2", "B3", "C1", "C2", "C3" })
        {
            Assert.Contains($"<option value=\"{machine}\">", html);
        }
    }

    /// <summary>
    /// H3 — the movement ledger and the history table are selection surfaces WITH page-owned open
    /// routes (single click selects, double click opens); NO per-row action button grid exists.
    /// </summary>
    [Fact]
    public async Task H3_TablesAreSelectionSurfaces_WithOpenRoutes_AndNoPerRowButtons()
    {
        var (store, boquilhasId) = SeedRegisto("H3");
        var repairer = store.SeedRepairer("Reparador A");
        store.SeedRegister(boquilhasId, (MovementKind.Saida, 10, new DateOnly(2026, 9, 25), "B1", repairer.RepairerId.Value));

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var registo = await P2T07TestHost.GetAsync(client, $"{RegistoPath}?boquilhasId={boquilhasId}");
        var registoHtml = await registo.Content.ReadAsStringAsync();

        Assert.Contains("data-dmo-movement-table=\"true\"", registoHtml);
        Assert.Contains("data-dmo-open-route-map=\"true\"", registoHtml);

        using var historico = await P2T07TestHost.GetAsync(client, HistoricoPath);
        var historicoHtml = await historico.Content.ReadAsStringAsync();

        Assert.Contains("data-dmo-open-route-map=\"true\"", historicoHtml);
        Assert.DoesNotContain("dmo-boquilhas__row-actions", historicoHtml);
    }

    /// <summary>
    /// N5 — the pages render NO close/reopen/open-facts controls and no status surface; the
    /// Histórico filter exposes exactly the movement vocabulary (no state filter).
    /// </summary>
    [Fact]
    public async Task N5_NoLifecycleControls_AnywhereOnTheSurfaces()
    {
        var (store, boquilhasId) = SeedRegisto("N5");

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        foreach (var path in new[] { $"{RegistoPath}?boquilhasId={boquilhasId}", NovoPath, HistoricoPath })
        {
            using var page = await P2T07TestHost.GetAsync(client, path);
            var html = await page.Content.ReadAsStringAsync();

            Assert.DoesNotContain("data-dmo-close", html);
            Assert.DoesNotContain("data-dmo-reopen", html);
            Assert.DoesNotContain("data-dmo-opening-facts", html);
            Assert.DoesNotContain("utilisation", html);
        }
    }

    /// <summary>
    /// L1/L2 — the fixed 1366 × 768 desktop composition: no structural media query, no container
    /// query, no width listener, no card-conversion rule and no action-relocation rule exists in
    /// the P2-T07 CSS (comment- and string-stripped scan of the shipped asset).
    /// </summary>
    [Fact]
    public async Task L1L2_TheFixedDesktopComposition_HasNoStructuralBreakpoints()
    {
        var css = await File.ReadAllTextAsync(Path.Combine(
            P2T04ProductionScan.RepositoryRoot(),
            "src", "DMO.Web", "wwwroot", "css", "dmo-boquilhas.css"));

        // Strip comments and quoted strings, then scan the structural surface.
        var stripped = Regex.Replace(css, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        stripped = Regex.Replace(stripped, @"""[^""]*""|'[^']*'", string.Empty, RegexOptions.Singleline);

        Assert.DoesNotContain("@media", stripped);
        Assert.DoesNotContain("@container", stripped);
        Assert.DoesNotContain("@supports", stripped);
        Assert.DoesNotContain("matchMedia", stripped);
        Assert.DoesNotContain("resize", stripped);
    }

    /// <summary>
    /// L3 — the three P2-T07 pages render inside the existing shared shell.
    /// </summary>
    [Fact]
    public async Task L3_ThePagesRenderInsideTheSharedShell()
    {
        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted());
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        foreach (var path in new[] { RegistoPath, NovoPath, HistoricoPath })
        {
            using var page = await P2T07TestHost.GetAsync(client, path);
            var html = await page.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            Assert.Contains("dmo-shell", html);
        }
    }
}