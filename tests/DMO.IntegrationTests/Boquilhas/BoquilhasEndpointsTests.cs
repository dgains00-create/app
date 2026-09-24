using System.Net;
using System.Net.Http.Json;
using DMO.Application.Boquilhas;
using DMO.Domain.Boquilhas;
using DMO.Domain.Tools;
using DMO.IntegrationTests.JobOn;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace DMO.IntegrationTests.Boquilhas;

/// <summary>
/// P2-T07 endpoint tests (OWNER CLARIFICATION): the critical executable coverage of the
/// production movement register — production association, movements after the production end
/// date, the three-type vocabulary, the derived outstanding replay, the Entrada sem reparação
/// semantics, the edit/audit single-event rule, the repairer historical preservation, the local
/// Histórico and the register identity creation WITHOUT a quantity event.
/// </summary>
public sealed class BoquilhasEndpointsTests
{
    // ------------------------------------------------------------------ arrangement

    private static (P2T07TestStore Store, Guid JobOnId, Guid BqId) ArrangeProduction(
        P2T07TestStore store,
        string reference = "REF-1",
        string production = "P1",
        string machine = "B1",
        DateOnly? productionDate = null)
    {
        var tool = store.SeedTool(ToolType.Bq, "5447T173", "LOTE-1");
        var (jobOnId, bqId) = store.SeedProductionWithBq(
            reference, production, tool.ToolId.Value, machine, productionDate);
        return (store, jobOnId, bqId);
    }

    private static Guid SeedRepairer(P2T07TestStore store, string name = "Reparador Externo A") =>
        store.SeedRepairer(name).RepairerId.Value;

    // 1. production association — every register belongs to a REAL Job On/BQ context ----------

    [Fact]
    public async Task T1_RegisterCreationAnchorsARealBqContext()
    {
        var store = new P2T07TestStore();
        var (_, _, bqId) = ArrangeProduction(store);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var create = await P2T07TestHost.SendJsonAsync(
            client, HttpMethod.Post, "/boquilhas/registers", P2T07TestHost.Json(new { bqId }));

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = await create.Content.ReadFromJsonAsync<RegisterCreatedResponse>();
        Assert.NotNull(created?.BoquilhasId);
        Assert.NotEqual(Guid.Empty, created.BoquilhasId);

        // The register belongs to the REAL production: the ficha resolves the context.
        using var ficha = await P2T07TestHost.GetAsync(
            client, $"/boquilhas/registers/{created.BoquilhasId}");

        Assert.Equal(HttpStatusCode.OK, ficha.StatusCode);
        var payload = await ficha.Content.ReadFromJsonAsync<FichaEnvelope>();
        Assert.NotNull(payload?.Ficha);
        Assert.Equal(bqId, payload.Ficha.BqId);
        Assert.NotNull(payload.Ficha.Production);
        Assert.Equal("P1", payload.Ficha.Production.ProductionNumber);
    }

    [Fact]
    public async Task T2_RegisterCreationWithAFakeBqId_IsRefused()
    {
        var store = new P2T07TestStore();

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var create = await P2T07TestHost.SendJsonAsync(
            client, HttpMethod.Post, "/boquilhas/registers",
            P2T07TestHost.Json(new { bqId = Guid.NewGuid() }));

        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        var payload = await create.Content.ReadFromJsonAsync<ValidationEnvelope>();
        Assert.Contains(BoquilhasValidationErrors.BqContextNotFound, payload?.Errors ?? []);
        Assert.Equal(0, store.RegisterCount);
    }

    [Fact]
    public async Task T3_RegisterCreation_DoesNotManufactureAMovement()
    {
        var store = new P2T07TestStore();
        var (_, _, bqId) = ArrangeProduction(store);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var create = await P2T07TestHost.SendJsonAsync(
            client, HttpMethod.Post, "/boquilhas/registers", P2T07TestHost.Json(new { bqId }));
        var created = await create.Content.ReadFromJsonAsync<RegisterCreatedResponse>();

        using var ficha = await P2T07TestHost.GetAsync(
            client, $"/boquilhas/registers/{created!.BoquilhasId}");

        var payload = await ficha.Content.ReadFromJsonAsync<FichaEnvelope>();
        Assert.NotNull(payload?.Ficha);
        Assert.Equal(0, payload.Ficha.Outstanding);
        Assert.Empty(payload.Ficha.Movements);
    }

