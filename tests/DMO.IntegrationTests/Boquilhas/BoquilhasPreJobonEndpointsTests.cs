using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DMO.Application.Boquilhas;
using DMO.Domain.Boquilhas;
using DMO.Domain.Tools;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DMO.IntegrationTests.Boquilhas;

/// <summary>
/// P2-T07 §34 OWNER-CLARIFICATION delta — transport proofs of the transitional pré-JobOn register
/// and the human-confirmed association (the same accepted Peso associate pattern of P2-T05 §4.4:
/// the anchor must match by the same canonical <c>tool_id</c> UUID; no inference rule).
/// </summary>
/// <remarks>
/// Authority: P2-T07 OWNER CLARIFICATION §34.1–§34.4. The real <see cref="BoquilhasService"/> runs
/// over the controlled <see cref="P2T07TestStore"/> (with the REAL Job On/Tool services over the
/// accepted P2-T04 store). The proofs: a register can exist BEFORE any Job On, anchored only on the
/// real canonical BQ Tool; a different-UUID <c>bq_id</c> never becomes a candidate and never
/// associates (association-mismatch); the same UUID produces the candidates; the association is
/// executed ONLY by the explicit action (nothing is ever silent or automatic); the SAME
/// <c>boquilhas_id</c> keeps its history and passes to <c>bq_id → jobon_id</c>; a repeated
/// association is refused typed (<c>already-associated</c>) and never duplicates; stale versions
/// are refused; the Job-On-incoming read (<c>pending-registers</c>) is a specific, tool-keyed
/// light packet — never a global scan.
/// </remarks>
public sealed class BoquilhasPreJobonEndpointsTests
{
    // ------------------------------------------------------------------ arrangement

