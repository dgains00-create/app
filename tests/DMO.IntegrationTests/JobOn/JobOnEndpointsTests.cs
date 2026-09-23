using System.Net;
using System.Text.Json;
using DMO.Application.Access;
using DMO.Application.JobOn;
using DMO.Application.Repositories;
using DMO.Application.Tools;
using DMO.Domain.JobOn;
using DMO.Domain.Tools;
using DMO.IntegrationTests.Host;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DomainJobOn = DMO.Domain.JobOn.JobOn;

namespace DMO.IntegrationTests.JobOn;

/// <summary>
/// P2-T04 transport-class (<c>I</c>) proofs of the Job On occurrence endpoints and surfaces: the REAL
/// web host with the REAL <c>JobOnEndpoints</c>/Razor Pages, the REAL <c>JobOnService</c> and the REAL
/// validators, over the controlled repository double.
/// </summary>
/// <remarks>
/// <para>
/// Authority: P2-T04 contract §13.2 (routes 2, 3, 5, 6 and 11), §14.3 (the exact transport mapping),
/// §8.1 (reference → productions), §8.4 (ficha), §11 (create/edit/delete transactions) and §20.2/§20.3/
/// §20.4/§20.5/§20.7 rows JOB6, JOB11, JOB12, JOB13, JOB14, JOB16, JOB17, JOB28, JOB29, JOB30, CTX13,
/// DUP11, DEP5, DEP7, DEP8 and ORC5.
/// </para>
/// <para>
/// Every caller in this class holds the grant the route under test requires, so each observed outcome
/// is a domain/transport outcome and never a policy denial; denials are proven by
/// <see cref="JobOnAccessTests"/>.
/// </para>
/// </remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class JobOnEndpointsTests
{
    private const string JobOnPath = "/jobon";
    private const string ProductionsPath = "/jobon/productions";
    private const string CreateProductionsPath = "/jobon/create/productions";
    private const string ToolsPath = "/ferramentas/tools";

    /// <summary>JOB6 (contract §20.2) — proves AC-16, AC-20: the second create of the same
    /// (<c>reference</c>, <c>production_number</c>) pair is refused with
    /// <c>duplicate-production</c> and the existing <c>jobonId</c>, and exactly one occurrence
    /// exists.</summary>
    [Fact]
    public async Task JOB6_SecondCreateForTheSamePairIsRefusedWithTheExistingId()
    {
        var store = new P2T04TestStore();
        using var factory = P2T04TestHost.ForUser([CreateOnly()], store);
        using var client = factory.CreateClient();

        var body = CreateBody("REF-JOB6", "1000");

        using var first = await P2T04TestHost.SendJsonAsync(client, HttpMethod.Post, JobOnPath, body);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var created = await ReadJsonAsync(first);
        var firstJobOnId = created.GetProperty("jobonId").GetGuid();
        Assert.NotEqual(Guid.Empty, firstJobOnId);
        Assert.Equal(1, created.GetProperty("version").GetInt32());

        using var second = await P2T04TestHost.SendJsonAsync(client, HttpMethod.Post, JobOnPath, body);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var refusal = await ReadJsonAsync(second);
        Assert.Equal("duplicate-production", refusal.GetProperty("reason").GetString());
        Assert.Equal(firstJobOnId, refusal.GetProperty("existingJobOnId").GetGuid());
        Assert.False(string.IsNullOrWhiteSpace(refusal.GetProperty("message").GetString()));

        // The refused retry wrote nothing: exactly one occurrence remains.
        Assert.Equal(1, store.JobOnCount);
        Assert.Equal(0, store.ContextCount);
    }

    /// <summary>JOB11 (contract §20.2) — proves AC-21, AC-23: the reference → productions query
    /// returns EVERY matching occurrence, including older ones, with no "latest" marker, flag or
    /// field anywhere in the response.</summary>
    [Fact]
    public async Task JOB11_ProductionsQueryReturnsEveryMatchingOccurrenceIncludingOlderOnes()
    {
        var store = new P2T04TestStore();
        var oldest = store.SeedJobOn("REF-JOB11", "0900", "B1", new DateOnly(2024, 1, 5));
        var middle = store.SeedJobOn("REF-JOB11", "1000", "B2", new DateOnly(2025, 6, 1));
        var newest = store.SeedJobOn("REF-JOB11", "1100", "C1", new DateOnly(2026, 2, 2));
        store.SeedJobOn("REF-OTHER", "1000");

        using var factory = P2T04TestHost.ForUser([ViewOnly()], store);
        using var client = factory.CreateClient();

        using var response = await P2T04TestHost.GetAsync(client, $"{ProductionsPath}?reference=REF-JOB11");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        var productions = payload.GetProperty("productions");
        Assert.Equal(3, productions.GetArrayLength());

        var ids = productions.EnumerateArray().Select(item => item.GetProperty("jobonId").GetGuid()).ToArray();
        Assert.Contains(oldest.JobOnId.Value, ids);
        Assert.Contains(middle.JobOnId.Value, ids);
        Assert.Contains(newest.JobOnId.Value, ids);

        // Every returned item carries identity plus human production facts only: no age filter, no
        // "latest" marker and no recency meaning exists in the carrier.
        foreach (var production in productions.EnumerateArray())
        {
            var members = production.EnumerateObject().Select(member => member.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            Assert.Equal(
                new[] { "jobonId", "machine", "productionDate", "productionNumber", "reference" },
                members);
            Assert.Equal("REF-JOB11", production.GetProperty("reference").GetString());
        }

        var oldestItem = productions.EnumerateArray()
            .Single(item => item.GetProperty("jobonId").GetGuid() == oldest.JobOnId.Value);
        Assert.Equal("0900", oldestItem.GetProperty("productionNumber").GetString());
        Assert.Equal("2024-01-05", oldestItem.GetProperty("productionDate").GetString());
    }

    /// <summary>JOB12 (contract §20.2) — proves AC-22: exactly one match returns exactly one item and
    /// the consuming consult surface still selects nothing (no auto-resolution, no preselected
    /// row).</summary>
    [Fact]
    public async Task JOB12_SingleMatchReturnsOneItemAndTheConsumingSurfaceSelectsNothing()
    {
        var store = new P2T04TestStore();
        var only = store.SeedJobOn("REF-JOB12", "1000", "B1", new DateOnly(2026, 4, 9));

        using var factory = P2T04TestHost.ForUser([ViewOnly()], store);
        using var client = factory.CreateClient();

        using var response = await P2T04TestHost.GetAsync(client, $"{ProductionsPath}?reference=REF-JOB12");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        var productions = payload.GetProperty("productions");
        Assert.Equal(1, productions.GetArrayLength());
        Assert.Equal(only.JobOnId.Value, productions[0].GetProperty("jobonId").GetGuid());
        Assert.Equal("1000", productions[0].GetProperty("productionNumber").GetString());

        // The consuming surface renders the single row but selects nothing.
        using var page = await P2T04TestHost.GetAsync(client, $"{JobOnPath}?reference=REF-JOB12");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);

        var html = await page.Content.ReadAsStringAsync();
        Assert.Equal(1, Count(html, "data-dmo-row=\"true\""));
        Assert.Contains("data-dmo-selection-enabled=\"false\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("aria-selected=\"true\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("dmo-table__row--selected", html, StringComparison.Ordinal);
    }

    /// <summary>JOB13 (contract §20.2) — proves AC-24: zero matches, a failed lookup and a denied
    /// caller are three DIFFERENT outcomes on both the query and the consuming surface, and no one of
    /// them is aliased to the empty result.</summary>
    [Fact]
    public async Task JOB13_EmptyLookupFailureAndDenialRenderAsThreeDistinctOutcomes()
    {
        // (a) zero matches -> an explicit empty result, never a failure and never a denial.
        var emptyStore = new P2T04TestStore();
        using (var emptyFactory = P2T04TestHost.ForUser([ViewOnly()], emptyStore))
        using (var emptyClient = emptyFactory.CreateClient())
        {
            using var query = await P2T04TestHost.GetAsync(emptyClient, $"{ProductionsPath}?reference=REF-JOB13-NONE");
            Assert.Equal(HttpStatusCode.OK, query.StatusCode);

            var payload = await ReadJsonAsync(query);
            Assert.Equal(0, payload.GetProperty("productions").GetArrayLength());

            using var page = await P2T04TestHost.GetAsync(emptyClient, $"{JobOnPath}?reference=REF-JOB13-NONE");
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);

            var emptyHtml = await page.Content.ReadAsStringAsync();
            Assert.Contains("data-dmo-state=\"empty\"", emptyHtml, StringComparison.Ordinal);
            Assert.DoesNotContain("data-dmo-state=\"lookup-failed\"", emptyHtml, StringComparison.Ordinal);
            Assert.Equal(0, Count(emptyHtml, "data-dmo-row=\"true\""));
        }

        // (b) a forced repository failure -> lookup-failed, never an empty list and never a 200 with
        // zero rows.
        var failingStore = new P2T04TestStore();
        using (var failingFactory = P2T04TestHost.ForUser(
            [ViewOnly()],
            failingStore,
            services =>
            {
                services.RemoveAll<IJobOnRepository>();
                services.AddSingleton<IJobOnRepository>(new ThrowingJobOnRepository());
            }))
        using (var failingClient = failingFactory.CreateClient())
        {
            // The transport maps an unexpected infrastructure failure to 500 (contract §14.3): it is
            // never an empty 200 payload.
            var failure = await SendToleratingInfrastructureFailureAsync(
                failingClient,
                $"{ProductionsPath}?reference=REF-JOB13");
            var observed = failure is null ? "an unhandled infrastructure failure" : failure.StatusCode.ToString();
            Assert.True(
                failure is null || failure.StatusCode == HttpStatusCode.InternalServerError,
                "A forced repository failure must surface as an infrastructure failure, never as a "
                + "successful response; observed: " + observed + ".");

            if (failure is not null)
            {
                var failureBody = await failure.Content.ReadAsStringAsync();
                Assert.DoesNotContain("\"productions\"", failureBody, StringComparison.Ordinal);
            }

            // The consuming surface renders the failed lookup as its own state, structurally distinct
            // from "no results".
            using var page = await P2T04TestHost.GetAsync(failingClient, $"{JobOnPath}?reference=REF-JOB13");
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);

            var lookupFailedHtml = await page.Content.ReadAsStringAsync();
            Assert.Contains("data-dmo-state=\"lookup-failed\"", lookupFailedHtml, StringComparison.Ordinal);
            Assert.DoesNotContain("data-dmo-state=\"empty\"", lookupFailedHtml, StringComparison.Ordinal);
            Assert.Equal(0, Count(lookupFailedHtml, "data-dmo-row=\"true\""));
        }

        // (c) a denied caller -> a denial, never a blank-but-served surface.
        using (var deniedFactory = P2T04TestHost.ForUser([CreateOnly()], new P2T04TestStore()))
        using (var deniedClient = deniedFactory.CreateClient())
        {
            using var deniedQuery = await P2T04TestHost.GetAsync(deniedClient, $"{ProductionsPath}?reference=REF-JOB13");
            using var deniedPage = await P2T04TestHost.GetAsync(deniedClient, $"{JobOnPath}?reference=REF-JOB13");

            Assert.Equal(HttpStatusCode.Forbidden, deniedQuery.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, deniedPage.StatusCode);

            var deniedHtml = await deniedPage.Content.ReadAsStringAsync();
            Assert.DoesNotContain("data-dmo-jobon-surface", deniedHtml, StringComparison.Ordinal);
            Assert.DoesNotContain("data-dmo-state=\"empty\"", deniedHtml, StringComparison.Ordinal);
            Assert.DoesNotContain("data-dmo-state=\"lookup-failed\"", deniedHtml, StringComparison.Ordinal);
        }
    }

    /// <summary>JOB14 (contract §20.2) — proves AC-45: a blank, whitespace or missing reference is a
    /// validation failure carrying <c>REFERENCE_REQUIRED</c> on BOTH contracted productions reads, and
    /// is never rendered as an empty result.</summary>
    [Fact]
    public async Task JOB14_BlankReferenceIsAValidationFailureNeverAnEmptyList()
    {
        var store = new P2T04TestStore();
        store.SeedJobOn("REF-JOB14", "1000");

        using var factory = P2T04TestHost.ForUser([ViewOnly(), CreateOnly()], store);
        using var client = factory.CreateClient();

        foreach (var path in new[]
                 {
                     ProductionsPath,
                     $"{ProductionsPath}?reference=",
                     $"{ProductionsPath}?reference=%20%20",
                     CreateProductionsPath,
                     $"{CreateProductionsPath}?reference=",
                     $"{CreateProductionsPath}?reference=%20%20",
                 })
        {
            using var response = await P2T04TestHost.GetAsync(client, path);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var payload = await ReadJsonAsync(response);
            Assert.Equal("validation-failed", payload.GetProperty("reason").GetString());
            Assert.Contains(
                JobOnValidationErrors.ReferenceRequired,
                payload.GetProperty("errors").EnumerateArray().Select(error => error.GetString() ?? string.Empty));

            // A validation failure is never aliased to "no results".
            Assert.False(payload.TryGetProperty("productions", out _));
        }
    }

    /// <summary>JOB16 (contract §20.2) — proves AC-26: the Job On read returns the occurrence with its
    /// existing contexts and each context's FROZEN triple, which a later canonical Tool change never
    /// rewrites.</summary>
    [Fact]
    public async Task JOB16_FichaReadReturnsTheOccurrenceWithItsContextsAndFrozenTriples()
    {
        var store = new P2T04TestStore();
        var tool = store.SeedTool(ToolType.Cm, "5447T173", "LOTE-JOB16");
        var occurrence = store.SeedJobOn(
            "REF-JOB16",
            "1000",
            "B1",
            new DateOnly(2026, 5, 6),
            contexts: [Context(ToolContextType.Cm, tool)]);

        // The canonical Tool's own metadata changes after the context was frozen.
        store.ChangeToolMetadata(tool.ToolId.Value, "CHANGED-REFERENCE", "CHANGED-LOT");

        using var factory = P2T04TestHost.ForUser([ViewOnly()], store);
        using var client = factory.CreateClient();

        using var response = await P2T04TestHost.GetAsync(client, $"{JobOnPath}/{occurrence.JobOnId.Value}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("data-dmo-jobon-surface=\"sheet\"", html, StringComparison.Ordinal);
        Assert.Contains($"data-dmo-jobon-reference=\"REF-JOB16\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-jobon-production-number=\"1000\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-jobon-machine=\"B1\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-jobon-version=\"1\"", html, StringComparison.Ordinal);

        Assert.Contains("data-dmo-jobon-context=\"CM\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-frozen-triple=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-frozen-type=\"CM\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-frozen-reference=\"5447T173\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-frozen-lot=\"LOTE-JOB16\"", html, StringComparison.Ordinal);

        // The live Tool projection is presented separately and is refreshed; the frozen triple is not.
        Assert.Contains("CHANGED-REFERENCE", html, StringComparison.Ordinal);
        Assert.Equal(1, Count(html, "data-dmo-frozen-reference=\"5447T173\""));
        Assert.Equal(0, Count(html, "data-dmo-frozen-reference=\"CHANGED-REFERENCE\""));
    }

    /// <summary>JOB17 (contract §20.2) — proves AC-24: an unknown <c>jobonId</c> is a 404, never an
    /// empty or fabricated ficha.</summary>
    [Fact]
    public async Task JOB17_UnknownIdIsNotFoundNeverAnEmptyFicha()
    {
        var store = new P2T04TestStore();
        store.SeedJobOn("REF-JOB17", "1000");

        using var factory = P2T04TestHost.ForUser([ViewOnly()], store);
        using var client = factory.CreateClient();

        using var response = await P2T04TestHost.GetAsync(client, $"{JobOnPath}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("data-dmo-jobon-surface", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-frozen-triple", html, StringComparison.Ordinal);
        Assert.DoesNotContain("REF-JOB17", html, StringComparison.Ordinal);
    }

    /// <summary>JOB28 (contract §20.2) — proves AC-70, AC-71: with a reached/passed persisted
    /// production date an unacknowledged edit is refused with
    /// <c>date-threshold-confirmation-required</c> and writes NOTHING, while the same edit with the
    /// explicit acknowledgement is accepted and persisted.</summary>
    [Fact]
    public async Task JOB28_EditOnAReachedProductionDateRequiresAcknowledgementAndThenPersists()
    {
        var store = new P2T04TestStore();
        var reachedDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3);
        var occurrence = store.SeedJobOn("REF-JOB28", "1000", "B1", reachedDate);

        using var factory = P2T04TestHost.ForUser([CreateOnly()], store);
        using var client = factory.CreateClient();

        var repository = factory.Services.GetRequiredService<IJobOnRepository>();
        var before = await repository.GetByIdAsync(occurrence.JobOnId.Value, CancellationToken.None);
        Assert.NotNull(before);

        var body = UpdateBody(
            expectedVersion: before!.Version,
            reference: "REF-JOB28-CHANGED",
            productionNumber: "1001",
            machine: "B2",
            productionDate: reachedDate.AddDays(1),
            acknowledged: false);

        using var refused = await P2T04TestHost.SendJsonAsync(
            client,
            HttpMethod.Put,
            $"{JobOnPath}/{occurrence.JobOnId.Value}",
            body);

        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);

        var refusal = await ReadJsonAsync(refused);
        Assert.Equal("date-threshold-confirmation-required", refusal.GetProperty("reason").GetString());

        // Nothing was written: every persisted fact and the version are unchanged.
        var afterRefusal = await repository.GetByIdAsync(occurrence.JobOnId.Value, CancellationToken.None);
        Assert.NotNull(afterRefusal);
        Assert.Equal(before.Reference, afterRefusal!.Reference);
        Assert.Equal(before.ProductionNumber, afterRefusal.ProductionNumber);
        Assert.Equal(before.Machine, afterRefusal.Machine);
        Assert.Equal(before.ProductionDate, afterRefusal.ProductionDate);
        Assert.Equal(before.Version, afterRefusal.Version);
        Assert.Equal(1, store.JobOnCount);

        // The same request with the explicit acknowledgement proceeds and persists.
        var acknowledgedBody = UpdateBody(
            expectedVersion: before.Version,
            reference: "REF-JOB28-CHANGED",
            productionNumber: "1001",
            machine: "B2",
            productionDate: reachedDate.AddDays(1),
            acknowledged: true);

        using var accepted = await P2T04TestHost.SendJsonAsync(
            client,
            HttpMethod.Put,
            $"{JobOnPath}/{occurrence.JobOnId.Value}",
            acknowledgedBody);

        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        var updated = await ReadJsonAsync(accepted);
        Assert.Equal(occurrence.JobOnId.Value, updated.GetProperty("jobonId").GetGuid());
        Assert.Equal(before.Version + 1, updated.GetProperty("version").GetInt32());

        var persisted = await repository.GetByIdAsync(occurrence.JobOnId.Value, CancellationToken.None);
        Assert.NotNull(persisted);
        Assert.Equal("REF-JOB28-CHANGED", persisted!.Reference);
        Assert.Equal("1001", persisted.ProductionNumber);
        Assert.Equal(MachineCode.From("B2"), persisted.Machine);
        Assert.Equal(reachedDate.AddDays(1), persisted.ProductionDate);
        Assert.Equal(before.Version + 1, persisted.Version);
    }

    /// <summary>JOB29 (contract §20.2) — proves AC-75: a delete without the explicit confirmation is
    /// refused with <c>validation-failed</c> carrying <c>DELETE_NOT_CONFIRMED</c>, and nothing is
    /// deleted.</summary>
    [Fact]
    public async Task JOB29_DeleteWithoutExplicitConfirmationIsRefusedAndDeletesNothing()
    {
        var store = new P2T04TestStore();
        var tool = store.SeedTool(ToolType.Cm, "5447T173", "LOTE-JOB29");
        var occurrence = store.SeedJobOn("REF-JOB29", "1000", contexts: [Context(ToolContextType.Cm, tool)]);

        using var factory = P2T04TestHost.ForUser([CreateOnly()], store);
        using var client = factory.CreateClient();

        using var response = await P2T04TestHost.SendAuthenticatedAsync(
            client,
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"{JobOnPath}/{occurrence.JobOnId.Value}?expectedVersion=1&dateThresholdAcknowledged=true"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("validation-failed", payload.GetProperty("reason").GetString());
        Assert.Contains(
            JobOnValidationErrors.DeleteNotConfirmed,
            payload.GetProperty("errors").EnumerateArray().Select(error => error.GetString() ?? string.Empty));

        Assert.Equal(1, store.JobOnCount);
        Assert.Equal(1, store.ContextCount);

        var repository = factory.Services.GetRequiredService<IJobOnRepository>();
        Assert.NotNull(await repository.GetByIdAsync(occurrence.JobOnId.Value, CancellationToken.None));
    }

    /// <summary>JOB30 (contract §20.2) — proves AC-76: a delete of an occurrence whose production date
    /// was reached/passed without the stronger acknowledgement is refused with
    /// <c>date-threshold-confirmation-required</c> and nothing is deleted.</summary>
    [Fact]
    public async Task JOB30_DeleteOnAReachedProductionDateWithoutAcknowledgementIsRefused()
    {
        var store = new P2T04TestStore();
        var reachedDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10);
        var tool = store.SeedTool(ToolType.Cm, "5447T173", "LOTE-JOB30");
        var occurrence = store.SeedJobOn(
            "REF-JOB30",
            "1000",
            productionDate: reachedDate,
            contexts: [Context(ToolContextType.Cm, tool)]);

        using var factory = P2T04TestHost.ForUser([CreateOnly()], store);
        using var client = factory.CreateClient();

        using var response = await P2T04TestHost.SendAuthenticatedAsync(
            client,
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"{JobOnPath}/{occurrence.JobOnId.Value}?expectedVersion=1&deleteConfirmed=true&dateThresholdAcknowledged=false"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var refusal = await ReadJsonAsync(response);
        Assert.Equal("date-threshold-confirmation-required", refusal.GetProperty("reason").GetString());

        Assert.Equal(1, store.JobOnCount);
        Assert.Equal(1, store.ContextCount);

        var repository = factory.Services.GetRequiredService<IJobOnRepository>();
        Assert.NotNull(await repository.GetByIdAsync(occurrence.JobOnId.Value, CancellationToken.None));
    }

    /// <summary>CTX13 (contract §20.3) — proves AC-26, AC-27: reading an occurrence with no contexts
    /// returns an empty context list (an explicit empty region, no fabricated context) and creates no
    /// rows of any kind.</summary>
    [Fact]
    public async Task CTX13_FichaWithNoContextsReturnsAnEmptyContextListAndCreatesNoRows()
    {
        var store = new P2T04TestStore();
        var occurrence = store.SeedJobOn("REF-CTX13", "1000");

        using var factory = P2T04TestHost.ForUser([ViewOnly()], store);
        using var client = factory.CreateClient();

        Assert.Equal(0, store.ContextCount);

        using var response = await P2T04TestHost.GetAsync(client, $"{JobOnPath}/{occurrence.JobOnId.Value}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("data-dmo-jobon-surface=\"sheet\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-jobon-contexts=\"true\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-frozen-triple=\"true\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-jobon-context=", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-state=\"empty\"", html, StringComparison.Ordinal);

        // A read never creates a context row and never mutates the occurrence.
        Assert.Equal(0, store.ContextCount);
        Assert.Equal(1, store.JobOnCount);

        var repository = factory.Services.GetRequiredService<IJobOnRepository>();
        var persisted = await repository.GetByIdAsync(occurrence.JobOnId.Value, CancellationToken.None);
        Assert.NotNull(persisted);
        Assert.Empty(persisted!.Contexts);
        Assert.Equal(1, persisted.Version);
    }

    /// <summary>DUP11 (contract §20.4) — proves AC-63: a duplication forced to fail mid-transaction is
    /// refused with a 409 and leaves ZERO new rows (occurrence and contexts both rolled back).</summary>
    [Fact]
    public async Task DUP11_FailedDuplicationIsRefusedAndLeavesZeroNewRows()
    {
        var store = new P2T04TestStore();
        var tool = store.SeedTool(ToolType.Cm, "5447T173", "LOTE-DUP11");
        var source = store.SeedJobOn("REF-DUP11", "1000", contexts: [Context(ToolContextType.Cm, tool)]);

        store.FailJobOnDuplicate = true;

        using var factory = P2T04TestHost.ForUser([CreateOnly()], store);
        using var client = factory.CreateClient();

        var jobOnsBefore = store.JobOnCount;
        var contextsBefore = store.ContextCount;

        using var response = await P2T04TestHost.SendJsonAsync(
            client,
            HttpMethod.Post,
            $"{JobOnPath}/{source.JobOnId.Value}/duplicate",
            DuplicateBody(expectedSourceVersion: source.Version, productionNumber: "1001"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var refusal = await ReadJsonAsync(response);
        Assert.False(string.IsNullOrWhiteSpace(refusal.GetProperty("reason").GetString()));

        // Total rollback: no new occurrence and no new context row exist.
        Assert.Equal(jobOnsBefore, store.JobOnCount);
        Assert.Equal(contextsBefore, store.ContextCount);
        Assert.Equal(1, store.JobOnCount);
        Assert.Equal(1, store.ContextCount);

        var repository = factory.Services.GetRequiredService<IJobOnRepository>();
        var persistedSource = await repository.GetByIdAsync(source.JobOnId.Value, CancellationToken.None);
        Assert.NotNull(persistedSource);
        Assert.Equal(source.Version, persistedSource!.Version);
        Assert.Single(persistedSource.Contexts);
    }

    /// <summary>DUP15 (contract §20.4) — proves AC-60, AC-59: after duplication the new Job On's CM
    /// Tool can be changed while the source's CM context keeps its <c>tool_id</c> and frozen triple.</summary>
    [Fact]
    public async Task DUP15_TheDuplicatedJobOnsCmToolCanBeChangedWhileTheSourceKeepsItsToolIdAndFrozenTriple()
    {
        var store = new P2T04TestStore();
        var toolA = store.SeedTool(ToolType.Cm, "5447T173", "LOTE-DUP15A");
        var toolB = store.SeedTool(ToolType.Cm, "5447T174", "LOTE-DUP15B");
        var source = store.SeedJobOn("REF-DUP15", "1000", contexts: [Context(ToolContextType.Cm, toolA)]);

        using var factory = P2T04TestHost.ForUser([CreateOnly()], store);
        using var client = factory.CreateClient();

        // Duplicate from the explicitly chosen source.
        using var duplicated = await P2T04TestHost.SendJsonAsync(
            client,
            HttpMethod.Post,
            $"{JobOnPath}/{source.JobOnId.Value}/duplicate",
            DuplicateBody(expectedSourceVersion: source.Version, productionNumber: "1001"));

        Assert.Equal(HttpStatusCode.Created, duplicated.StatusCode);

        var payload = await ReadJsonAsync(duplicated);
        var duplicateId = payload.GetProperty("jobonId").GetGuid();
        Assert.Equal(source.JobOnId.Value, payload.GetProperty("sourceJobOnId").GetGuid());
        Assert.Equal(1, payload.GetProperty("version").GetInt32());
        Assert.NotEqual(source.JobOnId.Value, duplicateId);

        // Change the DUPLICATE's CM Tool through the explicit ToolAssociationChange list.
        using var updated = await P2T04TestHost.SendJsonAsync(
            client,
            HttpMethod.Put,
            $"{JobOnPath}/{duplicateId}",
            UpdateBody(
                expectedVersion: 1,
                reference: "REF-DUP15",
                productionNumber: "1001",
                machine: "B1",
                productionDate: null,
                acknowledged: false,
                associations:
                [
                    new { contextType = "CM", action = "Set", toolId = toolB.ToolId.Value },
                ]));

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        // The duplicate's CM context now references Tool B and carries Tool B's frozen triple...
        var repository = factory.Services.GetRequiredService<IJobOnRepository>();
        var persistedDuplicate = await repository.GetByIdAsync(duplicateId, CancellationToken.None);
        Assert.NotNull(persistedDuplicate);

        var duplicateContext = Assert.Single(persistedDuplicate!.Contexts);
        Assert.Equal(ToolContextType.Cm, duplicateContext.ContextType);
        Assert.Equal(toolB.ToolId.Value, duplicateContext.ToolId.Value);
        Assert.Equal(new ToolContextSnapshot(toolB.Type, toolB.Reference, toolB.Lot), duplicateContext.Frozen);

        // ...while the source's CM context keeps Tool A and its own frozen triple, untouched.
        var persistedSource = await repository.GetByIdAsync(source.JobOnId.Value, CancellationToken.None);
        Assert.NotNull(persistedSource);

        var sourceContext = Assert.Single(persistedSource!.Contexts);
        Assert.Equal(ToolContextType.Cm, sourceContext.ContextType);
        Assert.Equal(toolA.ToolId.Value, sourceContext.ToolId.Value);
        Assert.Equal(new ToolContextSnapshot(toolA.Type, toolA.Reference, toolA.Lot), sourceContext.Frozen);
        Assert.Equal(1, persistedSource.Version);
    }

    /// <summary>DEP5 (contract §20.5) — proves AC-77, AC-78: an occurrence recorded as a duplication
    /// source cannot be deleted; the refusal is 409 <c>dependency-exists</c> whose
    /// <c>dependencies[]</c> names the <c>duplication-lineage</c> kind with a description, and nothing
    /// is deleted.</summary>
    [Fact]
    public async Task DEP5_DeleteRefusalNamesTheDuplicationLineageDependency()
    {
        var store = new P2T04TestStore();
        var source = store.SeedJobOn("REF-DEP5", "1000");
        var dependent = store.SeedJobOn("REF-DEP5", "1001", copiedFromJobOnId: source.JobOnId.Value);

        using var factory = P2T04TestHost.ForUser([CreateOnly()], store);
        using var client = factory.CreateClient();

        using var response = await P2T04TestHost.SendAuthenticatedAsync(
            client,
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"{JobOnPath}/{source.JobOnId.Value}?expectedVersion=1&deleteConfirmed=true&dateThresholdAcknowledged=true"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var refusal = await ReadJsonAsync(response);
        Assert.Equal("dependency-exists", refusal.GetProperty("reason").GetString());

        var dependencies = refusal.GetProperty("dependencies");
        Assert.True(dependencies.GetArrayLength() >= 1, "The refusal must name at least one dependency.");

        var lineage = dependencies.EnumerateArray()
            .Where(dependency => dependency.GetProperty("kind").GetString() == "duplication-lineage")
            .ToArray();

        Assert.NotEmpty(lineage);
        Assert.All(
            lineage,
            dependency => Assert.False(
                string.IsNullOrWhiteSpace(dependency.GetProperty("description").GetString()),
                "A reported dependency must carry a human description."));

        // Nothing was deleted, and the recorded lineage still points at the source.
        Assert.Equal(2, store.JobOnCount);

        var repository = factory.Services.GetRequiredService<IJobOnRepository>();
        Assert.NotNull(await repository.GetByIdAsync(source.JobOnId.Value, CancellationToken.None));
        var persistedDependent = await repository.GetByIdAsync(dependent.JobOnId.Value, CancellationToken.None);
        Assert.NotNull(persistedDependent);
        Assert.Equal(source.JobOnId.Value, persistedDependent!.CopiedFromJobOnId);
    }

    /// <summary>DEP7 (contract §20.5) — proves AC-82: with an ADDITIONAL registered dependency probe
    /// reporting a dependency, the delete is refused naming that contribution and no row is
    /// deleted.</summary>
    [Fact]
    public async Task DEP7_ARegisteredDependencyProbeRefusesTheDeleteAndRemovesNoRow()
    {
        var store = new P2T04TestStore();
        var tool = store.SeedTool(ToolType.Cm, "5447T173", "LOTE-DEP7");
        var occurrence = store.SeedJobOn("REF-DEP7", "1000", contexts: [Context(ToolContextType.Cm, tool)]);

        using var factory = P2T04TestHost.ForUser(
            [CreateOnly()],
            store,
            services => services.AddSingleton<IJobOnDependencyProbe>(new ContributingDependencyProbe()));

        using var client = factory.CreateClient();

        using var response = await P2T04TestHost.SendAuthenticatedAsync(
            client,
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"{JobOnPath}/{occurrence.JobOnId.Value}?expectedVersion=1&deleteConfirmed=true&dateThresholdAcknowledged=true"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var refusal = await ReadJsonAsync(response);
        Assert.Equal("dependency-exists", refusal.GetProperty("reason").GetString());

        var dependencies = refusal.GetProperty("dependencies").EnumerateArray().ToArray();
        Assert.Contains(
            dependencies,
            dependency => dependency.GetProperty("kind").GetString() == ContributingDependencyProbe.Kind);
        Assert.Contains(
            dependencies,
            dependency => dependency.GetProperty("description").GetString() == ContributingDependencyProbe.Description);

        Assert.Equal(1, store.JobOnCount);
        Assert.Equal(1, store.ContextCount);

        var repository = factory.Services.GetRequiredService<IJobOnRepository>();
        Assert.NotNull(await repository.GetByIdAsync(occurrence.JobOnId.Value, CancellationToken.None));
    }

    /// <summary>DEP8 (contract §20.5) — proves AC-77, AC-82: the SAME delete, on a composition that
    /// differs ONLY by not registering that extra probe, succeeds and removes the occurrence with its
    /// contexts.</summary>
    [Fact]
    public async Task DEP8_WithoutThatProbeRegisteredTheSameDeleteSucceeds()
    {
        var store = new P2T04TestStore();
        var tool = store.SeedTool(ToolType.Cm, "5447T173", "LOTE-DEP8");
        var occurrence = store.SeedJobOn("REF-DEP8", "1000", contexts: [Context(ToolContextType.Cm, tool)]);

        using var factory = P2T04TestHost.ForUser([CreateOnly()], store);
        using var client = factory.CreateClient();

        using var response = await P2T04TestHost.SendAuthenticatedAsync(
            client,
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"{JobOnPath}/{occurrence.JobOnId.Value}?expectedVersion=1&deleteConfirmed=true&dateThresholdAcknowledged=true"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, store.JobOnCount);
        Assert.Equal(0, store.ContextCount);

        var repository = factory.Services.GetRequiredService<IJobOnRepository>();
        Assert.Null(await repository.GetByIdAsync(occurrence.JobOnId.Value, CancellationToken.None));
    }

    /// <summary>ORC5 (contract §20.7) — proves AC-51: the inline Tool create subflow returns the REAL
    /// persisted <c>tool_id</c>, and the originating Job On create then persists that exact canonical
    /// id in the CM context.</summary>
    [Fact]
    public async Task ORC5_TheInlineToolCreateIdIsTheIdTheOriginContextPersists()
    {
        var store = new P2T04TestStore();
        using var factory = P2T04TestHost.ForUser([CreateOnly(), FerramentasOnly()], store);
        using var client = factory.CreateClient();

        using var toolResponse = await P2T04TestHost.SendJsonAsync(
            client,
            HttpMethod.Post,
            ToolsPath,
            P2T04TestHost.Json(new
            {
                type = "CM",
                reference = "5447T173",
                lot = "LOTE-ORC5",
                machines = new[] { "B1", "C2" },
            }));

        Assert.Equal(HttpStatusCode.Created, toolResponse.StatusCode);

        var createdTool = await ReadJsonAsync(toolResponse);
        var toolId = createdTool.GetProperty("toolId").GetGuid();
        Assert.NotEqual(Guid.Empty, toolId);
        Assert.Equal(1, store.ToolCount);

        using var jobOnResponse = await P2T04TestHost.SendJsonAsync(
            client,
            HttpMethod.Post,
            JobOnPath,
            CreateBody("REF-ORC5", "1000", cmToolId: toolId));

        Assert.Equal(HttpStatusCode.Created, jobOnResponse.StatusCode);

        var createdJobOn = await ReadJsonAsync(jobOnResponse);
        var jobOnId = createdJobOn.GetProperty("jobonId").GetGuid();

        // The origin workflow resumed with the returned canonical id: the persisted CM context
        // references that exact tool_id and freezes the Tool's own triple.
        var repository = factory.Services.GetRequiredService<IJobOnRepository>();
        var persisted = await repository.GetByIdAsync(jobOnId, CancellationToken.None);
        Assert.NotNull(persisted);

        var context = Assert.Single(persisted!.Contexts);
        Assert.Equal(ToolContextType.Cm, context.ContextType);
        Assert.Equal(toolId, context.ToolId.Value);
        Assert.Equal(ToolType.Cm, context.Frozen.Type);
        Assert.Equal("5447T173", context.Frozen.Reference);
        Assert.Equal("LOTE-ORC5", context.Frozen.Lot);
        Assert.Equal(1, store.ContextCount);
    }

    // ---- arrangement helpers -------------------------------------------------------------

    /// <summary>The Job On View grant alone.</summary>
    private static ModuleDefinition ViewOnly() =>
        P2T04TestHost.Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");

    /// <summary>The Job On Create grant alone.</summary>
    private static ModuleDefinition CreateOnly() =>
        P2T04TestHost.Definition(ModuleCatalog.JobOnCreate, "Job On Create", "job-on", "Job On");

    /// <summary>The contextual Ferramentas grant alone.</summary>
    private static ModuleDefinition FerramentasOnly() =>
        P2T04TestHost.Definition(ModuleCatalog.Ferramentas, "Ferramentas", null, "Ferramentas", contextual: true);

    /// <summary>Builds a frozen production context over an already seeded canonical Tool.</summary>
    private static ToolContext Context(ToolContextType contextType, Tool tool) =>
        new(
            contextType,
            Guid.NewGuid(),
            JobOnId.New(),
            tool.ToolId,
            new ToolContextSnapshot(tool.Type, tool.Reference, tool.Lot));

    /// <summary>The Job On create transport body (the simplified Beta facts plus optional slots).</summary>
    private static string CreateBody(
        string reference,
        string productionNumber,
        string machine = "B1",
        Guid? cmToolId = null,
        Guid? mfToolId = null,
        Guid? bqToolId = null) =>
        P2T04TestHost.Json(new { reference, productionNumber, machine, cmToolId, mfToolId, bqToolId });

    /// <summary>The Job On edit transport body (four facts + the explicit association change list).</summary>
    private static string UpdateBody(
        int expectedVersion,
        string reference,
        string productionNumber,
        string machine,
        DateOnly? productionDate,
        bool acknowledged,
        object[]? associations = null) =>
        P2T04TestHost.Json(new
        {
            expectedVersion,
            reference,
            productionNumber,
            machine,
            productionDate,
            associations = associations ?? [],
            dateThresholdWarningAcknowledged = acknowledged,
        });

    /// <summary>The Job On duplication transport body (explicit source version + the new facts).</summary>
    private static string DuplicateBody(
        int expectedSourceVersion,
        string productionNumber,
        string machine = "B1",
        DateOnly? productionDate = null) =>
        P2T04TestHost.Json(new { expectedSourceVersion, productionNumber, machine, productionDate });

    /// <summary>Reads a JSON response body into a detached element.</summary>
    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.Clone();
    }

    /// <summary>
    /// Sends a request whose handler is expected to fail at the infrastructure boundary: the response
    /// is returned when the host mapped the failure, and <c>null</c> when the unhandled failure
    /// reached the client instead. Either way it is never a successful payload.
    /// </summary>
    private static async Task<HttpResponseMessage?> SendToleratingInfrastructureFailureAsync(
        HttpClient client,
        string path)
    {
        try
        {
            return await P2T04TestHost.GetAsync(client, path);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Counts non-overlapping occurrences of a fragment in rendered markup.</summary>
    private static int Count(string html, string fragment)
    {
        var count = 0;
        var index = 0;

        while ((index = html.IndexOf(fragment, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += fragment.Length;
        }

        return count;
    }

    /// <summary>
    /// A repository double whose every read fails, so the consuming surfaces observe a genuine
    /// infrastructure failure instead of an empty result.
    /// </summary>
    private sealed class ThrowingJobOnRepository : IJobOnRepository
    {
        public Task<DomainJobOn?> GetByIdAsync(Guid jobOnId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Forced repository failure (test double).");

        public Task<DomainJobOn?> FindByProductionAsync(
            string reference,
            string productionNumber,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Forced repository failure (test double).");

        public Task<IReadOnlyList<JobOnProductionListItem>> ListByReferenceAsync(
            string reference,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Forced repository failure (test double).");

        public Task<DomainJobOn> CreatedAsync(
            DomainJobOn jobOn,
            IReadOnlyList<ToolContext> contexts,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Forced repository failure (test double).");

        public Task<DomainJobOn> UpdatedAsync(
            DomainJobOn jobOn,
            IReadOnlyList<ToolContextChange> changes,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Forced repository failure (test double).");

        public Task<DomainJobOn> DuplicatedAsync(
            DomainJobOn duplicate,
            IReadOnlyList<ToolContext> duplicatedContexts,
            int expectedSourceVersion,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Forced repository failure (test double).");

        public Task DeletedAsync(Guid jobOnId, int expectedVersion, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Forced repository failure (test double).");

        public Task<IReadOnlyList<JobOnDependency>> ListLineageDependentsAsync(
            Guid jobOnId,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Forced repository failure (test double).");
    }

    /// <summary>
    /// An additional contributing module's probe: it reports one dependency of its own kind, proving
    /// that a dependency contributed outside the Job On module blocks the delete without P2-T04
    /// hardcoding that module's logic.
    /// </summary>
    private sealed class ContributingDependencyProbe : IJobOnDependencyProbe
    {
        /// <summary>The dependency kind this test double reports.</summary>
        public const string Kind = "test-double-dependent-fact";

        /// <summary>The dependency description this test double reports.</summary>
        public const string Description = "Um facto operacional de outro módulo depende deste Job On.";

        /// <inheritdoc />
        public Task<JobOnDependencyReport> InspectAsync(
            JobOnDependencyTarget target,
            CancellationToken cancellationToken) =>
            Task.FromResult(new JobOnDependencyReport(
                "test-double",
                [new JobOnDependency(Kind, Description)]));
    }
}