    [Fact]
    public async Task T4_RegisterCreation_OneRegisterPerBqContext()
    {
        var store = new P2T07TestStore();
        var (_, _, bqId) = ArrangeProduction(store);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var first = await P2T07TestHost.SendJsonAsync(
            client, HttpMethod.Post, "/boquilhas/registers", P2T07TestHost.Json(new { bqId }));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        using var second = await P2T07TestHost.SendJsonAsync(
            client, HttpMethod.Post, "/boquilhas/registers", P2T07TestHost.Json(new { bqId }));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var refusal = await second.Content.ReadFromJsonAsync<RefusalEnvelope>();
        Assert.Equal("register-exists", refusal?.Reason);
    }

    // 2. production-ended case — movement recorded after the production end date --------------

    [Fact]
    public async Task T5_AProductionEndedOn09027_StillAcceptsAMovementOn09029()
    {
        var store = new P2T07TestStore();
        var (_, _, bqId) = ArrangeProduction(
            store, productionDate: new DateOnly(2026, 9, 27));
        var repairerId = SeedRepairer(store);
        var register = store.SeedRegister(bqId, (MovementKind.Saida, 10, new DateOnly(2026, 9, 25), "B1", repairerId));

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // 29/09 — AFTER the production ended on 27/09: perfectly valid.
        using var append = await P2T07TestHost.SendJsonAsync(
            client,
            HttpMethod.Post,
            $"/boquilhas/registers/{register.BoquilhasId.Value}/movements",
            P2T07TestHost.Json(new
            {
                movementType = "entrada_sem_reparacao",
                quantity = 6,
                businessDate = "2026-09-29",
            }));

        Assert.Equal(HttpStatusCode.Created, append.StatusCode);

        using var ficha = await P2T07TestHost.GetAsync(
            client, $"/boquilhas/registers/{register.BoquilhasId.Value}");

        var payload = await ficha.Content.ReadFromJsonAsync<FichaEnvelope>();
        Assert.NotNull(payload?.Ficha);
        Assert.Equal(2, payload.Ficha.Movements.Count);
        Assert.Equal(4, payload.Ficha.Outstanding);
        // The production stays the historical context.
        Assert.Equal("P1", payload.Ficha.Production?.ProductionNumber);
        Assert.Equal(new DateOnly(2026, 9, 27), payload.Ficha.Production?.ProductionDate);
    }

    // 3. movement vocabulary — Saída / Entrada / Entrada sem reparação; no Início, no Irreparável

    [Fact]
    public async Task T6_TheThreeMovementTypes_AreAcceptedAndDistinct()
    {
        var store = new P2T07TestStore();
        var (_, _, bqId) = ArrangeProduction(store);
        var repairerId = SeedRepairer(store);
        var register = store.SeedRegister(bqId);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Saída (machine + repairer required), Entrada, Entrada sem reparação.
        var saida = await AppendAsync(client, register, "saida", 10, "2026-09-25", "B1", repairerId);
        var entrada = await AppendAsync(client, register, "entrada", 4, "2026-09-27", null, null);
        var semReparacao = await AppendAsync(client, register, "entrada_sem_reparacao", 6, "2026-09-29", null, null);

        Assert.Equal(HttpStatusCode.Created, saida.StatusCode);
        Assert.Equal(HttpStatusCode.Created, entrada.StatusCode);
        Assert.Equal(HttpStatusCode.Created, semReparacao.StatusCode);

        using var ficha = await P2T07TestHost.GetAsync(
            client, $"/boquilhas/registers/{register.BoquilhasId.Value}");

        var payload = await ficha.Content.ReadFromJsonAsync<FichaEnvelope>();
        Assert.NotNull(payload?.Ficha);
        Assert.Equal(
            ["saida", "entrada", "entrada_sem_reparacao"],
            payload.Ficha.Movements.Select(movement => movement.MovementType));
    }

