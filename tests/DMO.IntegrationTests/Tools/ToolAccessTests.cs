using System.Net;
using DMO.Application.Access;
using DMO.IntegrationTests.Host;
using DMO.IntegrationTests.JobOn;

namespace DMO.IntegrationTests.Tools;

/// <summary>
/// P2-T04 access-class (<c>I</c>) proofs of the canonical Tool routes: the real host, the real
/// Module policies and the real <c>ModuleAuthorizationHandler</c> over the controlled access
/// outcome.
/// </summary>
/// <remarks>
/// <para>
/// Authority: P2-T04 contract §13.2 routes 12 and 13, §13.4 (policy ownership and the ADMIN
/// fail-closed rule), §14.3 ("authenticated but not granted, or module not available" ⇒ 403) and
/// §20.1 rows TOL21 and TOL22.
/// </para>
/// <para>
/// The authenticated test scheme marks the caller as authenticated, so a policy denial surfaces as
/// HTTP 403 rather than as a 401 challenge. The two rows prove the two denial families that the
/// contract fixes: a USER without the <c>ferramentas</c> grant ("permission-denied", never an empty
/// list) and an ADMIN session ("ADMIN gains no operational access", even when the Module appears in
/// the effective-access outcome).
/// </para>
/// </remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class ToolAccessTests
{
    private const string ToolsPath = "/ferramentas/tools";

    /// <summary>A bounded search with a valid criterion: it would answer 200 for a permitted caller.</summary>
    private const string SearchPath = ToolsPath + "?reference=5447T173&limit=10";

    /// <summary>The reference → productions route of the Job On consult surface.</summary>
    private const string ProductionsPath = "/jobon/productions?reference=5447T173";

    /// <summary>A valid canonical Tool create body, so the POST is refused by the policy and not by validation.</summary>
    private static readonly string ToolBody = P2T04TestHost.Json(new
    {
        type = "CM",
        reference = "5447T173",
        lot = "12",
        processo = "NNPB",
        quantity = 3,
        machines = new[] { "B1", "C2" },
    });

    /// <summary>
    /// TOL21 (contract §20.1) — proves AC-86 and AC-87: a caller without the <c>ferramentas</c>
    /// grant receives 403 on <c>GET /ferramentas/tools</c> and on <c>POST /ferramentas/tools</c>,
    /// never a 200 and never an empty list body, and the refused create writes nothing.
    /// </summary>
    [Fact]
    public async Task TOL21_CallerWithoutFerramentasIsDeniedAndNeverShownAnEmptyToolList()
    {
        var store = new P2T04TestStore();

        // Exactly one Module is granted — Job On View. Ferramentas is deliberately NOT granted.
        IReadOnlyList<ModuleDefinition> grantedWithoutFerramentas =
        [
            P2T04TestHost.Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On"),
        ];

        using var factory = P2T04TestHost.ForUser(grantedWithoutFerramentas, store);
        using var client = factory.CreateClient();

        var search = await P2T04TestHost.GetAsync(client, SearchPath);
        Assert.Equal(HttpStatusCode.Forbidden, search.StatusCode);

        // "permission-denied ≠ empty": the denial is never rendered as an empty result list.
        var searchBody = await search.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"items\"", searchBody, StringComparison.Ordinal);

        var create = await P2T04TestHost.SendJsonAsync(client, HttpMethod.Post, ToolsPath, ToolBody);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        var createBody = await create.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"toolId\"", createBody, StringComparison.Ordinal);

        // The denied create reached no write: no canonical Tool was persisted.
        Assert.Equal(0, store.ToolCount);
    }

    /// <summary>
    /// TOL22 (contract §20.1) — proves AC-88: an ADMIN session receives 403 on
    /// <c>GET /ferramentas/tools</c>, <c>POST /ferramentas/tools</c> and
    /// <c>GET /jobon/productions</c>, gaining no operational access and writing nothing, even though
    /// every P2-T04 Module is present in the effective-access outcome of this host.
    /// </summary>
    [Fact]
    public async Task TOL22_AdminSessionIsDeniedOnEveryOperationalToolAndJobOnRoute()
    {
        var store = new P2T04TestStore();

        using var factory = P2T04TestHost.ForAdmin(P2T04TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        var search = await P2T04TestHost.GetAsync(client, SearchPath);
        Assert.Equal(HttpStatusCode.Forbidden, search.StatusCode);

        var create = await P2T04TestHost.SendJsonAsync(client, HttpMethod.Post, ToolsPath, ToolBody);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        var productions = await P2T04TestHost.GetAsync(client, ProductionsPath);
        Assert.Equal(HttpStatusCode.Forbidden, productions.StatusCode);

        // A denial is never an empty/successful payload.
        Assert.DoesNotContain("\"items\"", await search.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.DoesNotContain("\"productions\"", await productions.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        Assert.Equal(0, store.ToolCount);
        Assert.Equal(0, store.JobOnCount);
    }
}
