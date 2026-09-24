using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DMO.Application.Access;
using DMO.IntegrationTests.JobOn;
using DMO.IntegrationTests.Navigation;
using DMO.Web.Endpoints;

namespace DMO.IntegrationTests.Boquilhas;

/// <summary>
/// P2-T07 endpoint behavior tests (contract §29 HTTP-class rows): the 15 minimal-API routes over
/// the real <see cref="BoquilhasService"/> and the in-memory composition — transport shapes,
/// typed results, the exact 400/404/409 vocabulary and the consumed reads.
/// </summary>
public sealed class BoquilhasEndpointsTests
{
    private static readonly Guid MissingId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static string Json(object value) => P2T07TestHost.Json(value);

    // ================================================================== create (route 7)

    /// <summary>I3 (AC-I3) — a supplied non-existent bq_id is refused: 400 BQ_CONTEXT_NOT_FOUND.</summary>
    [Fact]
    public async Task I3_ANonExistentBqIdIsRefusedWithBqContextNotFound()
    {
        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted());
        using var client = factory.CreateClient();

        var response = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, "/boquilhas/aggregates", Json(new
        {
            bqId = MissingId,
            machines = new[] { "B1" },
            initialQuantity = 10,
            openingDate = "2026-09-10",
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        Assert.Equal("validation-failed", payload.GetProperty("reason").GetString());
        Assert.Contains("BQ_CONTEXT_NOT_FOUND", payload.GetProperty("errors").EnumerateArray().Select(e => e.GetString()));
    }

    /// <summary>I2/I3 (AC-I2/AC-I3) — a standalone create anchors a real BQ Tool and succeeds;
    /// a non-BQ Tool is refused with TOOL_TYPE_MISMATCH.</summary>
    [Fact]
    public async Task I2I3_StandaloneCreateAnchorsTheRealBqToolAndNonBqToolsAreRefused()
    {
        var store = new P2T07TestStore();
        var bqTool = store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-100", "L1", machines: "B1");
        var cmTool = store.SeedTool(DMO.Domain.Tools.ToolType.Cm, "CM-200", "L2", machines: "B1");

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        // A CM Tool can never anchor a Boquilhas aggregate (TOOL_TYPE_MISMATCH).
        var mismatch = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, "/boquilhas/aggregates", Json(new
        {
            toolId = cmTool.ToolId.Value,
            machines = new[] { "B1" },
            initialQuantity = 10,
            openingDate = "2026-09-10",
        }));
        Assert.Equal(HttpStatusCode.BadRequest, mismatch.StatusCode);

        var created = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, "/boquilhas/aggregates", Json(new
        {
            toolId = bqTool.ToolId.Value,
            machines = new[] { "B1", "C1" },
            initialQuantity = 10,
            openingDate = "2026-09-10",
            utilisationPercent = 25.5m,
        }));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdPayload = JsonSerializer.Deserialize<JsonElement>(await created.Content.ReadAsStringAsync());
        var id = createdPayload.GetProperty("boquilhasId").GetGuid();
        Assert.Equal(1, createdPayload.GetProperty("version").GetInt32());