    [Theory]
    [InlineData("inicio")]
    [InlineData("irreparavel")]
    [InlineData("editar")]
    public async Task T7_SupersededMovementTypes_AreRefused(string supersededType)
    {
        var store = new P2T07TestStore();
        var (_, _, bqId) = ArrangeProduction(store);
        var register = store.SeedRegister(bqId);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await AppendAsync(
            client, register, supersededType, 1, "2026-09-25", null, null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ValidationEnvelope>();
        Assert.Contains(BoquilhasValidationErrors.MovementTypeInvalid, payload?.Errors ?? []);
    }

    [Fact]
    public async Task T8_SaidaRequiresMachineAndRepairer()
    {
        var store = new P2T07TestStore();
        var (_, _, bqId) = ArrangeProduction(store);
        var register = store.SeedRegister(bqId);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var payload = new
        {
            movementType = "saida",
            quantity = 10,
            businessDate = "2026-09-25",
        };

        using var noFacts = await P2T07TestHost.SendJsonAsync(
            client, HttpMethod.Post, $"/boquilhas/registers/{register.BoquilhasId.Value}/movements",
            P2T07TestHost.Json(payload));

        Assert.Equal(HttpStatusCode.BadRequest, noFacts.StatusCode);
        var errors = await noFacts.Content.ReadFromJsonAsync<ValidationEnvelope>();
        Assert.Contains(BoquilhasValidationErrors.MachineRequired, errors?.Errors ?? []);
        Assert.Contains(BoquilhasValidationErrors.RepairerRequired, errors?.Errors ?? []);
        Assert.Equal(0, store.GetByIdAsync(register.BoquilhasId.Value, CancellationToken.None).Result?.Movements.Count);
    }

    // 4. quantity replay — Saída 10 → Entrada 4 → Entrada sem reparação 6 → outstanding 0 -------

    [Fact]
    public async Task T9_Replay_OwnerExample_EndsAtZero()
    {
        var store = new P2T07TestStore();
        var (_, _, bqId) = ArrangeProduction(store);
        var repairerId = SeedRepairer(store);
        var register = store.SeedRegister(bqId);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await AppendAsync(client, register, "saida", 10, "2026-09-25", "B1", repairerId);
        await AppendAsync(client, register, "entrada", 4, "2026-09-27", null, null);
        await AppendAsync(client, register, "entrada_sem_reparacao", 6, "2026-09-29", null, null);

        using var ficha = await P2T07TestHost.GetAsync(
            client, $"/boquilhas/registers/{register.BoquilhasId.Value}");

        var payload = await ficha.Content.ReadFromJsonAsync<FichaEnvelope>();
        Assert.NotNull(payload?.Ficha);
        Assert.Equal(0, payload.Ficha.Outstanding);
        Assert.Equal(0, payload.Ficha.Movements[^1].Saldo);
    }

    [Fact]
    public async Task T10_NegativeOutstanding_IsVisibleAndNonBlocking()
    {
        var store = new P2T07TestStore();
        var (_, _, bqId) = ArrangeProduction(store);
        var repairerId = SeedRepairer(store);
        var register = store.SeedRegister(bqId);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await AppendAsync(client, register, "saida", 2, "2026-09-25", "B1", repairerId);
        using var entrada = await AppendAsync(client, register, "entrada", 5, "2026-09-27", null, null);

        Assert.Equal(HttpStatusCode.Created, entrada.StatusCode);

        using var ficha = await P2T07TestHost.GetAsync(
            client, $"/boquilhas/registers/{register.BoquilhasId.Value}");

        var payload = await ficha.Content.ReadFromJsonAsync<FichaEnvelope>();
        Assert.NotNull(payload?.Ficha);
        Assert.Equal(-3, payload.Ficha.Outstanding);
    }

    // 5. Entrada sem reparação — returns quantity, explicit in history, no Tool mutation --------

    [Fact]
    public async Task T11_EntradaSemReparacao_IsExplicitInHistory_andDoesNotMutateTheTool()
    {
        var store = new P2T07TestStore();
        var tool = store.SeedTool(ToolType.Bq, "5447T173", "LOTE-1");
        var (_, _, bqId) = ArrangeProduction(store);
        var repairerId = SeedRepairer(store);
        var register = store.SeedRegister(
            bqId,
            (MovementKind.Saida, 6, new DateOnly(2026, 9, 25), "B1", repairerId));

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await AppendAsync(client, register, "entrada_sem_reparacao", 6, "2026-09-29", null, null);

        // History shows the exact movement with its distinct meaning (token preserved).
        using var history = await P2T07TestHost.GetAsync(client, "/boquilhas/history?movementType=entrada_sem_reparacao");
        var historyPayload = await history.Content.ReadFromJsonAsync<HistoryEnvelope>();
        var row = Assert.Single(historyPayload?.Rows ?? []);
        Assert.Equal("entrada_sem_reparacao", row.MovementType);
        Assert.Equal(6, row.Quantity);
        // The reference is the frozen BQ triple reference of the REAL context (the Tool identity).
        Assert.Equal("5447T173", row.Reference);

        // The Tool is NOT marked irreparable, NOT separated, NOT destroyed: it still resolves.
        var canonical = await store.JobOnToolStore
            .GetByIdAsync(tool.ToolId.Value, CancellationToken.None);
        Assert.NotNull(canonical);
        Assert.Equal(ToolType.Bq, canonical.Type);
    }

    // 6. edit — same movement_id, no double-count, audit preserved -----------------------------

    [Fact]
    public async Task T12_Edit_KeepsTheSameMovementId_NoDoubleCount_AuditPreserved()
    {
        var store = new P2T07TestStore();
        var (_, _, bqId) = ArrangeProduction(store);
        var repairerId = SeedRepairer(store);
        var register = store.SeedRegister(bqId);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var append = await AppendAsync(client, register, "saida", 10, "2026-09-25", "B1", repairerId);
        var appended = await append.Content.ReadFromJsonAsync<MovementAppliedEnvelope>();
        var movementId = appended!.MovementId;

        using var edit = await P2T07TestHost.SendJsonAsync(
            client,
            HttpMethod.Put,
            $"/boquilhas/registers/{register.BoquilhasId.Value}/movements/{movementId}",
            P2T07TestHost.Json(new
            {
                expectedMovementVersion = 1,
                quantity = 7,
                businessDate = "2026-09-26",
                machine = "B1",
                repairerId,
            }));

        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);

        using var ficha = await P2T07TestHost.GetAsync(
            client, $"/boquilhas/registers/{register.BoquilhasId.Value}");

        var payload = await ficha.Content.ReadFromJsonAsync<FichaEnvelope>();
        Assert.NotNull(payload?.Ficha);

        // The SAME movement row was replaced: count unchanged, id unchanged, type immutable.
        Assert.Single(payload.Ficha.Movements);
        Assert.Equal(movementId, payload.Ficha.Movements[0].MovementId);
        Assert.Equal("saida", payload.Ficha.Movements[0].MovementType);
        Assert.Equal(7, payload.Ficha.Movements[0].Quantity);
        Assert.Equal(2, payload.Ficha.Movements[0].Version);

        // One quantity event only: the outstanding reflects the single edited movement.
        Assert.Equal(7, payload.Ficha.Outstanding);

        // The audit trail holds the exact before/after facts.
        using var audit = await P2T07TestHost.GetAsync(
            client, $"/boquilhas/registers/{register.BoquilhasId.Value}/movements/{movementId}/audit");
        var auditPayload = await audit.Content.ReadFromJsonAsync<AuditEnvelope>();
        Assert.NotNull(auditPayload?.Entries);
        var entry = Assert.Single(auditPayload.Entries);
        Assert.Equal(10, entry.BeforeQuantity);
        Assert.Equal(7, entry.AfterQuantity);
        Assert.Equal("B1", entry.BeforeMachine);
        Assert.Equal("B1", entry.AfterMachine);
    }

