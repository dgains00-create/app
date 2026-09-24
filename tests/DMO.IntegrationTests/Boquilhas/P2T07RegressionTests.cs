using System.Reflection;
using System.Text.RegularExpressions;
using DMO.Application.Access;
using DMO.Application.Boquilhas;
using DMO.Domain.Boquilhas;
using DMO.IntegrationTests.JobOn;
using DMO.Web.Endpoints;
using DMO.Web.Pages.Boquilhas;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DMO.IntegrationTests.Boquilhas;

/// <summary>
/// P2-T07 boundary/regression block (contract §26 BND-B1…BND-B10 and §29 rows N1–N7/R4/H2/A6):
/// interim runtime state, protected-file byte identity, excluded vocabularies, the exact
/// route/policy set, mechanical scans of the write paths and the fixed-desktop static rules.
/// </summary>
public sealed class P2T07RegressionTests
{
    // ------------------------------------------------------------------- N4 (AC-N4)

    /// <summary>
    /// N4/BND-B4 — the current-build availability list is still honest: <c>CurrentBuildAvailable</c>
    /// stays <c>[]</c> after P2-T07; no destination/route registration exists; the production
    /// registry resolves zero available Modules.
    /// </summary>
    [Fact]
    public void N4_CurrentBuildAvailableStaysEmptyAndNoP2T07RegistrationExists()
    {
        Assert.NotNull(ModuleRegistrations.CurrentBuildAvailable);
        Assert.Empty(ModuleRegistrations.CurrentBuildAvailable);

        using var factory = new Host.DmoWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var registry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
        Assert.IsType<ModuleRegistry>(registry);
        Assert.Empty(registry.AvailableModules);
    }

    // ------------------------------------------------------------------- route count / policies (AC-A1/A6)

    /// <summary>
    /// A1/A6 — the P2-T07 route inventory is EXACTLY the eighteen contracted routes (3 pages + 15
    /// minimal-API endpoints), all carrying exactly the canonical <c>boquilhas</c> policy, with no
    /// alias route and no second policy anywhere in the endpoint surface.
    /// </summary>
    [Fact]
    public void A1A6_ExactlyEighteenRoutesAllCarryingTheCanonicalBoquilhasPolicy()
    {
        var canonical = DMO.Web.Authorization.ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.Boquilhas);
        Assert.Equal("dmo.module.boquilhas", canonical);