    private static (P2T07TestStore Store, Guid ToolId) ArrangePendingTool(
        string reference = "5447T173",
        string lot = "LOTE-1")
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(ToolType.Bq, reference, lot);
        return (store, tool.ToolId.Value);
    }

    /// <summary>A production whose BQ context anchors the supplied tool.</summary>
    private static (Guid JobOnId, Guid BqId) ArrangeProduction(
        P2T07TestStore store,
        Guid toolId,
        string reference = "REF-1",
        string production = "P1") =>
        store.SeedProductionWithBq(reference, production, toolId);

    // 1. pré-JobOn registration (route 4 with the pending_tool_id anchor) --------------------

    /// <summary>
    /// PRE1 — a Boquilhas register can exist BEFORE any Job On: the create with
    /// <c>pending_tool_id</c> anchors the REAL canonical BQ Tool (201); the ficha carries the
    /// pending state with the Tool facts and NO production context; NO quantity movement is ever
    /// manufactured.
    /// </summary>
    [Fact]
    public async Task PRE1_PréJobOnRegister_IsAnchoredOnTheCanonicalBqTool()
    {
        var (store, toolId) = ArrangePendingTool();

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var create = await P2T07TestHost.SendJsonAsync(
            client, HttpMethod.Post, "/boquilhas/registers", P2T07TestHost.Json(new { pendingToolId = toolId }));

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<RegisterCreatedResponse>();
        Assert.NotNull(created?.BoquilhasId);

        // The ficha is pending: BqId null, ToolId set, no production, no movements.
        using var ficha = await P2T07TestHost.GetAsync(client, $"/boquilhas/registers/{created.BoquilhasId}");
        Assert.Equal(HttpStatusCode.OK, ficha.StatusCode);
        var payload = await ReadJsonAsync(ficha);

        var value = payload.GetProperty("ficha");
        Assert.True(value.GetProperty("isPending").GetBoolean());
        Assert.Equal(JsonValueKind.Null, value.GetProperty("bqId").ValueKind);
        Assert.Equal(toolId, value.GetProperty("toolId").GetGuid());
        Assert.Equal(1, value.GetProperty("version").GetInt32());
        Assert.Equal(0, value.GetProperty("movements").GetArrayLength());
        Assert.Equal("5447T173", value.GetProperty("pendingTool").GetProperty("toolReference").GetString());
        Assert.Equal("LOTE-1", value.GetProperty("pendingTool").GetProperty("toolLot").GetString());
        Assert.Equal(JsonValueKind.Null, value.GetProperty("production").ValueKind);
    }

    /// <summary>
    /// PRE2 — the pré-JobOn anchor refuses a non-existent Tool (<c>TOOL_NOT_FOUND</c>) and a real
    /// Tool of the WRONG family (<c>TOOL_TYPE_MISMATCH</c>): the anchor is a REAL canonical BQ
    /// tool — never a minted identity; nothing is written.
    /// </summary>
    [Fact]
    public async Task PRE2_PendingAnchor_RefusesUnknownAndNonBqTools()
    {
        var store = new P2T07TestStore();
        var cmTool = store.SeedTool(ToolType.Cm, "CM-1", "LOTE-CM");

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using (var unknown = await P2T07TestHost.SendJsonAsync(
                   client, HttpMethod.Post, "/boquilhas/registers", P2T07TestHost.Json(new { pendingToolId = Guid.NewGuid() })))
        {
            Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
            var payload = await ReadJsonAsync(unknown);
            Assert.Contains(
                BoquilhasValidationErrors.ToolNotFound,
                payload.GetProperty("errors").EnumerateArray().Select(error => error.GetString() ?? string.Empty));
        }

        using (var wrongType = await P2T07TestHost.SendJsonAsync(
                   client, HttpMethod.Post, "/boquilhas/registers", P2T07TestHost.Json(new { pendingToolId = cmTool.ToolId.Value })))
        {
            Assert.Equal(HttpStatusCode.BadRequest, wrongType.StatusCode);
            var payload = await ReadJsonAsync(wrongType);
            Assert.Contains(
                BoquilhasValidationErrors.ToolTypeMismatch,
                payload.GetProperty("errors").EnumerateArray().Select(error => error.GetString() ?? string.Empty));
        }

        Assert.Equal(0, store.RegisterCount);
    }

    /// <summary>
    /// PRE3 — the anchor XOR rule is enforced at the transport: both anchors or neither are
    /// refused with <c>ANCHOR_CONFLICT</c> (never two concurrent authorities).
    /// </summary>
    [Fact]
    public async Task PRE3_CreateWithBothOrNeitherAnchor_IsRefused()
    {
        var (store, toolId) = ArrangePendingTool();
        var (_, bqId) = ArrangeProduction(store, toolId, reference: "REF-2");

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using (var both = await P2T07TestHost.SendJsonAsync(
                   client, HttpMethod.Post, "/boquilhas/registers", P2T07TestHost.Json(new { bqId, pendingToolId = toolId })))
        {
            Assert.Equal(HttpStatusCode.BadRequest, both.StatusCode);
            var payload = await ReadJsonAsync(both);
            Assert.Contains(
                BoquilhasValidationErrors.AnchorConflict,
                payload.GetProperty("errors").EnumerateArray().Select(error => error.GetString() ?? string.Empty));
        }

        using (var neither = await P2T07TestHost.SendJsonAsync(
                   client, HttpMethod.Post, "/boquilhas/registers", P2T07TestHost.Json(new { })))
        {
            Assert.Equal(HttpStatusCode.BadRequest, neither.StatusCode);
            var payload = await ReadJsonAsync(neither);
            Assert.Contains(
                BoquilhasValidationErrors.AnchorConflict,
                payload.GetProperty("errors").EnumerateArray().Select(error => error.GetString() ?? string.Empty));
        }

        Assert.Equal(0, store.RegisterCount);
    }

    // 2. the association point: the SAME canonical tool_id UUID ------------------------------

    /// <summary>
    /// ASC1 — a PENDING register finds candidates ONLY in the REAL <c>bq_contexts</c> rows with
    /// the SAME canonical Tool UUID: a production of the same tool appears, a production of a
    /// DIFFERENT tool never does (identity is the UUID — never reference/lote/máquina/texto).
    /// </summary>
    [Fact]
    public async Task ASC1_CandidatesAreOnlyTheSameToolUuidContexts()
    {
        var store = new P2T07TestStore();
        var pendingTool = store.SeedTool(ToolType.Bq, "5447T173", "LOTE-1");
        var otherTool = store.SeedTool(ToolType.Bq, "9999T999", "LOTE-X");

        // The SAME tool has a production; a DIFFERENT tool has another production.
        var (_, sameBqId) = ArrangeProduction(store, pendingTool.ToolId.Value, reference: "REF-SAME", production: "P-SAME");
        ArrangeProduction(store, otherTool.ToolId.Value, reference: "REF-OTHER", production: "P-OTHER");

        var pending = store.SeedPendingRegister(pendingTool.ToolId.Value);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var candidates = await P2T07TestHost.GetAsync(
            client, $"/boquilhas/registers/{pending.BoquilhasId.Value}/association-candidates");
        Assert.Equal(HttpStatusCode.OK, candidates.StatusCode);

        var payload = await ReadJsonAsync(candidates);
        var rows = payload.GetProperty("candidates").EnumerateArray().ToArray();
        var row = Assert.Single(rows);
        Assert.Equal(sameBqId, row.GetProperty("bqId").GetGuid());
        Assert.Equal("REF-SAME", row.GetProperty("reference").GetString());
        Assert.Equal("P-SAME", row.GetProperty("productionNumber").GetString());
    }

    /// <summary>
    /// ASC2 — the association is executed ONLY by the explicit human action: before any associate
    /// call nothing changes (the register stays pending — no silent association); a DIFFERENT-UUID
    /// <c>bq_id</c> is refused with 409 <c>association-mismatch</c> and nothing is written.
    /// </summary>
    [Fact]
    public async Task ASC2_NoSilentAssociation_AndAMismatchedUuidIsRefused()
    {
        var store = new P2T07TestStore();
        var pendingTool = store.SeedTool(ToolType.Bq, "5447T173", "LOTE-1");
        var otherTool = store.SeedTool(ToolType.Bq, "9999T999", "LOTE-X");
        var (_, otherBqId) = ArrangeProduction(store, otherTool.ToolId.Value, reference: "REF-OTHER", production: "P-OTHER");

        var pending = store.SeedPendingRegister(pendingTool.ToolId.Value);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // The mere existence of the production never associates anything: still pending.
        Assert.True(pending.IsPending);

        using var mismatch = await P2T07TestHost.SendJsonAsync(
            client,
            HttpMethod.Post,
            $"/boquilhas/registers/{pending.BoquilhasId.Value}/associate",
            P2T07TestHost.Json(new { bqId = otherBqId, expectedVersion = 1 }));

        Assert.Equal(HttpStatusCode.Conflict, mismatch.StatusCode);
        var refusal = await ReadJsonAsync(mismatch);
        Assert.Equal("association-mismatch", refusal.GetProperty("reason").GetString());

        // Nothing was written: the register keeps the pending anchor (single row, same id).
        var after = store.GetByIdAsync(pending.BoquilhasId.Value, CancellationToken.None).Result;
        Assert.NotNull(after);
        Assert.True(after!.IsPending);
        Assert.Equal(pendingTool.ToolId.Value, after.ToolId);
        Assert.Equal(1, after.Version);
    }

    /// <summary>
    /// ASC3 — the confirmed association preserves the SAME <c>boquilhas_id</c> and the whole
    /// history: bq_id set, provisional tool anchor cleared, version bumped once; the operational
    /// relation becomes <c>boquilhas_id → bq_id → jobon_id</c>; the movements keep every fact.
    /// </summary>
    [Fact]
    public async Task ASC3_ConfirmedAssociation_PreservesTheSameRegisterAndItsHistory()
    {
        var store = new P2T07TestStore();
        var repairerId = store.SeedRepairer("Reparador Externo").RepairerId.Value;
        var tool = store.SeedTool(ToolType.Bq, "5447T173", "LOTE-1");
        var (_, bqId) = ArrangeProduction(store, tool.ToolId.Value, reference: "REF-1", production: "P1");

        // A pending register with REAL work already recorded (legitimate pré-JobOn work).
        var pending = store.SeedPendingRegister(
            tool.ToolId.Value,
            (MovementKind.Saida, 10, new DateOnly(2026, 9, 25), "B1", repairerId),
            (MovementKind.Entrada, 3, new DateOnly(2026, 9, 26), null, null));
        var pendingId = pending.BoquilhasId.Value;

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var associate = await P2T07TestHost.SendJsonAsync(
            client,
            HttpMethod.Post,
            $"/boquilhas/registers/{pendingId}/associate",
            P2T07TestHost.Json(new { bqId, expectedVersion = 1 }));

        Assert.Equal(HttpStatusCode.OK, associate.StatusCode);
        var payload = await ReadJsonAsync(associate);
        Assert.Equal(pendingId, payload.GetProperty("boquilhasId").GetGuid());
        Assert.Equal(2, payload.GetProperty("version").GetInt32());
        Assert.Equal(bqId, payload.GetProperty("bqId").GetGuid());

        // The SAME register row: no second register, no copied movements, no fake Job On.
        Assert.Equal(1, store.RegisterCount);

        var after = store.GetByIdAsync(pendingId, CancellationToken.None).Result;
        Assert.NotNull(after);
        Assert.False(after!.IsPending);
        Assert.Equal(bqId, after.BqId);
        Assert.Null(after.ToolId);
        Assert.Equal(2, after.Version);
        Assert.Equal(2, after.Movements.Count);

        // The ficha resolves the REAL production through bq_id → jobon_id.
        using var ficha = await P2T07TestHost.GetAsync(client, $"/boquilhas/registers/{pendingId}");
        var fichaPayload = await ReadJsonAsync(ficha);
        var value = fichaPayload.GetProperty("ficha");
        Assert.False(value.GetProperty("isPending").GetBoolean());
        Assert.Equal("REF-1", value.GetProperty("production").GetProperty("reference").GetString());
        Assert.Equal("P1", value.GetProperty("production").GetProperty("productionNumber").GetString());
        Assert.Equal(2, value.GetProperty("movements").GetArrayLength());

        // The historical repairer fact is untouched.
        Assert.Equal(repairerId, value.GetProperty("movements")[0].GetProperty("repairerId").GetGuid());
    }

    /// <summary>
    /// ASC4 — a repeated association is refused TYPED (<c>already-associated</c>): the production-
    /// linked register never re-associates, never duplicates and never silently moves the context.
    /// A stale observed version is refused (<c>stale-version</c>).
    /// </summary>
    [Fact]
    public async Task ASC4_RepeatedAssociationIsRefusedTyped_AndStaleVersionsAreRefused()
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(ToolType.Bq, "5447T173", "LOTE-1");
        var (_, bqId) = ArrangeProduction(store, tool.ToolId.Value);
        var pending = store.SeedPendingRegister(tool.ToolId.Value);
        var pendingId = pending.BoquilhasId.Value;

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using (var first = await P2T07TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Post,
                   $"/boquilhas/registers/{pendingId}/associate",
                   P2T07TestHost.Json(new { bqId, expectedVersion = 1 })))
        {
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        }

        // Repeated association: typed refusal, nothing written, no duplicate.
        using (var again = await P2T07TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Post,
                   $"/boquilhas/registers/{pendingId}/associate",
                   P2T07TestHost.Json(new { bqId, expectedVersion = 2 })))
        {
            Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
            var refusal = await ReadJsonAsync(again);
            Assert.Equal("already-associated", refusal.GetProperty("reason").GetString());
        }

        Assert.Equal(1, store.RegisterCount);
        var after = store.GetByIdAsync(pendingId, CancellationToken.None).Result;
        Assert.NotNull(after);
        Assert.False(after!.IsPending);

        // A stale version on a pending register is refused before any write.
        var store2 = new P2T07TestStore();
        var tool2 = store2.SeedTool(ToolType.Bq, "5447T174", "LOTE-2");
        var (_, bq2) = ArrangeProduction(store2, tool2.ToolId.Value);
        var pending2 = store2.SeedPendingRegister(tool2.ToolId.Value);

        using var factory2 = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store2);
        using var client2 = factory2.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var stale = await P2T07TestHost.SendJsonAsync(
            client2,
            HttpMethod.Post,
            $"/boquilhas/registers/{pending2.BoquilhasId.Value}/associate",
            P2T07TestHost.Json(new { bqId = bq2, expectedVersion = 7 }));

        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var stalePayload = await ReadJsonAsync(stale);
        Assert.Equal("stale-version", stalePayload.GetProperty("reason").GetString());
        Assert.True(store2.GetByIdAsync(pending2.BoquilhasId.Value, CancellationToken.None).Result!.IsPending);
    }

    // 3. the Job-On-incoming direction (bq_id → tool_id → pending registers) ------------------

    /// <summary>
    /// INC1 — when the production context arrives (bq_id), the specific read returns ONLY the
    /// pending registers of ITS canonical Tool (the same UUID): a pending register of the same
    /// tool is presented; a pending register of another tool and the production's own
    /// production-linked register are NOT — a light packet (facts + derived movement counts), the
    /// history only when opened.
    /// </summary>
    [Fact]
    public async Task INC1_PendingRegisters_AreKeyedByTheProductionCanonicalTool()
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(ToolType.Bq, "5447T173", "LOTE-1");
        var otherTool = store.SeedTool(ToolType.Bq, "9999T999", "LOTE-X");
        var (_, bqId) = ArrangeProduction(store, tool.ToolId.Value);

        // The same tool's pending register (with work), another tool's pending register, and the
        // production's OWN register (already production-linked).
        var samePending = store.SeedPendingRegister(
            tool.ToolId.Value,
            (MovementKind.Saida, 10, new DateOnly(2026, 9, 25), "B1", store.SeedRepairer("R").RepairerId.Value));
        store.SeedPendingRegister(otherTool.ToolId.Value);
        store.SeedRegister(bqId);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await P2T07TestHost.GetAsync(
            client, $"/boquilhas/pending-registers?bqId={bqId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        var row = Assert.Single(payload.GetProperty("registers").EnumerateArray().ToArray());
        Assert.Equal(samePending.BoquilhasId.Value, row.GetProperty("boquilhasId").GetGuid());
        Assert.Equal(tool.ToolId.Value, row.GetProperty("toolId").GetGuid());
        Assert.Equal(1, row.GetProperty("version").GetInt32());
        Assert.Equal(1, row.GetProperty("movementCount").GetInt32());
        Assert.Equal(10, row.GetProperty("outstanding").GetInt32());

        // An unknown bq_id is a validation failure, never an invented answer.
        using var unknown = await P2T07TestHost.GetAsync(
            client, $"/boquilhas/pending-registers?bqId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
    }

    /// <summary>
    /// PRE4 — the pending register stays fully operational BEFORE the association: movements are
    /// recorded on the pending anchor exactly like on a production-linked register (legitimate
    /// pré-JobOn work; the closed three-type vocabulary applies).
    /// </summary>
    [Fact]
    public async Task PRE4_PendingRegister_RecordsMovementsBeforeAnyJobOn()
    {
        var store = new P2T07TestStore();
        var repairerId = store.SeedRepairer("Reparador Externo").RepairerId.Value;
        var tool = store.SeedTool(ToolType.Bq, "5447T173", "LOTE-1");
        var pending = store.SeedPendingRegister(tool.ToolId.Value);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var saida = await P2T07TestHost.SendJsonAsync(
            client,
            HttpMethod.Post,
            $"/boquilhas/registers/{pending.BoquilhasId.Value}/movements",
            P2T07TestHost.Json(new
            {
                movementType = "saida",
                quantity = 8,
                businessDate = "2026-09-25",
                machine = "B1",
                repairerId,
                observations = (string?)null,
            }));
        Assert.Equal(HttpStatusCode.Created, saida.StatusCode);

        using var entrada = await P2T07TestHost.SendJsonAsync(
            client,
            HttpMethod.Post,
            $"/boquilhas/registers/{pending.BoquilhasId.Value}/movements",
            P2T07TestHost.Json(new
            {
                movementType = "entrada",
                quantity = 2,
                businessDate = "2026-09-26",
                machine = (string?)null,
                repairerId = (Guid?)null,
                observations = (string?)null,
            }));
        Assert.Equal(HttpStatusCode.Created, entrada.StatusCode);

        Assert.Equal(2, store.GetByIdAsync(pending.BoquilhasId.Value, CancellationToken.None).Result!.Movements.Count);
        Assert.Equal(6, store.GetByIdAsync(pending.BoquilhasId.Value, CancellationToken.None).Result!.Outstanding);
    }

    // ---- helpers -------------------------------------------------------------------------

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.Clone();
    }

    private sealed record RegisterCreatedResponse(Guid BoquilhasId);
}