    [Fact]
    public async Task T13_EditWithAStaleMovementVersion_IsRefused_AndNothingIsWritten()
    {
        var store = new P2T07TestStore();
        var (_, _, bqId) = ArrangeProduction(store);
        var repairerId = SeedRepairer(store);
        var register = store.SeedRegister(bqId);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var append = await AppendAsync(client, register, "saida", 10, "2026-09-25", "B1", repairerId);
        var appended = await append.Content.ReadFromJsonAsync<MovementAppliedEnvelope>();

        using var stale = await P2T07TestHost.SendJsonAsync(
            client,
            HttpMethod.Put,
            $"/boquilhas/registers/{register.BoquilhasId.Value}/movements/{appended!.MovementId}",
            P2T07TestHost.Json(new
            {
                expectedMovementVersion = 42,
                quantity = 1,
                businessDate = "2026-09-26",
                machine = "B1",
                repairerId,
            }));

        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var refusal = await stale.Content.ReadFromJsonAsync<RefusalEnvelope>();
        Assert.Equal("stale-version", refusal?.Reason);

        // Nothing was written: the ledger still holds the original single movement at version 1.
        var current = store.GetByIdAsync(register.BoquilhasId.Value, CancellationToken.None).Result!;
        Assert.Single(current.Movements);
        Assert.Equal(10, current.Movements[0].Quantity);
        Assert.Equal(1, current.Movements[0].Version);
    }