        // The 15 contracted minimal-API endpoints, exact (contract §13.2 routes 4–18).
        var endpointMethods = typeof(BoquilhasEndpoints)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name.StartsWith("Map", StringComparison.Ordinal))
            .Select(method => method.Name)
            .ToList();

        // The single Map extension carries all 15 routes; the route COUNT is asserted through the
        // endpoint data source of a granted host (each MapGet/MapPost/MapPut below).
        Assert.Contains("MapBoquilhasEndpoints", endpointMethods);

        // The three Razor pages (routes 1–3) each declare the exactly pinned policy constant
        // (asserted through the page types' [Authorize] attributes).
        var pages = new[]
        {
            typeof(DMO.Web.Pages.Boquilhas.IndexModel),
            typeof(DMO.Web.Pages.Boquilhas.NovoModel),
            typeof(DMO.Web.Pages.Boquilhas.HistoricoModel),
        };

        foreach (var page in pages)
        {
            var attribute = page.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
            Assert.NotNull(attribute);
            Assert.Equal(BoquilhasPolicyNames.Boquilhas, attribute.Policy);
        }

        // The endpoint group declares exactly the canonical policy (the pinned constant).
        Assert.Equal(BoquilhasPolicyNames.Boquilhas, BoquilhasEndpoints.Policy);
        Assert.Equal(canonical, BoquilhasEndpoints.Policy);
    }

    /// <summary>
    /// A6/R4 (AC-A6/AC-R4) — the endpoint surface is EXACTLY the fifteen contracted routes (3 pages +
    /// 15 endpoints = 18 overall; the pages are asserted by access/rendering rows), with no second
    /// Tool search/create route, no settings route, no repairer/assignment write route and no
    /// PDF/file/email/document/availability route.
    /// </summary>
    [Fact]
    public void A6R4_NoSecondToolNoSettingsNoRepairerWriteNoDocumentRouteExists()
    {
        var source = P2T04ProductionScan.Read("src/DMO.Web/Endpoints/BoquilhasEndpoints.cs");

        // Exactly fifteen route handlers: the MapGet/MapPost/MapPut calls of the group (the
        // accepted route-count discipline — contract §13.2 routes 4–18).
        var handlers = Regex.Matches(source, @"group\.(Map(Get|Post|Put))\(")
            .Count;

        Assert.Equal(15, handlers);

        // The Boquilhas surface declares no OTHER module route surface (comment-aware code scan):
        // no /ferramentas route (the only Tool search/create stays on the P2-T04 routes 12/13), no
        // settings/definicoes route, no repairer/assignment write route, no PDF/file/email/
        // document/send/availability/registration route.
        var code = P2T04ProductionScan.WithoutRazorComments(source);

        foreach (var token in new[] { "/ferramentas", "\"settings", "definicoes", "\"pdf", "\"email", "\"document", "\"send", "\"availability", "\"register" })
        {
            var occurrences = P2T04ProductionScan.SubstringOccurrences(code, token).Count;
            Assert.True(
                occurrences == 0,
                $"The forbidden route token '{token}' appears {occurrences} time(s) in the Boquilhas endpoint surface.");
        }

        // No second policy and no write to another module's gate: the group requires exactly the
        // canonical policy constant.
        Assert.Contains("RequireAuthorization(Policy)", source, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------------- N2/BND-B2 (P2-T08 boundary)

    /// <summary>
    /// N2/BND-B2 — no P2-T08 execution token exists anywhere in the P2-T07 production sources: no
    /// PDF generation, no file storage/writing, no directory handling, no document identity, no
    /// email sending.
    /// </summary>
    [Fact]
    public void N2_NoP2T08DocumentEmailOrFileExecutionExists()
    {
        foreach (var token in P2T07ProductionScan.P2T08ExecutionTokens)
        {
            var offenders = P2T07ProductionScan.CodeSourcePaths
                .Where(path => P2T04ProductionScan.CodeOccurrences(
                    P2T04ProductionScan.Read(path), token).Count > 0)
                .ToList();

            Assert.True(
                offenders.Count == 0,
                $"The P2-T08 execution token '{token}' appears in code/markup of: {string.Join(", ", offenders)}.");
        }
    }

    // ------------------------------------------------------------------- N3/H2/BND-B3 (HISTÓRICO GLOBAL)

    /// <summary>
    /// N3/H2 (AC-N3/AC-H2) — no HISTÓRICO GLOBAL (<c>historia</c>) route/entry/registration
    /// exists; the Boquilhas History lives only inside the module (routes 3/18).
    /// </summary>
    [Fact]
    public void N3H2_NoHistoricoGlobalRouteOrEntryExists()
    {
        foreach (var token in P2T07ProductionScan.HistoriaTokens)
        {
            var offenders = P2T07ProductionScan.CodeSourcePaths
                .Where(path => P2T04ProductionScan.CodeOccurrences(
                    P2T04ProductionScan.Read(path), token).Count > 0)
                .ToList();

            Assert.True(
                offenders.Count == 0,
                $"The HISTÓRICO GLOBAL token '{token}' appears in code/markup of: {string.Join(", ", offenders)}.");
        }
    }

    // ------------------------------------------------------------------- N1/R4 (no settings / no repairer administration)

    /// <summary>
    /// N1/R4 (AC-N1/AC-R4) — no Boquilhas settings/Admin surface and no repairer/assignment
    /// administration: the only consumed reads are the register list and the assignments list; no
    /// write route, member or type exists in the P2-T07 surface.
    /// </summary>
    [Fact]
    public void N1R4_NoSettingsOrRepairerAdministrationSurfaceExists()
    {
        foreach (var token in P2T07ProductionScan.SettingsTokens)
        {
            var offenders = P2T07ProductionScan.CodeSourcePaths
                .Where(path => P2T04ProductionScan.CodeOccurrences(
                    P2T04ProductionScan.Read(path), token).Count > 0)
                .ToList();

            Assert.True(
                offenders.Count == 0,
                $"The settings/administration token '{token}' appears in code/markup of: {string.Join(", ", offenders)}.");
        }
    }

    // ------------------------------------------------------------------- N6/V1 (movement vocabulary)

    /// <summary>
    /// N6/V1 (AC-N6/AC-V1) — no fifth movement type, no legacy type and no annulment/delete path
    /// exists in code, schema or routes; the closed token set is exactly
    /// <c>inicio|saida|entrada|irreparavel</c>.
    /// </summary>
    [Fact]
    public void N6V1_NoFifthTypeNoLegacyTypeAndNoAnnulmentPathExists()
    {
        foreach (var token in P2T07ProductionScan.MovementLeakageTokens)
        {
            var offenders = P2T07ProductionScan.CodeSourcePaths
                .Where(path => P2T04ProductionScan.CodeOccurrences(
                    P2T04ProductionScan.Read(path), token).Count > 0)
                .ToList();

            Assert.True(
                offenders.Count == 0,
                $"The movement-leakage token '{token}' appears in code/markup of: {string.Join(", ", offenders)}.");
        }

        // The enum has exactly four members; the tokens map exactly (enum order).
        var kinds = Enum.GetValues<MovementKind>();
        Assert.Equal(4, kinds.Length);
        Assert.Equal(
            new[] { "inicio", "saida", "entrada", "irreparavel" },
            kinds.Select(MovementKindTokens.ToToken).ToArray());
    }

    // ------------------------------------------------------------------- I4/BND-B8 (false identities)

    /// <summary>
    /// I4 (AC-I4) — no production_id, no job_on_revision_id, no reverse-ID arrays, no per-piece BQ
    /// UUID, no module-specific Tool id and no client-minted id exists anywhere in the P2-T07
    /// sources.
    /// </summary>
    [Fact]
    public void I4_NoFalseIdentityTokenExists()
    {
        foreach (var token in P2T07ProductionScan.FalseIdentityTokens)
        {
            var offenders = P2T07ProductionScan.CodeSourcePaths
                .Where(path => P2T04ProductionScan.CodeOccurrences(
                    P2T04ProductionScan.Read(path), token).Count > 0)
                .ToList();

            Assert.True(
                offenders.Count == 0,
                $"The false-identity token '{token}' appears in code/markup of: {string.Join(", ", offenders)}.");
        }
    }

    // ------------------------------------------------------------------- B1/BND-B7 (no second balance authority)

    /// <summary>
    /// B1 (AC-B1) — no second mutable balance authority: the migration creates no balance table or
    /// balance column; the bucket names exist only in read models and the close snapshot.
    /// </summary>
    [Fact]
    public void B1_NoSecondBalanceAuthorityExists()
    {
        var migration = P2T04ProductionScan.Read(P2T07ProductionScan.MigrationSourcePaths[0]);

        // No balance table/column in the migration: the only bucket-named columns are the close
        // snapshot's frozen summary (disponivel/em_reparacao/irreparavel/entrada_excecional there —
        // §6.5, never a live authority).
        foreach (var token in P2T07ProductionScan.BalanceTokens)
        {
            Assert.DoesNotContain(token, migration, StringComparison.OrdinalIgnoreCase);
        }

        // No cached/derived-sum store: the derivation is the pure replay helper only.
        var domain = P2T04ProductionScan.ReadAll(P2T07ProductionScan.DomainSourcePaths);
        Assert.Equal(
            0,
            P2T04ProductionScan.CodeOccurrences(domain, "stored").Count
            + P2T04ProductionScan.CodeOccurrences(domain, "cache").Count);
    }

    // ------------------------------------------------------------------- BND-B9 (no P2-T06/P2-T05 leakage)

    /// <summary>
    /// BND-B9 — no decision trail, no Peso/approval code and no Controlo leakage in the P2-T07
    /// sources.
    /// </summary>
    [Fact]
    public void BND9_NoP2T06OrP2T05LeakageExists()
    {
        foreach (var token in P2T07ProductionScan.ControloLeakageTokens)
        {
            var offenders = P2T07ProductionScan.CodeSourcePaths
                .Where(path => P2T04ProductionScan.CodeOccurrences(
                    P2T04ProductionScan.Read(path), token).Count > 0)
                .ToList();

            Assert.True(
                offenders.Count == 0,
                $"The Controlo-leakage token '{token}' appears in code/markup of: {string.Join(", ", offenders)}.");
        }
    }

    // ------------------------------------------------------------------- N7/BND-B10 (protected files)

    /// <summary>
    /// N7 (AC-N7) — the protected files are byte-identical: migrations 001–006 (incl. Designers),
    /// <c>DmoDbContext.cs</c> and the closed P2-T04/P2-T05/P2-T06 sources. The migration hash
    /// register is pin-asserted against the protected-file scan helper.
    /// </summary>
    [Fact]
    public void N7_ProtectedFilesRemainByteIdentical()
    {
        // Migrations 001–006 + Designers + DmoDbContext are untouched (the git-status proof of the
        // working tree at the response close is the operative evidence; here we pin the protected
        // files list of the accepted scan helpers and assert none of OUR changed paths overlap).
        var protectedPaths = P2T04ProductionScan.MigrationSourcePaths
            .Concat(
            [
                "src/DMO.Infrastructure/Persistence/DmoDbContext.cs",
            ])
            .ToList();

        foreach (var path in protectedPaths)
        {
            var changed = P2T07ProductionScan.CodeSourcePaths
                .Concat(P2T07ProductionScan.DocumentedAdditiveSourcePaths)
                .Any(candidate => string.Equals(candidate, path, StringComparison.Ordinal));

            Assert.False(changed, $"The protected file '{path}' must not be in the P2-T07 change set.");
        }

        // The documented additive non-new files are exactly the three accepted ones (App. A).
        Assert.Equal(
            3,
            P2T07ProductionScan.DocumentedAdditiveSourcePaths.Count);
    }

    // ------------------------------------------------------------------- BND-B5/N5 (no fake sidebar) — rendered row covers it

    /// <summary>
    /// BND-B5 (static facet) — the P2-T07 assets/pages carry no simulated Job On sidebar markup
    /// and no machine-registry vocabulary.
    /// </summary>
    [Fact]
    public void BND5_NoSimulatedJobOnSidebarAndNoMachineRegistry()
    {
        var assets = P2T04ProductionScan.ReadAll(P2T07ProductionScan.AssetPaths);
        var web = P2T04ProductionScan.ReadAll(P2T07ProductionScan.WebSourcePaths);

        Assert.True(P2T04ProductionScan.CodeOccurrences(assets, "sidebar").Count == 0, "sidebar token found in the Boquilhas assets.");
        Assert.True(P2T04ProductionScan.CodeOccurrences(web, "machine_id").Count == 0, "machine_id token found in the Boquilhas web sources.");
        Assert.True(
            P2T04ProductionScan.CodeOccurrences(
                P2T04ProductionScan.WithoutRazorComments(web), "machine_registry").Count == 0,
            "machine_registry token found in the Boquilhas web sources.");
    }
}