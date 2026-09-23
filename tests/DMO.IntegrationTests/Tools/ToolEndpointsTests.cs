using System.Net;
using System.Text.Json;
using DMO.Domain.Tools;
using DMO.IntegrationTests.Host;
using DMO.IntegrationTests.JobOn;

namespace DMO.IntegrationTests.Tools;

/// <summary>
/// P2-T04 transport-class (<c>I</c>) proofs of the canonical Tool endpoints: the REAL web host, the
/// REAL <c>FerramentasEndpoints</c> group, the REAL <c>ToolService</c> and the REAL validator, over
/// the controlled repository double.
/// </summary>
/// <remarks>
/// <para>
/// Authority: P2-T04 contract §13.2 routes 12 and 13 (paths, verbs, policy), §14.3 (the exact
/// transport mapping of <c>SearchResults</c>, <c>Created</c>, <c>ValidationFailed</c> and
/// <c>DuplicateIdentity</c>) and §20.1 rows TOL13, TOL14 and TOL15.
/// </para>
/// <para>
/// The caller holds the <c>ferramentas</c> grant throughout this class, so every outcome observed
/// here is a domain/transport outcome and never a policy denial. Denials are proven separately by
/// <see cref="ToolAccessTests"/>. The three failure families stay distinct: a validation failure is
/// a 400 with codes, an identity collision is a 409 with the existing identity, and "no matches" is
/// a 200 with an explicit empty <c>items</c> array.
/// </para>
/// </remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class ToolEndpointsTests
{
    private const string ToolsPath = "/ferramentas/tools";

    /// <summary>The canonical Tool search route with a bounded limit and one exact criterion.</summary>
    private const string SearchByReferencePath = ToolsPath + "?reference=5447T173&limit=10";

    /// <summary>
    /// TOL13 (contract §20.1) — proves AC-8: <c>POST /ferramentas/tools</c> returns 201 carrying the
    /// real canonical <c>toolId</c>, and the persisted row is retrievable by that exact id (search by
    /// its reference), with its type/reference/lot/processo/quantity/machines round-tripping.
    /// </summary>
    [Fact]
    public async Task TOL13_CreateReturnsThePersistedCanonicalToolId()
    {
        var store = new P2T04TestStore();
        using var factory = P2T04TestHost.ForUser(P2T04TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        using var create = await P2T04TestHost.SendJsonAsync(
            client,
            HttpMethod.Post,
            ToolsPath,
            ToolBody("CM", "5447T173", "12"));

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        Guid toolId;
        using (var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync()))
        {
            toolId = created.RootElement.GetProperty("toolId").GetGuid();
        }

        Assert.NotEqual(Guid.Empty, toolId);

        // The persisted row is retrievable by the returned canonical id.
        var search = await P2T04TestHost.GetAsync(client, SearchByReferencePath);
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);

        using (var found = JsonDocument.Parse(await search.Content.ReadAsStringAsync()))
        {
            var root = found.RootElement;
            Assert.Equal(10, root.GetProperty("limit").GetInt32());

            var items = root.GetProperty("items");
            Assert.Equal(1, items.GetArrayLength());

            var item = items[0];
            Assert.Equal(toolId, item.GetProperty("toolId").GetGuid());
            Assert.Equal("CM", item.GetProperty("type").GetString());
            Assert.Equal("5447T173", item.GetProperty("reference").GetString());
            Assert.Equal("12", item.GetProperty("lot").GetString());
            Assert.Equal("NNPB", item.GetProperty("processo").GetString());
            Assert.Equal(3, item.GetProperty("quantity").GetInt32());
            Assert.Equal(
                new[] { "B1", "C2" },
                item.GetProperty("compatibleMachines")
                    .EnumerateArray()
                    .Select(machine => machine.GetString() ?? string.Empty)
                    .ToArray());
        }

        // The identity returned by the endpoint is the identity the repository actually persisted:
        // the identity tuple resolves to exactly that canonical Tool.
        var persisted = await store.FindByIdentityAsync(ToolType.Cm, "5447T173", "12", CancellationToken.None);
        Assert.NotNull(persisted);
        Assert.Equal(toolId, persisted!.ToolId.Value);
        Assert.Equal(ToolType.Cm, persisted.Type);
        Assert.Equal("5447T173", persisted.Reference);
        Assert.Equal("12", persisted.Lot);
        Assert.Equal("B1,C2", string.Join(",", persisted.CompatibleMachines.Select(machine => machine.Value)));
        Assert.Equal(1, store.ToolCount);
    }

    /// <summary>
    /// TOL14 (contract §20.1) — proves AC-4: posting the same canonical identity tuple twice yields
    /// 201 and then 409 <c>duplicate-identity</c> naming the FIRST <c>toolId</c> as
    /// <c>existingToolId</c>, and the registry still holds exactly one matching Tool.
    /// </summary>
    [Fact]
    public async Task TOL14_DuplicateIdentityIsRefusedWithTheExistingToolIdAndNoSecondRow()
    {
        var store = new P2T04TestStore();
        using var factory = P2T04TestHost.ForUser(P2T04TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        // The SAME identity tuple (type, reference, lot) both times.
        var body = ToolBody("CM", "5447T173", "12");

        using var first = await P2T04TestHost.SendJsonAsync(client, HttpMethod.Post, ToolsPath, body);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        Guid firstToolId;
        using (var created = JsonDocument.Parse(await first.Content.ReadAsStringAsync()))
        {
            firstToolId = created.RootElement.GetProperty("toolId").GetGuid();
        }

        Assert.NotEqual(Guid.Empty, firstToolId);

        using var second = await P2T04TestHost.SendJsonAsync(client, HttpMethod.Post, ToolsPath, body);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        using (var refusal = JsonDocument.Parse(await second.Content.ReadAsStringAsync()))
        {
            var root = refusal.RootElement;
            Assert.Equal("duplicate-identity", root.GetProperty("reason").GetString());
            Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("message").GetString()));

            var existingToolId = root.GetProperty("existingToolId").GetGuid();
            Assert.NotEqual(Guid.Empty, existingToolId);
            Assert.Equal(firstToolId, existingToolId);
        }

        // No second row: the registry holds exactly one item for that reference.
        var search = await P2T04TestHost.GetAsync(client, SearchByReferencePath);
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);

        using (var found = JsonDocument.Parse(await search.Content.ReadAsStringAsync()))
        {
            var items = found.RootElement.GetProperty("items");
            Assert.Equal(1, items.GetArrayLength());
            Assert.Equal(firstToolId, items[0].GetProperty("toolId").GetGuid());
        }

        Assert.Equal(1, store.ToolCount);
    }

    /// <summary>
    /// TOL15 (contract §20.1) — proves AC-42 and AC-45: a missing criterion set, an out-of-range
    /// limit, an unparseable type and an unparseable machine are each a 400
    /// <c>validation-failed</c> carrying the exact contracted code, while a valid bounded search
    /// with no matches is a 200 with an explicit EMPTY <c>items</c> array — never a 400 and never a
    /// denial.
    /// </summary>
    [Fact]
    public async Task TOL15_InvalidCriteriaAreRefusedWithExactCodesAndNoMatchesIsAnEmptyTwoHundred()
    {
        var store = new P2T04TestStore();

        // The registry is deliberately NOT empty, so the empty result below is a real filter
        // outcome and not an artefact of an empty store.
        store.SeedTool(ToolType.Cm, "5447T173", "12", Processo.Nnpb, 3, "B1");

        using var factory = P2T04TestHost.ForUser(P2T04TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        // (a) no search criterion at all: never an unbounded registry dump.
        var noCriteria = await ReadValidationErrorsAsync(
            await P2T04TestHost.GetAsync(client, ToolsPath));
        Assert.Contains("SEARCH_CRITERIA_REQUIRED", noCriteria);

        // (b) limit below the contracted range 1..100.
        var zeroLimit = await ReadValidationErrorsAsync(
            await P2T04TestHost.GetAsync(client, $"{ToolsPath}?reference=5447T173&limit=0"));
        Assert.Contains("LIMIT_OUT_OF_RANGE", zeroLimit);
        Assert.DoesNotContain("SEARCH_CRITERIA_REQUIRED", zeroLimit);

        // (c) limit above the contracted range 1..100.
        var tooLargeLimit = await ReadValidationErrorsAsync(
            await P2T04TestHost.GetAsync(client, $"{ToolsPath}?reference=5447T173&limit=101"));
        Assert.Contains("LIMIT_OUT_OF_RANGE", tooLargeLimit);
        Assert.DoesNotContain("SEARCH_CRITERIA_REQUIRED", tooLargeLimit);

        // (d) an unparseable type filter is refused, never silently ignored (which would widen the
        // result set behind the operator's back).
        var unknownType = await ReadValidationErrorsAsync(
            await P2T04TestHost.GetAsync(client, $"{ToolsPath}?type=XX&limit=10"));
        Assert.Contains("TOOL_TYPE_UNKNOWN", unknownType);

        // (e) an unparseable machine filter (URL-encoded "linha-b", which is not a settled machine).
        var unknownMachine = await ReadValidationErrorsAsync(
            await P2T04TestHost.GetAsync(client, $"{ToolsPath}?machine=linha%2Db&limit=10"));
        Assert.Contains("MACHINE_UNKNOWN", unknownMachine);

        // (f) a valid criterion with no match is an explicit empty, distinct from a failure.
        var noMatches = await P2T04TestHost.GetAsync(client, $"{ToolsPath}?reference=SEM-REGISTO&limit=10");
        Assert.Equal(HttpStatusCode.OK, noMatches.StatusCode);

        using var empty = JsonDocument.Parse(await noMatches.Content.ReadAsStringAsync());
        Assert.False(empty.RootElement.TryGetProperty("reason", out _));
        Assert.Equal(10, empty.RootElement.GetProperty("limit").GetInt32());
        Assert.Equal(0, empty.RootElement.GetProperty("items").GetArrayLength());
    }

    /// <summary>The six Beta minimum Tool facts of a valid create request, in the contracted shape.</summary>
    private static string ToolBody(string type, string reference, string lot) =>
        P2T04TestHost.Json(new
        {
            type,
            reference,
            lot,
            processo = "NNPB",
            quantity = 3,
            machines = new[] { "B1", "C2" },
        });

    /// <summary>
    /// Asserts the contracted validation transport (400 + <c>validation-failed</c>) and returns the
    /// exact machine-readable codes carried in <c>errors</c>.
    /// </summary>
    private static async Task<string[]> ReadValidationErrorsAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("validation-failed", document.RootElement.GetProperty("reason").GetString());

        return document.RootElement
            .GetProperty("errors")
            .EnumerateArray()
            .Select(error => error.GetString() ?? string.Empty)
            .ToArray();
    }
}