    // 7. repairer history — a later assignment change does not rewrite the movement -------------

    [Fact]
    public async Task T14_LaterAssignmentChange_DoesNotRewriteTheMovementRepairer()
    {
        var store = new P2T07TestStore();
        var (_, _, bqId) = ArrangeProduction(store);
        var firstRepairer = SeedRepairer(store, "Reparador A");
        var secondRepairer = SeedRepairer(store, "Reparador B");
        store.SeedAssignment("B1", firstRepairer);

        var register = store.SeedRegister(
            bqId,
            (MovementKind.Saida, 10, new DateOnly(2026, 9, 25), "B1", firstRepairer));

        // A LATER assignment change happens (arranged after the movement was recorded).
        store.ChangeAssignment("B1", secondRepairer);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var ficha = await P2T07TestHost.GetAsync(
            client, $"/boquilhas/registers/{register.BoquilhasId.Value}");

        var payload = await ficha.Content.ReadFromJsonAsync<FichaEnvelope>();
        Assert.NotNull(payload?.Ficha);

        // The movement still carries the repairer used AT THE TIME of the movement.
        Assert.Equal(firstRepairer, payload.Ficha.Movements[0].RepairerId);
    }

    // 8. local Histórico — movement-level chronological history with filters --------------------

    [Fact]
    public async Task T15_History_IsMovementLevel_Chronological_WithTheProductionContext()
    {
        var store = new P2T07TestStore();
        var (_, _, bqId) = ArrangeProduction(store);
        var repairerId = SeedRepairer(store);
        var register = store.SeedRegister(
            bqId,
            (MovementKind.Saida, 10, new DateOnly(2026, 9, 25), "B1", repairerId),
            (MovementKind.Entrada, 4, new DateOnly(2026, 9, 27), null, null));

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var history = await P2T07TestHost.GetAsync(client, "/boquilhas/history");
        var payload = await history.Content.ReadFromJsonAsync<HistoryEnvelope>();

        Assert.NotNull(payload);
        Assert.Equal(2, payload.Total);
        Assert.Equal(2, payload.Rows.Count);
        Assert.Equal("saida", payload.Rows[0].MovementType);
        Assert.Equal("entrada", payload.Rows[1].MovementType);
        // The reference/lot are the frozen BQ triple of the REAL context (the Tool identity).
        Assert.Equal("5447T173", payload.Rows[0].Reference);
        Assert.Equal("P1", payload.Rows[0].ProductionNumber);

        // Unknown filter values are refused (never a silent full list).
        using var invalid = await P2T07TestHost.GetAsync(
            client, "/boquilhas/history?movementType=irreparavel");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task T16_RegisterList_RendersTheDerivedOutstanding()
    {
        var store = new P2T07TestStore();
        var (_, _, bqId) = ArrangeProduction(store);
        var repairerId = SeedRepairer(store);
        store.SeedRegister(
            bqId,
            (MovementKind.Saida, 10, new DateOnly(2026, 9, 25), "B1", repairerId));

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var list = await P2T07TestHost.GetAsync(client, "/boquilhas/registers");
        var payload = await list.Content.ReadFromJsonAsync<RegisterListEnvelope>();

        Assert.NotNull(payload);
        Assert.Equal(1, payload.Total);
        Assert.Equal(10, payload.Rows[0].Outstanding);
        // The reference is the frozen BQ triple reference of the REAL context (the Tool identity).
        Assert.Equal("5447T173", payload.Rows[0].Reference);
        Assert.Equal("P1", payload.Rows[0].ProductionNumber);
    }

    // 9. productions / ficha / BQ association (Job On composition) ------------------------------

    [Fact]
    public async Task T17_ProductionsAndFicha_ComposeTheRealJobOnReads()
    {
        var store = new P2T07TestStore();
        var (_, jobOnId, _) = ArrangeProduction(store, reference: "REF-X", production: "PX");

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var productions = await P2T07TestHost.GetAsync(client, "/boquilhas/productions?reference=REF-X");
        var productionsPayload = await productions.Content.ReadFromJsonAsync<ProductionsEnvelope>();
        Assert.Single(productionsPayload?.Productions ?? []);
        Assert.Equal(jobOnId, productionsPayload!.Productions[0].JobonId);

        using var ficha = await P2T07TestHost.GetAsync(client, $"/boquilhas/jobons/{jobOnId}");
        Assert.Equal(HttpStatusCode.OK, ficha.StatusCode);
    }

    [Fact]
    public async Task T18_BqAssociation_CreatesTheContextThroughJobOn_ReturningTheRealBqId()
    {
        var store = new P2T07TestStore();
        // A production WITHOUT a BQ context (standalone-ish start, no context slot at all).
        var jobOn = store.JobOnToolStore.SeedJobOn("REF-BQ", "P-BQ");
        var tool = store.SeedTool(ToolType.Bq, "5447BQ1", "LOTE-BQ");

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var associate = await P2T07TestHost.SendJsonAsync(
            client,
            HttpMethod.Post,
            $"/boquilhas/jobons/{jobOn.JobOnId.Value}/bq-association",
            P2T07TestHost.Json(new { toolId = tool.ToolId.Value, expectedJobOnVersion = 1 }));

        if (associate.StatusCode != HttpStatusCode.Created)
        {
            var body = await associate.Content.ReadAsStringAsync();
            Assert.Fail($"BQ association failed with body: {body}");
        }

        var payload = await associate.Content.ReadFromJsonAsync<BqAssociatedEnvelope>();
        Assert.NotNull(payload?.BqId);

        // The bq_id is REAL: a register can now anchor it.
        using var create = await P2T07TestHost.SendJsonAsync(
            client, HttpMethod.Post, "/boquilhas/registers", P2T07TestHost.Json(new { bqId = payload!.BqId }));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
    }

    // 10. repairer / assignment reads ------------------------------------------------------------

    [Fact]
    public async Task T19_MachineAssignmentsAndRepairers_AreConsumedReads()
    {
        var store = new P2T07TestStore();
        var repairerId = SeedRepairer(store, "Reparador A");
        store.SeedAssignment("B1", repairerId);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var assignments = await P2T07TestHost.GetAsync(client, "/boquilhas/machine-assignments");
        var assignmentsPayload = await assignments.Content.ReadFromJsonAsync<AssignmentsEnvelope>();
        Assert.NotNull(assignmentsPayload?.Assignments);
        Assert.Equal(6, assignmentsPayload.Assignments.Count);

        var b1 = assignmentsPayload.Assignments.Single(row => row.Machine == "B1");
        Assert.Equal(repairerId, b1.RepairerId);
        var unassigned = assignmentsPayload.Assignments.Single(row => row.Machine == "B3");
        Assert.True(unassigned.AssignmentUnavailable);

        using var repairers = await P2T07TestHost.GetAsync(client, "/boquilhas/repairers");
        var repairersPayload = await repairers.Content.ReadFromJsonAsync<RepairersEnvelope>();
        Assert.Single(repairersPayload?.Repairers ?? []);
    }

    [Fact]
    public async Task T20_AppendWithAnUnknownRepairer_IsRefused()
    {
        var store = new P2T07TestStore();
        var (_, _, bqId) = ArrangeProduction(store);
        var register = store.SeedRegister(bqId);

        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted(), store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var append = await AppendAsync(
            client, register, "saida", 10, "2026-09-25", "B1", Guid.NewGuid());

        Assert.Equal(HttpStatusCode.BadRequest, append.StatusCode);
        var payload = await append.Content.ReadFromJsonAsync<ValidationEnvelope>();
        Assert.Contains(BoquilhasValidationErrors.RepairerNotFound, payload?.Errors ?? []);
    }

    // ------------------------------------------------------------------ transport shapes

    private static async Task<HttpResponseMessage> AppendAsync(
        HttpClient client,
        DMO.Domain.Boquilhas.BoquilhaRegister register,
        string movementType,
        int quantity,
        string businessDate,
        string? machine,
        Guid? repairerId) =>
        await P2T07TestHost.SendJsonAsync(
            client,
            HttpMethod.Post,
            $"/boquilhas/registers/{register.BoquilhasId.Value}/movements",
            P2T07TestHost.Json(new
            {
                movementType,
                quantity,
                businessDate,
                machine,
                repairerId,
            }));

    private sealed record RegisterCreatedResponse(Guid BoquilhasId);
    private sealed record FichaEnvelope(FichaResponse? Ficha);
    private sealed record FichaResponse(
        Guid BoquilhasId,
        Guid BqId,
        string? Reference,
        string? Lot,
        int Outstanding,
        Guid CreatedByUserId,
        DateTimeOffset CreatedAt,
        AnchorResponse? Anchor,
        ProductionContextResponse? Production,
        IReadOnlyList<MovementResponse> Movements);
    private sealed record AnchorResponse(
        Guid ToolId,
        string FrozenToolType,
        string FrozenToolReference,
        string FrozenToolLot);
    private sealed record ProductionContextResponse(
        string Reference,
        string ProductionNumber,
        string Machine,
        DateOnly? ProductionDate);
    private sealed record MovementResponse(
        Guid MovementId,
        string MovementType,
        int Quantity,
        DateOnly BusinessDate,
        DateTimeOffset RecordedAt,
        Guid RecordedByUserId,
        string? Machine,
        Guid? RepairerId,
        string? Observations,
        int Version,
        int Saldo);
    private sealed record ValidationEnvelope(string Reason, IReadOnlyList<string> Errors);
    private sealed record RefusalEnvelope(string Reason, string Message);
    private sealed record MovementAppliedEnvelope(Guid MovementId, int Version);
    private sealed record AuditEnvelope(Guid MovementId, IReadOnlyList<AuditEntryResponse> Entries);
    private sealed record AuditEntryResponse(
        Guid MovementAuditId,
        Guid MovementId,
        Guid EditedByUserId,
        DateTimeOffset EditedAt,
        int BeforeQuantity,
        int AfterQuantity,
        DateOnly BeforeBusinessDate,
        DateOnly AfterBusinessDate,
        string? BeforeMachine,
        string? AfterMachine,
        Guid? BeforeRepairerId,
        Guid? AfterRepairerId,
        string? BeforeObservations,
        string? AfterObservations);
    private sealed record HistoryEnvelope(IReadOnlyList<HistoryRowResponse> Rows, int Total);
    private sealed record HistoryRowResponse(
        Guid MovementId,
        Guid BoquilhasId,
        string MovementType,
        int Quantity,
        DateOnly BusinessDate,
        DateTimeOffset RecordedAt,
        Guid RecordedByUserId,
        string? Machine,
        Guid? RepairerId,
        string? Observations,
        string? Reference,
        string? Lot,
        string? ProductionNumber,
        string? ProductionMachine);
    private sealed record RegisterListEnvelope(IReadOnlyList<RegisterRowResponse> Rows, int Total);
    private sealed record RegisterRowResponse(
        Guid BoquilhasId,
        Guid BqId,
        string? Reference,
        string? Lot,
        string? ProductionNumber,
        string? ProductionMachine,
        DateOnly? ProductionDate,
        int Outstanding,
        int MovementCount,
        DateTimeOffset? LastMovementAt);
    private sealed record ProductionsEnvelope(IReadOnlyList<ProductionRowResponse> Productions);
    private sealed record ProductionRowResponse(
        Guid JobonId,
        string Reference,
        string ProductionNumber,
        string Machine,
        DateOnly? ProductionDate);
    private sealed record BqAssociatedEnvelope(Guid JobonId, Guid BqId, int Version);
    private sealed record AssignmentsEnvelope(IReadOnlyList<AssignmentRowResponse> Assignments);
    private sealed record AssignmentRowResponse(
        string Machine,
        Guid? RepairerId,
        string? RepairerName,
        bool AssignmentUnavailable);
    private sealed record RepairersEnvelope(IReadOnlyList<RepairerRowResponse> Repairers);
    private sealed record RepairerRowResponse(Guid RepairerId, string Name);
}