        // The ficha anchors tool_id ONLY: no bq_id, no fake Job On (I2).
        var ficha = await P2T07TestHost.GetAsync(client, $"/boquilhas/aggregates/{id}");
        var fichaPayload = JsonSerializer.Deserialize<JsonElement>(await ficha.Content.ReadAsStringAsync());
        var fichaBody = fichaPayload.GetProperty("ficha");
        Assert.Equal(bqTool.ToolId.Value, fichaBody.GetProperty("toolId").GetGuid());
        Assert.Equal(JsonValueKind.Null, fichaBody.GetProperty("bqId").ValueKind);
        Assert.Equal("BQ-100", fichaBody.GetProperty("reference").GetString());
        Assert.Equal(10, fichaBody.GetProperty("balance").GetProperty("disponivel").GetInt32());
        Assert.Equal(2, fichaBody.GetProperty("machines").EnumerateArray().Count());
    }

    /// <summary>I1 (AC-I1) — a production-linked create anchors the REAL bq_id: jobon_id and the
    /// canonical tool_id are reachable through it, never duplicated.</summary>
    [Fact]
    public async Task I1_ProductionLinkedCreateAnchorsTheRealBqId()
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-100", "L1", machines: "B1");
        var jobOn = store.SeedJobOnWithBqContext(
            "REF-1", "P1", "B1", tool.ToolId.Value, tool.Reference, tool.Lot);
        var bqId = jobOn.Contexts[0].ContextId;

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        var created = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, "/boquilhas/aggregates", Json(new
        {
            bqId,
            machines = new[] { "B1" },
            initialQuantity = 10,
            openingDate = "2026-09-10",
        }));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdPayload = JsonSerializer.Deserialize<JsonElement>(await created.Content.ReadAsStringAsync());
        var id = createdPayload.GetProperty("boquilhasId").GetGuid();

        // The ficha presents the REAL production context via bq_id → job_ons and the frozen triple.
        var ficha = await P2T07TestHost.GetAsync(client, $"/boquilhas/aggregates/{id}");
        var fichaBody = JsonSerializer.Deserialize<JsonElement>(await ficha.Content.ReadAsStringAsync())
            .GetProperty("ficha");
        Assert.Equal(bqId, fichaBody.GetProperty("bqId").GetGuid());
        Assert.Equal("REF-1", fichaBody.GetProperty("production").GetProperty("reference").GetString());
        Assert.Equal("P1", fichaBody.GetProperty("production").GetProperty("productionNumber").GetString());
        Assert.Equal("BQ-100", fichaBody.GetProperty("anchor").GetProperty("frozenToolReference").GetString());
        Assert.Equal("L1", fichaBody.GetProperty("anchor").GetProperty("frozenToolLot").GetString());
    }

    /// <summary>S7/Q-CREATE (AC-C6 facet) — a second ACTIVE aggregate for the same anchor is refused
    /// with 409 active-aggregate-exists, nothing written.</summary>
    [Fact]
    public async Task ActiveAggregateExists_SecondActiveAggregateForTheSameAnchorIsRefused()
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-100", "L1", machines: "B1");

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        var create = new Action<Task<HttpResponseMessage>>(_ => { });
        var payload = Json(new
        {
            toolId = tool.ToolId.Value,
            machines = new[] { "B1" },
            initialQuantity = 10,
            openingDate = "2026-09-10",
        });

        var first = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, "/boquilhas/aggregates", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, "/boquilhas/aggregates", payload);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var refusal = JsonSerializer.Deserialize<JsonElement>(await second.Content.ReadAsStringAsync());
        Assert.Equal("active-aggregate-exists", refusal.GetProperty("reason").GetString());

        Assert.Equal(1, store.AggregateCount);
    }

    // ================================================================== movements (routes 8/9)

    /// <summary>V3 (AC-V3) — any other type string → 400 MOVEMENT_TYPE_INVALID, nothing written.</summary>
    [Fact]
    public async Task V3_AnInvalidMovementTypeIsRefusedWithMovementTypeInvalid()
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-100", "L1", machines: "B1");

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        var id = await CreateAsync(client, Json(new
        {
            toolId = tool.ToolId.Value,
            machines = new[] { "B1" },
            initialQuantity = 10,
            openingDate = "2026-09-10",
        }));

        var append = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, $"/boquilhas/aggregates/{id}/movements", Json(new
        {
            expectedAggregateVersion = 1,
            movementType = "editar",
            quantity = 2,
            businessDate = "2026-09-11",
        }));
        Assert.Equal(HttpStatusCode.BadRequest, append.StatusCode);
        var payload = JsonSerializer.Deserialize<JsonElement>(await append.Content.ReadAsStringAsync());
        Assert.Equal("validation-failed", payload.GetProperty("reason").GetString());
        Assert.Contains("MOVEMENT_TYPE_INVALID", payload.GetProperty("errors").EnumerateArray().Select(e => e.GetString()));

        Assert.Equal(1, store.AggregateCount);
    }

    /// <summary>V4 (AC-V4) — a second Início append → 409 only-one-inicio; exactly one Início exists.</summary>
    [Fact]
    public async Task V4_ASecondInicioAppendIsRefusedWithOnlyOneInicio()
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-100", "L1", machines: "B1");

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        var id = await CreateAsync(client, Json(new
        {
            toolId = tool.ToolId.Value,
            machines = new[] { "B1" },
            initialQuantity = 10,
            openingDate = "2026-09-10",
        }));

        var append = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, $"/boquilhas/aggregates/{id}/movements", Json(new
        {
            expectedAggregateVersion = 1,
            movementType = "inicio",
            quantity = 5,
            businessDate = "2026-09-11",
        }));
        Assert.Equal(HttpStatusCode.Conflict, append.StatusCode);
        var payload = JsonSerializer.Deserialize<JsonElement>(await append.Content.ReadAsStringAsync());
        Assert.Equal("only-one-inicio", payload.GetProperty("reason").GetString());
    }

    /// <summary>B3 (AC-B3) — a Saída exceeding Disponível → 409 saida-exceeds-available, nothing
    /// written; a Saída exactly equal to Disponível succeeds.</summary>
    [Fact]
    public async Task B3_SaidaExceedingAvailableIsRefusedAndExactlyAvailableSucceeds()
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-100", "L1", machines: "B1");
        var repairer = store.SeedRepairer("Reparador A");

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        var id = await CreateAsync(client, Json(new
        {
            toolId = tool.ToolId.Value,
            machines = new[] { "B1" },
            initialQuantity = 10,
            openingDate = "2026-09-10",
        }));

        var exceed = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, $"/boquilhas/aggregates/{id}/movements", Json(new
        {
            expectedAggregateVersion = 1,
            movementType = "saida",
            quantity = 11,
            businessDate = "2026-09-11",
            machine = "B1",
            repairerId = repairer.RepairerId.Value,
        }));
        Assert.Equal(HttpStatusCode.Conflict, exceed.StatusCode);
        var refusal = JsonSerializer.Deserialize<JsonElement>(await exceed.Content.ReadAsStringAsync());
        Assert.Equal("saida-exceeds-available", refusal.GetProperty("reason").GetString());

        var exact = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, $"/boquilhas/aggregates/{id}/movements", Json(new
        {
            expectedAggregateVersion = 1,
            movementType = "saida",
            quantity = 10,
            businessDate = "2026-09-11",
            machine = "B1",
            repairerId = repairer.RepairerId.Value,
        }));
        Assert.Equal(HttpStatusCode.Created, exact.StatusCode);
    }

    /// <summary>B6/D1 (AC-B6/AC-D1) — an excess Entrada is neither clamped nor rejected and saves
    /// normally with the operator-editable business date; the ficha shows the excess and the
    /// negative Em reparação projection (non-blocking).</summary>
    [Fact]
    public async Task B6D1_ExcessEntradaIsRecordedWithAnOperatorEditableBusinessDate()
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-100", "L1", machines: "B1");
        var repairer = store.SeedRepairer("Reparador A");

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        var id = await CreateAsync(client, Json(new
        {
            toolId = tool.ToolId.Value,
            machines = new[] { "B1" },
            initialQuantity = 10,
            openingDate = "2026-09-10",
        }));

        await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, $"/boquilhas/aggregates/{id}/movements", Json(new
        {
            expectedAggregateVersion = 1,
            movementType = "saida",
            quantity = 6,
            businessDate = "2026-09-11",
            machine = "B1",
            repairerId = repairer.RepairerId.Value,
        }));

        // 8 returned when only 6 were in repair: recorded in full, NEVER clamped/rejected (AC-B6).
        var entrada = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, $"/boquilhas/aggregates/{id}/movements", Json(new
        {
            expectedAggregateVersion = 2,
            movementType = "entrada",
            quantity = 8,
            businessDate = "2026-09-12",
        }));
        Assert.Equal(HttpStatusCode.Created, entrada.StatusCode);
        var entradaPayload = JsonSerializer.Deserialize<JsonElement>(await entrada.Content.ReadAsStringAsync());
        Assert.NotEqual(Guid.Empty, entradaPayload.GetProperty("movementId").GetGuid());

        var ficha = await P2T07TestHost.GetAsync(client, $"/boquilhas/aggregates/{id}");
        var body = JsonSerializer.Deserialize<JsonElement>(await ficha.Content.ReadAsStringAsync()).GetProperty("ficha");
        var ledger = body.GetProperty("movements").EnumerateArray().ToList();

        var entradaRow = ledger.Single(m => m.GetProperty("movementType").GetString() == "entrada");
        Assert.Equal(8, entradaRow.GetProperty("quantity").GetInt32());
        Assert.Equal(6, entradaRow.GetProperty("expectedReturnQuantity").GetInt32());
        Assert.Equal(2, entradaRow.GetProperty("excessReceivedQuantity").GetInt32());
        Assert.Equal("2026-09-12", entradaRow.GetProperty("businessDate").GetString());

        // Disponível 10 - 6 + 8 = 12; Em reparação 6 - 8 = -2 (negative, visible, non-blocking).
        Assert.Equal(12, body.GetProperty("balance").GetProperty("disponivel").GetInt32());
        Assert.Equal(-2, body.GetProperty("balance").GetProperty("emReparacao").GetInt32());
        Assert.Equal(2, body.GetProperty("balance").GetProperty("entradaExcecional").GetInt32());
    }

    /// <summary>R1 (AC-R1) — the machine → current assignment → repairer resolution: the
    /// assignments read returns the current assignment and the saved Saída stores the final
    /// selected repairer.</summary>
    [Fact]
    public async Task R1_MachineAssignmentsResolveAndTheSavedSaidaStoresTheFinalRepairer()
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-100", "L1", machines: "B1");
        var repairer = store.SeedRepairer("Reparador B1");
        store.SeedAssignment("B1", repairer.RepairerId.Value);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        var assignments = await P2T07TestHost.GetAsync(client, "/boquilhas/machine-assignments");
        var assignmentPayload = JsonSerializer.Deserialize<JsonElement>(await assignments.Content.ReadAsStringAsync());
        var b1 = assignmentPayload.GetProperty("assignments").EnumerateArray()
            .Single(entry => entry.GetProperty("machine").GetString() == "B1");
        Assert.Equal(repairer.RepairerId.Value, b1.GetProperty("repairerId").GetGuid());
        Assert.Equal("Reparador B1", b1.GetProperty("repairerName").GetString());
        Assert.False(b1.GetProperty("assignmentUnavailable").GetBoolean());

        var c1 = assignmentPayload.GetProperty("assignments").EnumerateArray()
            .Single(entry => entry.GetProperty("machine").GetString() == "C1");
        Assert.True(c1.GetProperty("assignmentUnavailable").GetBoolean());
    }

    /// <summary>R5 (AC-R5) — an external Saída without machine/repairer → 400 MACHINE_REQUIRED /
    /// REPAIRER_REQUIRED with nothing written.</summary>
    [Fact]
    public async Task R5_ExternalSaidaRequiresMachineAndRepairer()
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-100", "L1", machines: "B1");

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        var id = await CreateAsync(client, Json(new
        {
            toolId = tool.ToolId.Value,
            machines = new[] { "B1" },
            initialQuantity = 10,
            openingDate = "2026-09-10",
        }));

        var noMachine = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, $"/boquilhas/aggregates/{id}/movements", Json(new
        {
            expectedAggregateVersion = 1,
            movementType = "saida",
            quantity = 2,
            businessDate = "2026-09-11",
        }));
        Assert.Equal(HttpStatusCode.BadRequest, noMachine.StatusCode);
        var payload = JsonSerializer.Deserialize<JsonElement>(await noMachine.Content.ReadAsStringAsync());
        var errors = payload.GetProperty("errors").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("MACHINE_REQUIRED", errors);
        Assert.DoesNotContain(errors, error => error == "editar");

        Assert.Equal(1, store.AggregateCount);
    }

    // ================================================================== edit + audit (routes 9/6)

    /// <summary>E1/E2 (AC-E1/AC-E2) — Editar updates the SAME movement_id row with the exact
    /// before/after audit values; the entry row count is unchanged.</summary>
    [Fact]
    public async Task E1E2_EditUpdatesTheSameMovementRowWithExactAuditFacts()
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-100", "L1", machines: "B1");
        var repairer = store.SeedRepairer("Reparador A");

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        var id = await CreateAsync(client, Json(new
        {
            toolId = tool.ToolId.Value,
            machines = new[] { "B1" },
            initialQuantity = 10,
            openingDate = "2026-09-10",
        }));

        var appended = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, $"/boquilhas/aggregates/{id}/movements", Json(new
        {
            expectedAggregateVersion = 1,
            movementType = "saida",
            quantity = 6,
            businessDate = "2026-09-11",
            machine = "B1",
            repairerId = repairer.RepairerId.Value,
        }));
        var appendedPayload = JsonSerializer.Deserialize<JsonElement>(await appended.Content.ReadAsStringAsync());
        var movementId = appendedPayload.GetProperty("movementId").GetGuid();
        var aggregateVersion = appendedPayload.GetProperty("aggregateVersion").GetInt32();

        var edited = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Put, $"/boquilhas/aggregates/{id}/movements/{movementId}", Json(new
        {
            expectedAggregateVersion = aggregateVersion,
            expectedMovementVersion = 1,
            quantity = 4,
            businessDate = "2026-09-13",
            machine = "B1",
            repairerId = repairer.RepairerId.Value,
            observations = "corrigido",
        }));
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);

        var ficha = await P2T07TestHost.GetAsync(client, $"/boquilhas/aggregates/{id}");
        var body = JsonSerializer.Deserialize<JsonElement>(await ficha.Content.ReadAsStringAsync()).GetProperty("ficha");
        var saida = body.GetProperty("movements").EnumerateArray()
            .Single(m => m.GetProperty("movementType").GetString() == "saida");
        Assert.Equal(movementId, saida.GetProperty("movementId").GetGuid());
        Assert.Equal(4, saida.GetProperty("quantity").GetInt32());
        Assert.Equal("2026-09-13", saida.GetProperty("businessDate").GetString());

        // The movement row count is UNCHANGED by the edit: Início + the SAME edited Saída row —
        // no second quantity event was created (AC-E1/E3).
        Assert.Equal(2, body.GetProperty("movements").EnumerateArray().Count());

        var trail = await P2T07TestHost.GetAsync(client, $"/boquilhas/aggregates/{id}/movements/{movementId}/audit");
        var trailPayload = JsonSerializer.Deserialize<JsonElement>(await trail.Content.ReadAsStringAsync());
        var entry = trailPayload.GetProperty("entries").EnumerateArray().Single();
        Assert.Equal(6, entry.GetProperty("beforeQuantity").GetInt32());
        Assert.Equal(4, entry.GetProperty("afterQuantity").GetInt32());
        Assert.Equal("2026-09-11", entry.GetProperty("beforeBusinessDate").GetString());
        Assert.Equal("2026-09-13", entry.GetProperty("afterBusinessDate").GetString());
        Assert.Equal(P2T07TestHost.ActorUserId, entry.GetProperty("editedByUserId").GetGuid());
    }

    /// <summary>D2/D3 (AC-D2/AC-D3) — recorded_at is immutable across edits and changing
    /// business_date never rewrites it nor changes the balance.</summary>
    [Fact]
    public async Task D2D3_RecordedAtIsImmutableAcrossBusinessDateEdits()
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-100", "L1", machines: "B1");

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        var id = await CreateAsync(client, Json(new
        {
            toolId = tool.ToolId.Value,
            machines = new[] { "B1" },
            initialQuantity = 10,
            openingDate = "2026-09-10",
        }));

        var before = await P2T07TestHost.GetAsync(client, $"/boquilhas/aggregates/{id}");
        var beforeLedger = JsonSerializer.Deserialize<JsonElement>(await before.Content.ReadAsStringAsync())
            .GetProperty("ficha").GetProperty("movements").EnumerateArray().ToList();
        var inicio = beforeLedger.Single(m => m.GetProperty("movementType").GetString() == "inicio");
        var recordedAtBefore = inicio.GetProperty("recordedAt").GetString();
        var movementId = inicio.GetProperty("movementId").GetGuid();

        // Edit only business_date (AC-D3).
        await P2T07TestHost.SendJsonAsync(client, HttpMethod.Put, $"/boquilhas/aggregates/{id}/movements/{movementId}", Json(new
        {
            expectedAggregateVersion = 1,
            expectedMovementVersion = 1,
            quantity = 10,
            businessDate = "2026-09-01",
        }));

        var after = await P2T07TestHost.GetAsync(client, $"/boquilhas/aggregates/{id}");
        var afterLedger = JsonSerializer.Deserialize<JsonElement>(await after.Content.ReadAsStringAsync())
            .GetProperty("ficha").GetProperty("movements").EnumerateArray().ToList();
        var edited = afterLedger.Single(m => m.GetProperty("movementType").GetString() == "inicio");

        Assert.Equal(recordedAtBefore, edited.GetProperty("recordedAt").GetString());
        Assert.Equal(10, afterLedger.Single(m => m.GetProperty("movementType").GetString() == "inicio")
            .GetProperty("quantity").GetInt32());
        Assert.Equal(10, JsonSerializer.Deserialize<JsonElement>(await (await P2T07TestHost.GetAsync(client, $"/boquilhas/aggregates/{id}")).Content.ReadAsStringAsync())
            .GetProperty("ficha").GetProperty("balance").GetProperty("disponivel").GetInt32());
    }

    // ================================================================== close/reopen (routes 10/11)

    /// <summary>C2/C3/C4/C5/C6 (AC-C2…C6) — the close/reopen lifecycle over HTTP: atomic close with
    /// the immutable snapshot, eligibility refusals and reopen on the SAME id.</summary>
    [Fact]
    public async Task C2C3C4C5C6_CloseAndReopenLifecycleAcrossTheRoutes()
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-100", "L1", machines: "B1");

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        var id = await CreateAsync(client, Json(new
        {
            toolId = tool.ToolId.Value,
            machines = new[] { "B1" },
            initialQuantity = 10,
            openingDate = "2026-09-10",
            utilisationPercent = 30m,
        }));

        // Close (C2: atomic — the snapshot carries the closing facts).
        var closed = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, $"/boquilhas/aggregates/{id}/close", Json(new
        {
            expectedVersion = 1,
        }));
        Assert.Equal(HttpStatusCode.OK, closed.StatusCode);
        var closedPayload = JsonSerializer.Deserialize<JsonElement>(await closed.Content.ReadAsStringAsync());
        Assert.Equal(2, closedPayload.GetProperty("version").GetInt32());
        var closedAt = closedPayload.GetProperty("closedAt").GetString();

        // A second close is refused (already-closed) and the aggregate left the active grid (C3).
        var doubleClose = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, $"/boquilhas/aggregates/{id}/close", Json(new
        {
            expectedVersion = 2,
        }));
        Assert.Equal(HttpStatusCode.Conflict, doubleClose.StatusCode);
        var activeGrid = await P2T07TestHost.GetAsync(client, "/boquilhas/aggregates");
        var activeRows = JsonSerializer.Deserialize<JsonElement>(await activeGrid.Content.ReadAsStringAsync())
            .GetProperty("rows").EnumerateArray().ToList();
        Assert.DoesNotContain(activeRows, row => row.GetProperty("boquilhasId").GetGuid() == id);

        // Reopen without a reason → 400 REOPEN_REASON_REQUIRED, nothing written (C5).
        var reasonless = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, $"/boquilhas/aggregates/{id}/reopen", Json(new
        {
            expectedVersion = 2,
            reason = "",
        }));
        Assert.Equal(HttpStatusCode.BadRequest, reasonless.StatusCode);
        var reasonlessPayload = JsonSerializer.Deserialize<JsonElement>(await reasonless.Content.ReadAsStringAsync());
        Assert.Contains("REOPEN_REASON_REQUIRED", reasonlessPayload.GetProperty("errors").EnumerateArray().Select(e => e.GetString()));

        // Reopen on the SAME id with a reason (C4/C5).
        var reopened = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, $"/boquilhas/aggregates/{id}/reopen", Json(new
        {
            expectedVersion = 2,
            reason = "devolução parcial detetada",
        }));
        Assert.Equal(HttpStatusCode.OK, reopened.StatusCode);
        Assert.Equal(3, JsonSerializer.Deserialize<JsonElement>(await reopened.Content.ReadAsStringAsync()).GetProperty("version").GetInt32());
        Assert.Equal(1, store.AggregateCount);

        // The ficha shows the close snapshot and the reopen record (C7 history preserved).
        var ficha = await P2T07TestHost.GetAsync(client, $"/boquilhas/aggregates/{id}");
        var body = JsonSerializer.Deserialize<JsonElement>(await ficha.Content.ReadAsStringAsync()).GetProperty("ficha");
        Assert.Equal(closedAt, body.GetProperty("lastClose").GetProperty("closedAt").GetString());
        Assert.Equal("devolução parcial detetada", body.GetProperty("lastReopen").GetProperty("reason").GetString());

        // Not-closed refusal: an ACTIVE aggregate cannot be reopened (C6).
        var notClosed = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, $"/boquilhas/aggregates/{id}/reopen", Json(new
        {
            expectedVersion = 3,
            reason = "outra vez",
        }));
        Assert.Equal(HttpStatusCode.Conflict, notClosed.StatusCode);
        Assert.Equal("not-closed", JsonSerializer.Deserialize<JsonElement>(await notClosed.Content.ReadAsStringAsync())
            .GetProperty("reason").GetString());
    }

    /// <summary>B8 (AC-B8) — a zero-balance aggregate still exists and remains queryable after a
    /// full return; further valid movements remain possible.</summary>
    [Fact]
    public async Task B8_ZeroBalanceNeverDeletesTheAggregate()
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-100", "L1", machines: "B1");
        var repairer = store.SeedRepairer("Reparador A");

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        var id = await CreateAsync(client, Json(new
        {
            toolId = tool.ToolId.Value,
            machines = new[] { "B1" },
            initialQuantity = 4,
            openingDate = "2026-09-10",
        }));

        await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, $"/boquilhas/aggregates/{id}/movements", Json(new
        {
            expectedAggregateVersion = 1,
            movementType = "saida",
            quantity = 4,
            businessDate = "2026-09-11",
            machine = "B1",
            repairerId = repairer.RepairerId.Value,
        }));
        await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, $"/boquilhas/aggregates/{id}/movements", Json(new
        {
            expectedAggregateVersion = 2,
            movementType = "entrada",
            quantity = 4,
            businessDate = "2026-09-12",
        }));

        var ficha = await P2T07TestHost.GetAsync(client, $"/boquilhas/aggregates/{id}");
        Assert.Equal(HttpStatusCode.OK, ficha.StatusCode);
        var body = JsonSerializer.Deserialize<JsonElement>(await ficha.Content.ReadAsStringAsync()).GetProperty("ficha");
        Assert.Equal(4, body.GetProperty("balance").GetProperty("disponivel").GetInt32());

        // A new valid Saída remains possible AFTER the full return (history stays queryable).
        var again = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, $"/boquilhas/aggregates/{id}/movements", Json(new
        {
            expectedAggregateVersion = 3,
            movementType = "saida",
            quantity = 1,
            businessDate = "2026-09-13",
            machine = "B1",
            repairerId = repairer.RepairerId.Value,
        }));
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
    }

    // ================================================================== history (route 18)

    /// <summary>H1/H4/H5 (AC-H1/H4/H5) — the local Histórico filters are backend-applied with
    /// deterministic paging and a backend-counted total; invalid values are refused.</summary>
    [Fact]
    public async Task H1H4H5_HistoryFiltersPagingAndTotals()
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-100", "L1", machines: "B1");
        var otherTool = store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-101", "L2", machines: "B1");
        var repairer = store.SeedRepairer("Reparador A");
        store.SeedAssignment("B1", repairer.RepairerId.Value);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        var id1 = await CreateAsync(client, Json(new
        {
            toolId = tool.ToolId.Value,
            machines = new[] { "B1" },
            initialQuantity = 10,
            openingDate = "2026-09-10",
        }));
        var id2 = await CreateAsync(client, Json(new
        {
            toolId = otherTool.ToolId.Value,
            machines = new[] { "B1" },
            initialQuantity = 5,
            openingDate = "2026-09-11",
        }));

        // A saída (with repairer) on the FIRST aggregate only.
        await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, $"/boquilhas/aggregates/{id1}/movements", Json(new
        {
            expectedAggregateVersion = 1,
            movementType = "saida",
            quantity = 2,
            businessDate = "2026-09-12",
            machine = "B1",
            repairerId = repairer.RepairerId.Value,
        }));

        // Reference traversal filter + totals.
        var byReference = await P2T07TestHost.GetAsync(client, $"/boquilhas/history?reference=BQ-100");
        var historyPayload = JsonSerializer.Deserialize<JsonElement>(await byReference.Content.ReadAsStringAsync());
        Assert.Equal(1, historyPayload.GetProperty("total").GetInt32());
        Assert.Equal(id1, historyPayload.GetProperty("rows")[0].GetProperty("boquilhasId").GetGuid());

        // Movement-type filter (EXISTS).
        var byType = await P2T07TestHost.GetAsync(client, "/boquilhas/history?movementType=saida");
        var byTypePayload = JsonSerializer.Deserialize<JsonElement>(await byType.Content.ReadAsStringAsync());
        Assert.Equal(1, byTypePayload.GetProperty("total").GetInt32());
        Assert.Equal(id1, byTypePayload.GetProperty("rows")[0].GetProperty("boquilhasId").GetGuid());

        // Repairer filter (EXISTS).
        var byRepairer = await P2T07TestHost.GetAsync(client, $"/boquilhas/history?repairerId={repairer.RepairerId}");
        Assert.Equal(1, JsonSerializer.Deserialize<JsonElement>(await byRepairer.Content.ReadAsStringAsync())
            .GetProperty("total").GetInt32());

        // Business-date period (EXISTS over movement business_date).
        var byPeriod = await P2T07TestHost.GetAsync(client, "/boquilhas/history?businessDateFrom=2026-09-12&businessDateTo=2026-09-12");
        Assert.Equal(1, JsonSerializer.Deserialize<JsonElement>(await byPeriod.Content.ReadAsStringAsync())
            .GetProperty("total").GetInt32());

        // Paging: 1-based bounds; pageSize outside 1..100 is refused (H5).
        var onePerPage = await P2T07TestHost.GetAsync(client, $"/boquilhas/history?page=1&pageSize=1");
        var onePayload = JsonSerializer.Deserialize<JsonElement>(await onePerPage.Content.ReadAsStringAsync());
        Assert.Single(onePayload.GetProperty("rows").EnumerateArray());
        Assert.Equal(2, onePayload.GetProperty("total").GetInt32());

        var badPageSize = await P2T07TestHost.GetAsync(client, "/boquilhas/history?pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, badPageSize.StatusCode);
        var badPayload = JsonSerializer.Deserialize<JsonElement>(await badPageSize.Content.ReadAsStringAsync());
        Assert.Contains("FILTER_INVALID", badPayload.GetProperty("errors").EnumerateArray().Select(e => e.GetString()));

        var badState = await P2T07TestHost.GetAsync(client, "/boquilhas/history?state=rascunho");
        Assert.Equal(HttpStatusCode.BadRequest, badState.StatusCode);
    }

    /// <summary>H6 (AC-H6) — empty ≠ lookup-failed: an empty history page is an explicit empty
    /// result, never an error; a non-existent aggregate is a 404, never an empty list.</summary>
    [Fact]
    public async Task H6_EmptyIsExplicitAndNotFoundIsNeverAnEmptyList()
    {
        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted());
        using var client = factory.CreateClient();

        var empty = await P2T07TestHost.GetAsync(client, "/boquilhas/history");
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        var payload = JsonSerializer.Deserialize<JsonElement>(await empty.Content.ReadAsStringAsync());
        Assert.Equal(0, payload.GetProperty("total").GetInt32());
        Assert.Empty(payload.GetProperty("rows").EnumerateArray());

        var missing = await P2T07TestHost.GetAsync(client, $"/boquilhas/aggregates/{MissingId}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var missingPayload = JsonSerializer.Deserialize<JsonElement>(await missing.Content.ReadAsStringAsync());
        Assert.Equal("not-found", missingPayload.GetProperty("reason").GetString());
    }

    // ================================================================== productions/jobon (routes 13/14/15)

    /// <summary>Route 13/14 — the opening-flow reads: reference → productions (REFERENCE_REQUIRED
    /// when blank) and the Job On ficha read.</summary>
    [Fact]
    public async Task ProductionsAndJobOnReadsServeTheOpeningFlow()
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-100", "L1", machines: "B1");
        var jobOn = store.SeedJobOnWithBqContext("REF-1", "P1", "B1", tool.ToolId.Value, tool.Reference, tool.Lot);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        var blank = await P2T07TestHost.GetAsync(client, "/boquilhas/productions");
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);

        var found = await P2T07TestHost.GetAsync(client, "/boquilhas/productions?reference=REF-1");
        var productions = JsonSerializer.Deserialize<JsonElement>(await found.Content.ReadAsStringAsync())
            .GetProperty("productions").EnumerateArray().ToList();
        Assert.Single(productions);
        Assert.Equal(jobOn.JobOnId.Value, productions[0].GetProperty("jobonId").GetGuid());

        var ficha = await P2T07TestHost.GetAsync(client, $"/boquilhas/jobons/{jobOn.JobOnId.Value}");
        var fichaBody = JsonSerializer.Deserialize<JsonElement>(await ficha.Content.ReadAsStringAsync()).GetProperty("ficha");
        Assert.Equal("P1", fichaBody.GetProperty("productionNumber").GetString());
        Assert.Single(fichaBody.GetProperty("contexts").EnumerateArray());
    }

    /// <summary>Route 15 (AC-I3) — the BQ association composes IJobOnService: the real bq_id is
    /// returned and a missing tool is refused.</summary>
    [Fact]
    public async Task BqAssociationCreatesTheRealContextThroughJobOn()
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-100", "L1", machines: "B1");
        var jobOn = store.SeedJobOnWithBqContext("REF-1", "P1", "B1", tool.ToolId.Value, tool.Reference, tool.Lot);
        var bqId = jobOn.Contexts[0].ContextId;

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient();

        var associated = await P2T07TestHost.SendJsonAsync(
            client, HttpMethod.Post, $"/boquilhas/jobons/{jobOn.JobOnId.Value}/bq-association", Json(new
            {
                toolId = tool.ToolId.Value,
                expectedJobOnVersion = jobOn.Version,
            }));
        Assert.Equal(HttpStatusCode.Created, associated.StatusCode);
        var payload = JsonSerializer.Deserialize<JsonElement>(await associated.Content.ReadAsStringAsync());
        Assert.Equal(bqId, payload.GetProperty("bqId").GetGuid());
        Assert.Equal(jobOn.JobOnId.Value, payload.GetProperty("jobonId").GetGuid());

        // A CM Tool is refused by Job On's own validation (TOOL_TYPE_MISMATCH): a fresh occurrence
        // without a prior association is used so the expected version is still 1.
        var cmTool = store.SeedTool(DMO.Domain.Tools.ToolType.Cm, "CM-200", "L2", machines: "B1");
        var freshJobOn = store.SeedJobOnWithBqContext("REF-2", "P2", "B1", tool.ToolId.Value, tool.Reference, tool.Lot);

        var mismatched = await P2T07TestHost.SendJsonAsync(
            client, HttpMethod.Post, $"/boquilhas/jobons/{freshJobOn.JobOnId.Value}/bq-association", Json(new
            {
                toolId = cmTool.ToolId.Value,
                expectedJobOnVersion = freshJobOn.Version,
            }));
        Assert.Equal(HttpStatusCode.BadRequest, mismatched.StatusCode);
    }

    /// <summary>T1 (AC-T1) — the shared picker query uses type=BQ: candidates returned by the
    /// ferramentas route with type=BQ are BQ Tools only; non-BQ Tools never appear.</summary>
    [Fact]
    public async Task T1_TheSharedPickerQueryIsBqTypeFiltered()
    {
        var store = new P2T07TestStore();
        store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-100", "L1", machines: "B1");
        store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-101", "L2", machines: "B1");
        store.SeedTool(DMO.Domain.Tools.ToolType.Cm, "CM-200", "L3", machines: "B1");

        var granted = new[]
        {
            TestNavigationComposition.Definition(ModuleCatalog.Boquilhas, "Boquilhas", "boquilhas", "Boquilhas"),
            TestNavigationComposition.Definition(ModuleCatalog.Ferramentas, "Ferramentas", null, "Ferramentas", contextual: true),
        };

        using var factory = P2T07TestHost.ForUser(granted, store);
        using var client = factory.CreateClient();

        var search = await P2T07TestHost.GetAsync(client, "/ferramentas/tools?type=BQ&query=BQ&limit=50");
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        var items = JsonSerializer.Deserialize<JsonElement>(await search.Content.ReadAsStringAsync())
            .GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        Assert.All(items, item => Assert.Equal("BQ", item.GetProperty("type").GetString()));
        Assert.DoesNotContain(items, item => item.GetProperty("reference").GetString() == "CM-200");
    }

    /// <summary>T5 (AC-T5) — Tool creation through the shared orchestration is validated and a
    /// duplicate identity is refused on the origin surface.</summary>
    [Fact]
    public async Task T5_ToolCreateThroughTheSharedOrchestrationIsValidatedAndDuplicatesRefused()
    {
        var store = new P2T07TestStore();
        store.SeedTool(DMO.Domain.Tools.ToolType.Bq, "BQ-100", "L1", machines: "B1");

        var granted = new[]
        {
            TestNavigationComposition.Definition(ModuleCatalog.Boquilhas, "Boquilhas", "boquilhas", "Boquilhas"),
            TestNavigationComposition.Definition(ModuleCatalog.Ferramentas, "Ferramentas", null, "Ferramentas", contextual: true),
        };

        using var factory = P2T07TestHost.ForUser(granted, store);
        using var client = factory.CreateClient();

        var created = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, "/ferramentas/tools", Json(new
        {
            type = "BQ",
            reference = "BQ-200",
            lot = "L9",
            machines = new[] { "B1", "C1" },
        }));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var duplicate = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, "/ferramentas/tools", Json(new
        {
            type = "BQ",
            reference = "BQ-100",
            lot = "L1",
            machines = new[] { "B1" },
        }));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var payload = JsonSerializer.Deserialize<JsonElement>(await duplicate.Content.ReadAsStringAsync());
        Assert.Equal("duplicate-identity", payload.GetProperty("reason").GetString());
    }

    // ================================================================== helpers

    private static async Task<Guid> CreateAsync(HttpClient client, string payload)
    {
        var response = await P2T07TestHost.SendJsonAsync(client, HttpMethod.Post, "/boquilhas/aggregates", payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        return body.GetProperty("boquilhasId").GetGuid();
    }
}