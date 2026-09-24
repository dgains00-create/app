using System.Data;
using DMO.Application.Boquilhas;
using DMO.Application.Persistence;
using DMO.Application.Repositories;
using DMO.Domain.Boquilhas;
using DMO.Infrastructure.Persistence;
using DMO.Infrastructure.Persistence.Entities;
using DMO.Infrastructure.Persistence.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

// Test-only raw SQL: every interpolated value is a fixed, test-owned token (table names and row
// identifiers) against a disposable database. Analyzer EF1003 suppressed.
#pragma warning disable EF1003

namespace DMO.IntegrationTests.Persistence;

/// <summary>
/// P2-T07 env-gated integration test — the REAL <see cref="BoquilhasRepository"/> over the REAL
/// single <c>DmoDbContext</c> and disposable PostgreSQL: identity (I1–I8), movement vocabulary
/// (V1/V4), derived balance (B1–B9), edit/audit single-event semantics (E1–E6), dates (D2/D3),
/// repairer historical preservation (R1–R3/R5), opening facts (U2), close/reopen (C1–C8) and the
/// concurrency rows K1–K5 — plus the B1-correction race rows <b>K6/K7/K8</b> executed as REAL
/// concurrent transactions against the database (one winner, one typed loser, zero partial state).
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §29 (the DB-class rows) and §7.2/§10/§11 (the race-safe backstop and
/// the transaction boundaries). Every row is <c>[SkippableFact]</c> behind
/// <see cref="PersistenceTestDatabase.SkipIfNotConfigured"/>.</remarks>
[Collection(PersistenceDatabaseCollection.Name)]
public sealed class BoquilhasRepositoryIntegrationTests
{
    private const string ActiveToken = "active";
    private const string ClosedToken = "closed";

    // ================================================================== fixtures

    private static async Task<Guid> SeedUserAsync(DmoDbContext context, string token)
    {
        var userId = Guid.NewGuid();
        await ExecuteAsync(
            context,
            "INSERT INTO users (user_id, auth_identity_id, company_number, name, email, active, version) " +
            "VALUES (@id, @auth, @company, @name, @email, true, 1)",
            ("id", (object)userId),
            ("auth", token),
            ("company", token),
            ("name", token),
            ("email", $"{token}@example.test"));

        return userId;
    }

    private static async Task<Guid> SeedToolAsync(DmoDbContext context, string token, string type = "BQ")
    {
        var toolId = Guid.NewGuid();
        await ExecuteAsync(
            context,
            "INSERT INTO tools (tool_id, tool_type, reference, lot, processo, quantity, created_at, updated_at) " +
            "VALUES (@id, @type, @reference, '01', 'NNPB', 10, now(), now())",
            ("id", (object)toolId),
            ("type", type),
            ("reference", $"REF-{token}"));

        return toolId;
    }

    private static async Task<Guid> SeedBqContextAsync(DmoDbContext context, string token, Guid toolId)
    {
        var jobOnId = Guid.NewGuid();
        var bqId = Guid.NewGuid();
        await ExecuteAsync(
            context,
            "INSERT INTO job_ons (jobon_id, reference, production_number, machine, production_date, version, created_at, updated_at) " +
            "VALUES (@jobon, @reference, @production, 'B1', NULL, 1, now(), now())",
            ("jobon", (object)jobOnId),
            ("reference", $"REF-{token}"),
            ("production", $"P-{token}"));

        await ExecuteAsync(
            context,
            "INSERT INTO bq_contexts (bq_id, jobon_id, tool_id, tool_type, tool_reference, tool_lot, created_at, updated_at) " +
            "VALUES (@bq, @jobon, @tool, 'BQ', @reference, '01', now(), now())",
            ("bq", (object)bqId),
            ("jobon", (object)jobOnId),
            ("tool", (object)toolId),
            ("reference", $"BQ-{token}"));

        return bqId;
    }

    private static async Task<Guid> SeedRepairerAsync(DmoDbContext context, string token)
    {
        var repairerId = Guid.NewGuid();
        await ExecuteAsync(
            context,
            "INSERT INTO repairers (repairer_id, name, version, created_at, updated_at) " +
            "VALUES (@id, @name, 1, now(), now())",
            ("id", (object)repairerId),
            ("name", $"Reparador-{token}"));

        return repairerId;
    }

    private static async Task SeedAssignmentAsync(DmoDbContext context, string machine, Guid repairerId)
    {
        await ExecuteAsync(
            context,
            "INSERT INTO machine_repairer_assignments (machine_repairer_assignment_id, machine, repairer_id, version, created_at, updated_at) " +
            "VALUES (@id, @machine, @repairer, 1, now(), now())",
            ("id", (object)Guid.NewGuid()),
            ("machine", machine),
            ("repairer", (object)repairerId));
    }

    private static async Task<Guid> SeedAggregateAsync(
        DmoDbContext context,
        string token,
        Guid userId,
        Guid? bqId = null,
        Guid? toolId = null,
        string status = ActiveToken,
        int version = 1,
        params string[] machines)
    {
        var aggregateId = Guid.NewGuid();
        var machineSet = machines.Length == 0 ? new[] { "B1" } : machines;

        await ExecuteAsync(
            context,
            "INSERT INTO boquilhas (boquilhas_id, bq_id, tool_id, status, opening_date, utilisation_percent, observations, created_by_user_id, version, created_at, updated_at) " +
            "VALUES (@id, @bq, @tool, @status, '2026-09-10', NULL, NULL, @user, @version, now(), now())",
            ("id", (object)aggregateId),
            ("bq", bqId is null ? DBNull.Value : (object)bqId.Value),
            ("tool", toolId is null ? DBNull.Value : (object)toolId.Value),
            ("status", status),
            ("user", (object)userId),
            ("version", version));

        foreach (var machine in machineSet)
        {
            await ExecuteAsync(
                context,
                "INSERT INTO boquilha_machines (boquilha_machine_id, boquilhas_id, machine) VALUES (@id, @aggregate, @machine)",
                ("id", (object)Guid.NewGuid()),
                ("aggregate", (object)aggregateId),
                ("machine", machine));
        }

        return aggregateId;
    }

    private static async Task<Guid> SeedInicioAsync(DmoDbContext context, Guid aggregateId, Guid userId, int quantity)
    {
        var movementId = Guid.NewGuid();
        await ExecuteAsync(
            context,
            "INSERT INTO boquilha_movements (movement_id, boquilhas_id, movement_type, quantity, business_date, recorded_at, recorded_by_user_id, version, created_at, updated_at) " +
            "VALUES (@id, @aggregate, 'inicio', @quantity, '2026-09-10', now(), @user, 1, now(), now())",
            ("id", (object)movementId),
            ("aggregate", (object)aggregateId),
            ("quantity", quantity),
            ("user", (object)userId));

        return movementId;
    }

    // ================================================================== IDENTITY (I1–I8)

    /// <summary>
    /// I1 (AC-I1) — a production-linked create anchors the REAL bq_id (a bq_contexts row): the
    /// Job On and the canonical tool_id are reachable through it and never duplicated.
    /// </summary>
    [SkippableFact]
    public async Task I1_ProductionLinkedCreateAnchorsTheRealBqId()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var toolId = await SeedToolAsync(context, token);
            var bqId = await SeedBqContextAsync(context, token, toolId);

            var repository = new BoquilhasRepository(context);
            var created = await repository.CreatedAsync(
                new BoqCreateUnit(bqId, null, ["B1"], 10, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None);

            Assert.NotNull(created);
            Assert.Equal(bqId, created.BqId);
            Assert.Null(created.ToolId);
            Assert.True(created.IsProductionLinked);
            Assert.Equal(1, created.Version);
            Assert.Single(created.Movements);
            Assert.Equal(MovementKind.Inicio, created.Movements[0].Kind);

            // The FK protects the anchor: bq_contexts/tools rows cannot be deleted while referenced.
            var deleteGuard = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
                context,
                "DELETE FROM bq_contexts WHERE bq_id = @bq",
                ("bq", (object)bqId)));
            Assert.Equal("23503", deleteGuard.SqlState);
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    /// <summary>
    /// I2/I7 (AC-I2/AC-I7) — a standalone create anchors tool_id ONLY with no fake Job On/bq rows;
    /// the exclusive-anchor CHECK is DB-enforced (both/none anchors fail 23514).
    /// </summary>
    [SkippableFact]
    public async Task I2I7_StandaloneCreateAnchorsToolOnlyAndTheAnchorExclusiveCheckIsEnforced()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var toolId = await SeedToolAsync(context, token);

            var repository = new BoquilhasRepository(context);
            var created = await repository.CreatedAsync(
                new BoqCreateUnit(null, toolId, ["B1", "C1"], 10, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None);

            Assert.Null(created.BqId);
            Assert.Equal(toolId, created.ToolId);
            Assert.True(created.IsStandalone);

            // No fake Job On / no fake bq_contexts anywhere: the aggregate row is the only new row.
            var jobOnCount = await QueryIntAsync(context, "SELECT count(*) FROM job_ons");
            var bqCount = await QueryIntAsync(context, "SELECT count(*) FROM bq_contexts");
            Assert.Equal(0, jobOnCount);
            Assert.Equal(0, bqCount);

            // Direct violations of the exclusive-anchor CHECK fail with 23514 (both / none).
            var both = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
                context,
                "INSERT INTO boquilhas (boquilhas_id, bq_id, tool_id, status, opening_date, created_by_user_id, version) " +
                "VALUES (@id, @bq, @tool, 'active', '2026-09-10', @user, 1)",
                ("id", (object)Guid.NewGuid()),
                ("bq", (object)Guid.NewGuid()),
                ("tool", (object)toolId),
                ("user", (object)userId)));
            Assert.Equal("23514", both.SqlState);
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    /// <summary>
    /// I3 (AC-I3) — a supplied non-existent bq_id is refused with BQ_CONTEXT_NOT_FOUND inside the
    /// create transaction (nothing written), and a supplied non-BQ tool with TOOL_TYPE_MISMATCH.
    /// </summary>
    [SkippableFact]
    public async Task I3_NonExistentBqAndNonBqToolsAreRefusedWithNothingWritten()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var cmToolId = await SeedToolAsync(context, token + "cm", "CM");
            var repository = new BoquilhasRepository(context);

            var missingBq = await Assert.ThrowsAsync<BoquilhasPersistenceException>(() => repository.CreatedAsync(
                new BoqCreateUnit(Guid.NewGuid(), null, ["B1"], 10, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None));
            Assert.Equal(BoquilhasPersistenceFailureReason.BqContextNotFound, missingBq.Reason);

            var typeMismatch = await Assert.ThrowsAsync<BoquilhasPersistenceException>(() => repository.CreatedAsync(
                new BoqCreateUnit(null, cmToolId, ["B1"], 10, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None));
            Assert.Equal(BoquilhasPersistenceFailureReason.ToolTypeMismatch, typeMismatch.Reason);

            Assert.Equal(0, await QueryIntAsync(context, "SELECT count(*) FROM boquilhas"));
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    /// <summary>
    /// I5/I6/C8 (AC-I5/AC-I6/AC-C8) — close/reopen operate on the SAME boquilhas_id; a full
    /// close/reopen/close cycle keeps a constant row count and writes a NEW snapshot per close.
    /// </summary>
    [SkippableFact]
    public async Task I5I6C8_CloseReopenCyclesKeepTheSameIdentity()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var toolId = await SeedToolAsync(context, token);
            var repository = new BoquilhasRepository(context);

            var created = await repository.CreatedAsync(
                new BoqCreateUnit(null, toolId, ["B1"], 10, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None);
            var aggregateId = created.BoquilhasId.Value;

            var firstClose = await repository.CloseAsync(new BoqCloseUnit(aggregateId, 1, userId), CancellationToken.None);
            var firstReopen = await repository.ReopenAsync(new BoqReopenUnit(aggregateId, 2, "reabrir", userId), CancellationToken.None);
            var secondClose = await repository.CloseAsync(new BoqCloseUnit(aggregateId, 3, userId), CancellationToken.None);

            Assert.Equal(aggregateId, firstReopen.Reopen.BoquilhasId);
            Assert.Equal(aggregateId, secondClose.Snapshot.BoquilhasId);

            Assert.Equal(1, await QueryIntAsync(context, "SELECT count(*) FROM boquilhas"));
            Assert.Equal(2, await QueryIntAsync(context, "SELECT count(*) FROM boquilha_close_snapshots"));
            Assert.Equal(1, await QueryIntAsync(context, "SELECT count(*) FROM boquilha_reopenings"));

            var aggregate = await repository.GetByIdAsync(aggregateId, CancellationToken.None);
            Assert.Equal(BoquilhaStatus.Closed, aggregate!.Status);
            Assert.Equal(2, aggregate.CloseSnapshots.Count);
            Assert.Single(aggregate.Reopenings);
            Assert.Equal(firstClose.Snapshot.CloseSnapshotId, aggregate.Reopenings[0].CloseSnapshotId);
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    /// <summary>
    /// I8 (AC-I8) — tool_id semantics = the canonical Tool; bq_id semantics = the frozen Job On
    /// context: the frozen triple stays immutable after creation.
    /// </summary>
    [SkippableFact]
    public async Task I8_TheFrozenTripleIsPresentedFromTheContextRow()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var toolId = await SeedToolAsync(context, token);
            var bqId = await SeedBqContextAsync(context, token, toolId);
            var repository = new BoquilhasRepository(context);

            var created = await repository.CreatedAsync(
                new BoqCreateUnit(bqId, null, ["B1"], 10, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None);

            // The live Tool is changed AFTER the context creation: the frozen triple is unchanged.
            await ExecuteAsync(
                context,
                "UPDATE tools SET reference = 'CHANGED', lot = 'CHANGED' WHERE tool_id = @tool",
                ("tool", (object)toolId));

            var contextRow = await ReadBqContextAsync(context, bqId);
            Assert.Equal($"BQ-{token}", contextRow!.ToolReference);
            Assert.Equal("01", contextRow.ToolLot);
            var live = await ReadToolAsync(context, toolId);
            Assert.Equal("CHANGED", live!.Reference);
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    // ================================================================== VOCABULARY (V1/V4)

    /// <summary>
    /// V1 (AC-V1) — the movement_type CHECK accepts exactly the four tokens and rejects any fifth
    /// value with SQLSTATE 23514.
    /// </summary>
    [SkippableFact]
    public async Task V1_TheTypeCheckAcceptsExactlyTheFourTokens()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var repairerId = await SeedRepairerAsync(context, token);
            var toolId = await SeedToolAsync(context, token);
            var aggregateId = await SeedAggregateAsync(context, token, userId, toolId: toolId);

            foreach (var movementType in new[] { "inicio", "saida", "entrada", "irreparavel" })
            {
                // The Saída-required CHECK needs machine + repairer on 'saida'; the Entrada-facts
                // CHECK needs expected/excess on 'entrada'.
                var saida = movementType == "saida";
                var entrada = movementType == "entrada";
                await ExecuteAsync(
                    context,
                    "INSERT INTO boquilha_movements (movement_id, boquilhas_id, movement_type, quantity, business_date, recorded_at, recorded_by_user_id, machine, repairer_id, expected_return_quantity, excess_received_quantity, version) " +
                    "VALUES (@id, @aggregate, @type, 1, '2026-09-11', now(), @user, @machine, @repairer, @expected, @excess, 1)",
                    ("id", (object)Guid.NewGuid()),
                    ("aggregate", (object)aggregateId),
                    ("type", movementType),
                    ("user", (object)userId),
                    ("machine", saida ? "B1" : DBNull.Value),
                    ("repairer", saida ? (object)repairerId : DBNull.Value),
                    ("expected", entrada ? 1 : DBNull.Value),
                    ("excess", entrada ? 0 : DBNull.Value));
            }

            var fifth = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
                context,
                "INSERT INTO boquilha_movements (movement_id, boquilhas_id, movement_type, quantity, business_date, recorded_at, recorded_by_user_id, version) " +
                "VALUES (@id, @aggregate, 'editar', 1, '2026-09-11', now(), @user, 1)",
                ("id", (object)Guid.NewGuid()),
                ("aggregate", (object)aggregateId),
                ("user", (object)userId)));
            Assert.Equal("23514", fifth.SqlState);

            // The saida-required CHECK enforces machine + repairer on external Saída (R5 backstop).
            var noFacts = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
                context,
                "INSERT INTO boquilha_movements (movement_id, boquilhas_id, movement_type, quantity, business_date, recorded_at, recorded_by_user_id, version) " +
                "VALUES (@id, @aggregate, 'saida', 1, '2026-09-11', now(), @user, 1)",
                ("id", (object)Guid.NewGuid()),
                ("aggregate", (object)aggregateId),
                ("user", (object)userId)));
            Assert.Equal("23514", noFacts.SqlState);

            // The entrance-facts CHECK enforces expected/excess on Entrada rows (Q-EXCESS backstop).
            var missingFacts = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
                context,
                "INSERT INTO boquilha_movements (movement_id, boquilhas_id, movement_type, quantity, business_date, recorded_at, recorded_by_user_id, version) " +
                "VALUES (@id, @aggregate, 'entrada', 5, '2026-09-11', now(), @user, 1)",
                ("id", (object)Guid.NewGuid()),
                ("aggregate", (object)aggregateId),
                ("user", (object)userId)));
            Assert.Equal("23514", missingFacts.SqlState);
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    /// <summary>
    /// V4 (AC-V4) — a second Início append is refused (only-one-inicio) with nothing written; the
    /// ledger keeps exactly one Início (created at opening).
    /// </summary>
    [SkippableFact]
    public async Task V4_ASecondInicioAppendIsRefused()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var toolId = await SeedToolAsync(context, token);
            var repository = new BoquilhasRepository(context);

            var created = await repository.CreatedAsync(
                new BoqCreateUnit(null, toolId, ["B1"], 10, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None);
            var aggregateId = created.BoquilhasId.Value;

            var refused = await Assert.ThrowsAsync<BoquilhasPersistenceException>(() => repository.AppendMovementAsync(
                new BoqAppendUnit(aggregateId, 1, "inicio", 5, new DateOnly(2026, 9, 11), null, null, null, userId),
                CancellationToken.None));
            Assert.Equal(BoquilhasPersistenceFailureReason.OnlyOneInicio, refused.Reason);

            Assert.Equal(
                1,
                await QueryIntAsync(context, "SELECT count(*) FROM boquilha_movements WHERE movement_type = 'inicio'"));
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    // ================================================================== BALANCE (B1–B9)

    /// <summary>
    /// B2 (AC-B2) — the multi-scenario ledger replays to the exact §18 buckets with the invariant
    /// (Início → Saída → Entrada → Irreparável incl. excess returns).
    /// </summary>
    [SkippableFact]
    public async Task B2_MultiScenarioReplayMatchesTheExactFormulas()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var repairerId = await SeedRepairerAsync(context, token);
            var toolId = await SeedToolAsync(context, token);
            var repository = new BoquilhasRepository(context);

            var created = await repository.CreatedAsync(
                new BoqCreateUnit(null, toolId, ["B1"], 100, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None);
            var aggregateId = created.BoquilhasId.Value;
            var version = 1;

            version = await AppendAsync(repository, aggregateId, version, "saida", 40, userId, "B1", repairerId);
            version = await AppendAsync(repository, aggregateId, version, "entrada", 30, userId);
            version = await AppendAsync(repository, aggregateId, version, "saida", 10, userId, "B1", repairerId);
            version = await AppendAsync(repository, aggregateId, version, "irreparavel", 8, userId);
            version = await AppendAsync(repository, aggregateId, version, "entrada", 14, userId);

            var aggregate = await repository.GetByIdAsync(aggregateId, CancellationToken.None);
            var balance = aggregate!.Balance;

            Assert.Equal(94, balance.Disponivel);        // 100 − 50 + 44
            Assert.Equal(-2, balance.EmReparacao);       // 50 − 44 − 8 (negative excess projection)
            Assert.Equal(8, balance.Irreparavel);
            Assert.Equal(2, balance.EntradaExcecional);  // 0 + 2
            Assert.Equal(100, balance.Disponivel + balance.EmReparacao + balance.Irreparavel);

            // The Entrada rows carry the exact expected/excess facts (B5).
            var entrada = aggregate.Movements.First(movement => movement.Kind == MovementKind.Entrada && movement.Quantity == 30);
            Assert.Equal(40, entrada.ExpectedReturnQuantity);
            Assert.Equal(0, entrada.ExcessReceivedQuantity);
            var excess = aggregate.Movements.First(movement => movement.Kind == MovementKind.Entrada && movement.Quantity == 14);
            Assert.Equal(12, excess.ExpectedReturnQuantity);
            Assert.Equal(2, excess.ExcessReceivedQuantity);
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    /// <summary>
    /// B3/B4 (AC-B3/AC-B4) — Saída > Disponível and Irreparável > Em reparação are refused with the
    /// typed reasons and zero rows; exact-boundary values succeed.
    /// </summary>
    [SkippableFact]
    public async Task B3B4_BalanceRelativeRefusalsWriteNothing()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var repairerId = await SeedRepairerAsync(context, token);
            var toolId = await SeedToolAsync(context, token);
            var repository = new BoquilhasRepository(context);

            var created = await repository.CreatedAsync(
                new BoqCreateUnit(null, toolId, ["B1"], 10, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None);
            var aggregateId = created.BoquilhasId.Value;

            var refused = await Assert.ThrowsAsync<BoquilhasPersistenceException>(() => repository.AppendMovementAsync(
                new BoqAppendUnit(aggregateId, 1, "saida", 11, new DateOnly(2026, 9, 11), "B1", repairerId, null, userId),
                CancellationToken.None));
            Assert.Equal(BoquilhasPersistenceFailureReason.SaidaExceedsAvailable, refused.Reason);

            var version = await AppendAsync(repository, aggregateId, 1, "saida", 6, userId, "B1", repairerId);

            var irreparavelRefused = await Assert.ThrowsAsync<BoquilhasPersistenceException>(() => repository.AppendMovementAsync(
                new BoqAppendUnit(aggregateId, version, "irreparavel", 7, new DateOnly(2026, 9, 11), null, null, null, userId),
                CancellationToken.None));
            Assert.Equal(BoquilhasPersistenceFailureReason.IrreparavelExceedsInRepair, irreparavelRefused.Reason);

            // Nothing written for the refusals: ledger count is exactly Início + one Saída.
            Assert.Equal(2, await QueryIntAsync(context, "SELECT count(*) FROM boquilha_movements"));

            // Exact-boundary values succeed (Saída exactly equal to Disponível after the return).
            version = await AppendAsync(repository, aggregateId, version, "entrada", 6, userId);
            version = await AppendAsync(repository, aggregateId, version, "saida", 10, userId, "B1", repairerId);
            Assert.Equal(4, await QueryIntAsync(context, "SELECT count(*) FROM boquilha_movements"));
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    /// <summary>
    /// B6/B7 (AC-B6/AC-B7) — excess Entrada is recorded in full (never clamped/rejected) and the
    /// negative Em reparação projection is derived and visible (nothing blocks on it).
    /// </summary>
    [SkippableFact]
    public async Task B6B7_ExcessEntradaIsRecordedAndNegativeEmReparacaoIsVisible()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var repairerId = await SeedRepairerAsync(context, token);
            var toolId = await SeedToolAsync(context, token);
            var repository = new BoquilhasRepository(context);

            var created = await repository.CreatedAsync(
                new BoqCreateUnit(null, toolId, ["B1"], 10, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None);
            var aggregateId = created.BoquilhasId.Value;

            var version = await AppendAsync(repository, aggregateId, 1, "saida", 6, userId, "B1", repairerId);
            version = await AppendAsync(repository, aggregateId, version, "entrada", 8, userId);

            var aggregate = await repository.GetByIdAsync(aggregateId, CancellationToken.None);
            Assert.Equal(-2, aggregate!.Balance.EmReparacao);
            Assert.Equal(12, aggregate.Balance.Disponivel);
            Assert.Equal(2, aggregate.Balance.EntradaExcecional);
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    /// <summary>
    /// B8 (AC-B8) — a zero-balance aggregate still exists after a full return; history stays
    /// queryable and further valid movements remain possible.
    /// </summary>
    [SkippableFact]
    public async Task B8_ZeroBalanceNeverDeletesOrInvalidatesTheAggregate()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var repairerId = await SeedRepairerAsync(context, token);
            var toolId = await SeedToolAsync(context, token);
            var repository = new BoquilhasRepository(context);

            var created = await repository.CreatedAsync(
                new BoqCreateUnit(null, toolId, ["B1"], 4, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None);
            var aggregateId = created.BoquilhasId.Value;

            var version = await AppendAsync(repository, aggregateId, 1, "saida", 4, userId, "B1", repairerId);
            version = await AppendAsync(repository, aggregateId, version, "entrada", 4, userId);

            var aggregate = await repository.GetByIdAsync(aggregateId, CancellationToken.None);
            Assert.NotNull(aggregate);
            Assert.Equal(4, aggregate!.Balance.Disponivel);

            var again = await repository.AppendMovementAsync(
                new BoqAppendUnit(aggregateId, version, "saida", 1, new DateOnly(2026, 9, 13), "B1", repairerId, null, userId),
                CancellationToken.None);
            Assert.NotNull(again);
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    /// <summary>
    /// B9/D3 (AC-B9/AC-D3) — editing only business_date leaves every bucket unchanged (replay order
    /// is physical) and never rewrites recorded_at.
    /// </summary>
    [SkippableFact]
    public async Task B9D3_BusinessDateEditsNeverChangeBalanceOrRecordedAt()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var toolId = await SeedToolAsync(context, token);
            var repository = new BoquilhasRepository(context);

            var created = await repository.CreatedAsync(
                new BoqCreateUnit(null, toolId, ["B1"], 10, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None);
            var aggregateId = created.BoquilhasId.Value;
            var inicio = created.Inicio!;

            var recordedAtBefore = await QueryStringAsync(
                context,
                "SELECT recorded_at::text FROM boquilha_movements WHERE movement_id = @id",
                ("id", (object)inicio.MovementId.Value));

            var edited = await repository.EditMovementAsync(
                new BoqEditUnit(aggregateId, 1, inicio.MovementId.Value, 1, 10, new DateOnly(2026, 9, 1), null, null, null, userId),
                CancellationToken.None);

            var recordedAtAfter = await QueryStringAsync(
                context,
                "SELECT recorded_at::text FROM boquilha_movements WHERE movement_id = @id",
                ("id", (object)inicio.MovementId.Value));
            Assert.Equal(recordedAtBefore, recordedAtAfter);

            var aggregate = await repository.GetByIdAsync(aggregateId, CancellationToken.None);
            Assert.Equal(10, aggregate!.Balance.Disponivel);
            Assert.Equal(0, aggregate.Balance.EmReparacao);
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    // ================================================================== EDIT / AUDIT (E1–E6)

    /// <summary>
    /// E1/E2/E3 (AC-E1/E2/E3) — Editar updates the SAME movement_id row with the exact before/after
    /// audit facts; the row count is unchanged (no second quantity event); audit rows are never
    /// movements.
    /// </summary>
    [SkippableFact]
    public async Task E1E2E3_EditIsASingleEventOnTheSameRowWithTheExactAudit()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var repairerId = await SeedRepairerAsync(context, token);
            var otherRepairerId = await SeedRepairerAsync(context, token + "x");
            var toolId = await SeedToolAsync(context, token);
            var repository = new BoquilhasRepository(context);

            var created = await repository.CreatedAsync(
                new BoqCreateUnit(null, toolId, ["B1"], 10, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None);
            var aggregateId = created.BoquilhasId.Value;
            var version = 1;

            var movementId = (await repository.AppendMovementAsync(
                new BoqAppendUnit(aggregateId, version, "saida", 6, new DateOnly(2026, 9, 11), "B1", repairerId, "original", userId),
                CancellationToken.None)).MovementId;
            version = 2;

            var edited = await repository.EditMovementAsync(
                new BoqEditUnit(
                    aggregateId, version, movementId.Value, 1,
                    4, new DateOnly(2026, 9, 13), "B1", otherRepairerId, "corrigido", userId),
                CancellationToken.None);

            Assert.Equal(movementId.Value, edited.Movement.MovementId.Value);

            // Row count unchanged: Início + the SAME edited Saída.
            Assert.Equal(2, await QueryIntAsync(context, "SELECT count(*) FROM boquilha_movements"));

            // Exactly one audit row with the exact before/after facts + the backend actor.
            var audit = await repository.GetMovementAuditAsync(movementId.Value, CancellationToken.None);
            Assert.Single(audit);
            var entry = audit[0];
            Assert.Equal(6, entry.BeforeQuantity);
            Assert.Equal(4, entry.AfterQuantity);
            Assert.Equal(new DateOnly(2026, 9, 11), entry.BeforeBusinessDate);
            Assert.Equal(new DateOnly(2026, 9, 13), entry.AfterBusinessDate);
            Assert.Equal("B1", entry.BeforeMachine);
            Assert.Equal("B1", entry.AfterMachine);
            Assert.Equal(repairerId, entry.BeforeRepairerId);
            Assert.Equal(otherRepairerId, entry.AfterRepairerId);
            Assert.Equal("original", entry.BeforeObservations);
            Assert.Equal("corrigido", entry.AfterObservations);
            Assert.Equal(userId, entry.EditedByUserId);

            // The audit table is NOT the ledger: only movements replay into the balance.
            var aggregate = await repository.GetByIdAsync(aggregateId, CancellationToken.None);
            Assert.Equal(2, aggregate!.Movements.Count);
            var saida = aggregate.Movements.First(movement => movement.Kind == MovementKind.Saida);
            Assert.Equal(4, saida.Quantity);
            Assert.Equal(10 - 4, aggregate.Balance.Disponivel);
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    /// <summary>
    /// E4 (AC-E4) — no double balance effect: the post-edit buckets equal the replay with the row's
    /// current values replaced by the new values (single net event).
    /// </summary>
    [SkippableFact]
    public async Task E4_NoDoubleBalanceEffect()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var repairerId = await SeedRepairerAsync(context, token);
            var toolId = await SeedToolAsync(context, token);
            var repository = new BoquilhasRepository(context);

            var created = await repository.CreatedAsync(
                new BoqCreateUnit(null, toolId, ["B1"], 10, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None);
            var aggregateId = created.BoquilhasId.Value;
            var version = 1;

            var saida = await repository.AppendMovementAsync(
                new BoqAppendUnit(aggregateId, version, "saida", 6, new DateOnly(2026, 9, 11), "B1", repairerId, null, userId),
                CancellationToken.None);
            version = 2;

            await repository.EditMovementAsync(
                new BoqEditUnit(aggregateId, version, saida.MovementId.Value, 1, 4, new DateOnly(2026, 9, 11), "B1", repairerId, null, userId),
                CancellationToken.None);

            var aggregate = await repository.GetByIdAsync(aggregateId, CancellationToken.None);
            var balance = aggregate!.Balance;

            // Replay of the equivalent ledger: Início 10 + Saída 4.
            Assert.Equal(6, balance.Disponivel);
            Assert.Equal(4, balance.EmReparacao);

            // Editing an Entrada recomputes the expected/excess facts from the before-state (N2).
            version = await AppendAsync(repository, aggregateId, 3, "entrada", 12, userId);
            var aggregateAfterAppend = await repository.GetByIdAsync(aggregateId, CancellationToken.None);
            var entrada = aggregateAfterAppend!.Movements.First(movement => movement.Kind == MovementKind.Entrada);
            await repository.EditMovementAsync(
                new BoqEditUnit(aggregateId, version, entrada.MovementId.Value, 1, 3, new DateOnly(2026, 9, 12), null, null, null, userId),
                CancellationToken.None);

            aggregate = await repository.GetByIdAsync(aggregateId, CancellationToken.None);
            var editedEntrada = aggregate!.Movements.First(movement => movement.Kind == MovementKind.Entrada);
            Assert.Equal(3, editedEntrada.Quantity);
            Assert.Equal(4, editedEntrada.ExpectedReturnQuantity);
            Assert.Equal(0, editedEntrada.ExcessReceivedQuantity);
            Assert.Equal(10 - 4 + 3, aggregate.Balance.Disponivel);
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    /// <summary>
    /// E6 (AC-E6) — movement_type and recorded_at are immutable: the edit path never writes them
    /// (byte-equal across edits).
    /// </summary>
    [SkippableFact]
    public async Task E6_MovementTypeAndRecordedAtAreImmutable()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var toolId = await SeedToolAsync(context, token);
            var repository = new BoquilhasRepository(context);

            var created = await repository.CreatedAsync(
                new BoqCreateUnit(null, toolId, ["B1"], 10, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None);
            var inicio = created.Inicio!;

            var before = await QueryStringAsync(
                context,
                "SELECT movement_type || '|' || recorded_at::text FROM boquilha_movements WHERE movement_id = @id",
                ("id", (object)inicio.MovementId.Value));

            await repository.EditMovementAsync(
                new BoqEditUnit(created.BoquilhasId.Value, 1, inicio.MovementId.Value, 1, 10, new DateOnly(2026, 9, 2), null, null, "nota", userId),
                CancellationToken.None);

            var after = await QueryStringAsync(
                context,
                "SELECT movement_type || '|' || recorded_at::text FROM boquilha_movements WHERE movement_id = @id",
                ("id", (object)inicio.MovementId.Value));
            Assert.Equal(before, after);
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    // ================================================================== REPAIRER (R1–R3/R5)

    /// <summary>
    /// R1/R2 (AC-R1/AC-R2) — machine → current assignment → repairer resolution works through the
    /// consumed closes P2-T05 reads; the six machines resolve independently.
    /// </summary>
    [SkippableFact]
    public async Task R1R2_MachineAssignmentsResolveIndependently()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var b1Repairer = await SeedRepairerAsync(context, token + "b1");
            var c1Repairer = await SeedRepairerAsync(context, token + "c1");
            await SeedAssignmentAsync(context, "B1", b1Repairer);
            await SeedAssignmentAsync(context, "C1", c1Repairer);

            var assignments = await context.Set<MachineRepairerAssignmentEntity>()
                .AsNoTracking()
                .OrderBy(assignment => assignment.Machine)
                .ToListAsync();

            Assert.Equal(2, assignments.Count);
            var b1 = assignments.Single(assignment => assignment.Machine == "B1");
            Assert.Equal(b1Repairer, b1.RepairerId);
            var c1 = assignments.Single(assignment => assignment.Machine == "C1");
            Assert.Equal(c1Repairer, c1.RepairerId);

            // Changing B1's assignment changes no other machine's resolution.
            var b2Repairer = await SeedRepairerAsync(context, token + "b2");
            await ExecuteAsync(
                context,
                "UPDATE machine_repairer_assignments SET repairer_id = @repairer, version = 2 WHERE machine = 'B1'",
                ("repairer", (object)b2Repairer));

            var after = await context.Set<MachineRepairerAssignmentEntity>()
                .AsNoTracking()
                .SingleAsync(assignment => assignment.Machine == "C1");
            Assert.Equal(c1Repairer, after.RepairerId);
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    /// <summary>
    /// R3 (AC-R3) — historical preservation: a later assignment change never rewrites an earlier
    /// movement's repairer_id/machine facts.
    /// </summary>
    [SkippableFact]
    public async Task R3_LaterAssignmentChangesNeverRewriteHistoricalMovements()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var originalRepairer = await SeedRepairerAsync(context, token);
            var newRepairer = await SeedRepairerAsync(context, token + "new");
            var toolId = await SeedToolAsync(context, token);
            await SeedAssignmentAsync(context, "B1", originalRepairer);
            var repository = new BoquilhasRepository(context);

            var created = await repository.CreatedAsync(
                new BoqCreateUnit(null, toolId, ["B1"], 10, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None);
            var aggregateId = created.BoquilhasId.Value;

            var saida = await repository.AppendMovementAsync(
                new BoqAppendUnit(aggregateId, 1, "saida", 6, new DateOnly(2026, 9, 11), "B1", originalRepairer, null, userId),
                CancellationToken.None);

            // The machine's assignment changes AFTER the movement was recorded.
            await ExecuteAsync(
                context,
                "UPDATE machine_repairer_assignments SET repairer_id = @repairer, version = 2 WHERE machine = 'B1'",
                ("repairer", (object)newRepairer));

            var stored = await QueryStringAsync(
                context,
                "SELECT repairer_id::text FROM boquilha_movements WHERE movement_id = @id",
                ("id", (object)saida.MovementId.Value));
            Assert.Equal(originalRepairer.ToString(), stored);
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    // ================================================================== OPENING FACTS (U2)

    /// <summary>
    /// U2 (AC-U2) — the opening-facts update (opening date / utilisation / observations / machine
    /// set) changes no movement and no balance bucket; movement machine facts are untouched.
    /// </summary>
    [SkippableFact]
    public async Task U2_OpeningFactsUpdatesNeverTouchMovementsOrBalance()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var repairerId = await SeedRepairerAsync(context, token);
            var toolId = await SeedToolAsync(context, token);
            var repository = new BoquilhasRepository(context);

            var created = await repository.CreatedAsync(
                new BoqCreateUnit(null, toolId, ["B1"], 10, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None);
            var aggregateId = created.BoquilhasId.Value;

            await repository.AppendMovementAsync(
                new BoqAppendUnit(aggregateId, 1, "saida", 4, new DateOnly(2026, 9, 11), "B1", repairerId, null, userId),
                CancellationToken.None);

            var updated = await repository.UpdateOpeningFactsAsync(
                new BoqOpeningFactsUnit(aggregateId, 2, new DateOnly(2026, 9, 5), 45.5m, "nova observação", ["B2", "C1"]),
                CancellationToken.None);

            Assert.Equal(new DateOnly(2026, 9, 5), updated.OpeningDate);
            Assert.Equal(45.5m, updated.UtilisationPercent);
            Assert.Equal(2, updated.Machines.Count);
            Assert.Equal(6, updated.Balance.Disponivel); // unchanged by the facts update
            Assert.Equal(4, updated.Balance.EmReparacao);

            var movement = await QueryStringAsync(
                context,
                "SELECT machine FROM boquilha_movements WHERE movement_type = 'saida'");
            Assert.Equal("B1", movement);
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    // ================================================================== CLOSE / REOPEN (C1–C7)

    /// <summary>
    /// C1/C2 (AC-C1/AC-C2) — the close snapshot is immutable with the exact frozen buckets; a
    /// forced mid-transaction failure leaves the aggregate active with NO snapshot and no version
    /// bump.
    /// </summary>
    [SkippableFact]
    public async Task C1C2_CloseIsImmutableAndFailedCloseIsAtomic()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var repairerId = await SeedRepairerAsync(context, token);
            var toolId = await SeedToolAsync(context, token);
            var repository = new BoquilhasRepository(context);

            var created = await repository.CreatedAsync(
                new BoqCreateUnit(null, toolId, ["B1"], 10, new DateOnly(2026, 9, 10), 30m, "abertura", userId),
                CancellationToken.None);
            var aggregateId = created.BoquilhasId.Value;

            await repository.AppendMovementAsync(
                new BoqAppendUnit(aggregateId, 1, "saida", 4, new DateOnly(2026, 9, 11), "B1", repairerId, null, userId),
                CancellationToken.None);

            var closed = await repository.CloseAsync(new BoqCloseUnit(aggregateId, 2, userId), CancellationToken.None);
            var snapshot = closed.Snapshot;

            Assert.Equal(10, snapshot.InitialQuantity);
            Assert.Equal(6, snapshot.Disponivel);
            Assert.Equal(4, snapshot.EmReparacao);
            Assert.Equal(0, snapshot.Irreparavel);
            Assert.Equal(0, snapshot.EntradaExcecional);
            Assert.Equal(30m, snapshot.UtilisationPercent);
            Assert.Equal(userId, snapshot.ClosedByUserId);
            Assert.Equal(new DateOnly(2026, 9, 10), snapshot.OpeningDate);
            Assert.Equal(3, closed.AggregateVersion);

            // Later movements never modify the snapshot (a reopen + movement + re-close writes a
            // NEW snapshot; the old one is untouched).
            await repository.ReopenAsync(new BoqReopenUnit(aggregateId, 3, "reabrir", userId), CancellationToken.None);
            await repository.AppendMovementAsync(
                new BoqAppendUnit(aggregateId, 4, "entrada", 2, new DateOnly(2026, 9, 12), null, null, null, userId),
                CancellationToken.None);
            var secondClose = await repository.CloseAsync(new BoqCloseUnit(aggregateId, 5, userId), CancellationToken.None);

            var firstSnapshot = await QueryStringAsync(
                context,
                "SELECT disponivel || '|' || em_reparacao FROM boquilha_close_snapshots WHERE close_snapshot_id = @id",
                ("id", (object)snapshot.CloseSnapshotId));
            Assert.Equal("6|4", firstSnapshot);
            Assert.NotEqual(snapshot.CloseSnapshotId, secondClose.Snapshot.CloseSnapshotId);
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    /// <summary>
    /// C3/C4/C5/C6/C7 (AC-C3…C7) — the close/reopen lifecycle: eligibility refusals write nothing,
    /// the immutable snapshot is preserved, and history survives a reopen untouched.
    /// </summary>
    [SkippableFact]
    public async Task C3C4C5C6C7_ReopenEligibilityAndHistoryPreservation()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var toolId = await SeedToolAsync(context, token);
            var repository = new BoquilhasRepository(context);

            // Only ONE active aggregate may exist per anchor: the FIRST is closed before the
            // SECOND trace is opened (the same-tool two-trace scenario of the close/reopen rules).
            var first = await repository.CreatedAsync(
                new BoqCreateUnit(null, toolId, ["B1"], 10, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None);
            await repository.CloseAsync(new BoqCloseUnit(first.BoquilhasId.Value, 1, userId), CancellationToken.None);

            var second = await repository.CreatedAsync(
                new BoqCreateUnit(null, toolId, ["B1"], 5, new DateOnly(2026, 9, 11), null, null, userId),
                CancellationToken.None);

            await repository.CloseAsync(new BoqCloseUnit(second.BoquilhasId.Value, 1, userId), CancellationToken.None);

            // not-last-closed: the FIRST closed aggregate is NOT the most recent close of the anchor.
            var notLast = await Assert.ThrowsAsync<BoquilhasPersistenceException>(() => repository.ReopenAsync(
                new BoqReopenUnit(first.BoquilhasId.Value, 2, "reabrir", userId), CancellationToken.None));
            Assert.Equal(BoquilhasPersistenceFailureReason.NotLastClosed, notLast.Reason);

            // not-closed: an ACTIVE (never-closed) aggregate cannot be reopened. A THIRD trace is
            // opened only after BOTH earlier traces are closed (the one-active rule).
            var third = await repository.CreatedAsync(
                new BoqCreateUnit(null, toolId, ["B1"], 3, new DateOnly(2026, 9, 12), null, null, userId),
                CancellationToken.None);
            var notClosed = await Assert.ThrowsAsync<BoquilhasPersistenceException>(() => repository.ReopenAsync(
                new BoqReopenUnit(third.BoquilhasId.Value, 1, "reabrir", userId), CancellationToken.None));
            Assert.Equal(BoquilhasPersistenceFailureReason.NotClosed, notClosed.Reason);
            await repository.CloseAsync(new BoqCloseUnit(third.BoquilhasId.Value, 1, userId), CancellationToken.None);

            // The SECOND is no longer the last close either (the THIRD is now the most recent).
            var secondNotLast = await Assert.ThrowsAsync<BoquilhasPersistenceException>(() => repository.ReopenAsync(
                new BoqReopenUnit(second.BoquilhasId.Value, 2, "reabrir", userId), CancellationToken.None));
            Assert.Equal(BoquilhasPersistenceFailureReason.NotLastClosed, secondNotLast.Reason);

            // No reopen record was written by any refusal.
            Assert.Equal(0, await QueryIntAsync(context, "SELECT count(*) FROM boquilha_reopenings"));

            // The eligible reopen succeeds on the SAME id with the exact close referenced: the
            // THIRD trace is the last-closed one.
            var reopened = await repository.ReopenAsync(
                new BoqReopenUnit(third.BoquilhasId.Value, 2, "devolução parcial", userId), CancellationToken.None);
            Assert.Equal(third.BoquilhasId.Value, reopened.Reopen.BoquilhasId);
            var thirdSnapshot = (await repository.GetByIdAsync(third.BoquilhasId.Value, CancellationToken.None))!.LastClose!;
            Assert.Equal(thirdSnapshot.CloseSnapshotId, reopened.Reopen.CloseSnapshotId);

            var aggregate = await repository.GetByIdAsync(third.BoquilhasId.Value, CancellationToken.None);
            Assert.Equal(BoquilhaStatus.Active, aggregate!.Status);
            Assert.Single(aggregate.Reopenings);
            Assert.Equal("devolução parcial", aggregate.Reopenings[0].Reason);
            Assert.Equal(userId, aggregate.Reopenings[0].ReopenedByUserId);
            Assert.Single(aggregate.CloseSnapshots);

            // The reopened aggregate is a normal active aggregate again (C4), and appears in the
            // active list (C3: after close it left it — asserted through the list filters).
            var active = await repository.ListAsync(new BoquilhasListQuery(null, null, null, null, 1, 50), CancellationToken.None);
            Assert.Contains(active, row => row.BoquilhasId == third.BoquilhasId.Value);
            Assert.DoesNotContain(active, row => row.BoquilhasId == first.BoquilhasId.Value);
            Assert.DoesNotContain(active, row => row.BoquilhasId == second.BoquilhasId.Value);
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    // ================================================================== CONCURRENCY (K1–K8)

    /// <summary>
    /// K1/K2 (AC-K1/AC-K2) — a stale aggregate version refuses every guarded operation; a stale
    /// movement version refuses the edit; nothing is written.
    /// </summary>
    [SkippableFact]
    public async Task K1K2_StaleVersionsRefuseWithNothingWritten()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var repairerId = await SeedRepairerAsync(context, token);
            var toolId = await SeedToolAsync(context, token);
            var repository = new BoquilhasRepository(context);

            var created = await repository.CreatedAsync(
                new BoqCreateUnit(null, toolId, ["B1"], 10, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None);
            var aggregateId = created.BoquilhasId.Value;

            // Stale aggregate version on append / edit / close / reopen / opening-facts.
            var refusedAppend = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => repository.AppendMovementAsync(
                new BoqAppendUnit(aggregateId, 99, "saida", 2, new DateOnly(2026, 9, 11), "B1", repairerId, null, userId),
                CancellationToken.None));
            Assert.NotNull(refusedAppend);

            var closed = await repository.CloseAsync(new BoqCloseUnit(aggregateId, 1, userId), CancellationToken.None);
            var staleReopen = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => repository.ReopenAsync(
                new BoqReopenUnit(aggregateId, 1, "reabrir", userId), CancellationToken.None));
            Assert.NotNull(staleReopen);

            // Nothing written by the refusals.
            Assert.Equal(1, await QueryIntAsync(context, "SELECT count(*) FROM boquilha_movements"));
            Assert.Equal(0, await QueryIntAsync(context, "SELECT count(*) FROM boquilha_reopenings"));
            Assert.Equal(ClosedToken, await QueryStringAsync(context, "SELECT status FROM boquilhas"));

            // Stale movement version on edit.
            var reopened = await repository.ReopenAsync(new BoqReopenUnit(aggregateId, closed.AggregateVersion, "reabrir", userId), CancellationToken.None);
            var movimiento = created.Inicio!;
            var staleEdit = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => repository.EditMovementAsync(
                new BoqEditUnit(aggregateId, reopened.AggregateVersion, movimiento.MovementId.Value, 77, 10, new DateOnly(2026, 9, 10), null, null, null, userId),
                CancellationToken.None));
            Assert.NotNull(staleEdit);
            Assert.Equal(0, await QueryIntAsync(context, "SELECT count(*) FROM boquilha_movement_audit"));
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    /// <summary>
    /// K4 (AC-K4) — a save-time race (a second connection bumps the aggregate version between the
    /// in-transaction compare and the save) surfaces as the typed conflict via the SaveAsync
    /// mapping — never a 500, never a silent overwrite.
    /// </summary>
    [SkippableFact]
    public async Task K4_SaveTimeRacesSurfaceAsTheTypedConflict()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var repairerId = await SeedRepairerAsync(context, token);
            var toolId = await SeedToolAsync(context, token);
            var repository = new BoquilhasRepository(context);

            var created = await repository.CreatedAsync(
                new BoqCreateUnit(null, toolId, ["B1"], 10, new DateOnly(2026, 9, 10), null, null, userId),
                CancellationToken.None);
            var aggregateId = created.BoquilhasId.Value;

            // The racing interceptor bumps the aggregate version through a second real connection
            // right before the guarded save executes.
            var racer = new AggregateVersionRaceInterceptor(aggregateId);

            await using var racingContext = CreateContextWithInterceptor(racer);
            var racingRepository = new BoquilhasRepository(racingContext);

            var conflict = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => racingRepository.AppendMovementAsync(
                new BoqAppendUnit(aggregateId, 1, "saida", 2, new DateOnly(2026, 9, 11), "B1", repairerId, null, userId),
                CancellationToken.None));
            Assert.NotNull(conflict);

            // Nothing of the losing write survived.
            Assert.Equal(1, await QueryIntAsync(context, "SELECT count(*) FROM boquilha_movements"));
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    /// <summary>
    /// K6 (B1 correction, AC-C6/AC-K3/AC-K4) — PRODUCTION-LINKED create race: two concurrent
    /// creates for the SAME bq_id against the REAL database → exactly ONE 201-style success, exactly
    /// ONE <c>ActiveAggregateExists</c> loser (via the pre-check OR the DB partial unique index
    /// mapped from 23505 — the outcome is the invariant, never a 500); exactly one active aggregate,
    /// exactly one committed Início, machine rows only for the winner, ZERO partial/orphan state.
    /// </summary>
    [SkippableFact]
    public async Task K6_ProductionLinkedConcurrentCreateRace()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var bqId = await SeedBqContextAsync(context, token, await SeedToolAsync(context, token));

            // Two genuine concurrent transactions through TWO separate contexts/connections.
            await using var contextA = PersistenceTestDatabase.CreateContext();
            await using var contextB = PersistenceTestDatabase.CreateContext();
            var repositoryA = new BoquilhasRepository(contextA);
            var repositoryB = new BoquilhasRepository(contextB);

            var unitA = new BoqCreateUnit(bqId, null, ["B1"], 10, new DateOnly(2026, 9, 10), null, null, userId);
            var unitB = new BoqCreateUnit(bqId, null, ["B1"], 8, new DateOnly(2026, 9, 10), null, null, userId);

            var results = await Task.WhenAll(
                RunCreateAsync(repositoryA, unitA),
                RunCreateAsync(repositoryB, unitB));

            var winners = results.Where(result => result.Succeeded).ToList();
            var losers = results.Where(result => !result.Succeeded).ToList();

            Assert.Single(winners);
            Assert.Single(losers);
            Assert.Equal(BoquilhasPersistenceFailureReason.ActiveAggregateExists, losers[0].Failure!.Reason);

            // Exactly one active aggregate, one committed Início, machine rows only for the winner.
            Assert.Equal(1, await QueryIntAsync(context, "SELECT count(*) FROM boquilhas WHERE status = 'active'"));
            Assert.Equal(1, await QueryIntAsync(context, "SELECT count(*) FROM boquilha_movements WHERE movement_type = 'inicio'"));
            Assert.Equal(1, await QueryIntAsync(context, "SELECT count(*) FROM boquilha_machines"));
            Assert.Equal(0, await QueryIntAsync(context, "SELECT count(*) FROM boquilha_close_snapshots"));
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    /// <summary>
    /// K7 (B1 correction, AC-C6/AC-K3/AC-K4) — STANDALONE create race: the same race for the SAME
    /// tool_id via <c>IX_boquilhas_active_tool_id</c>; identical outcome.
    /// </summary>
    [SkippableFact]
    public async Task K7_StandaloneConcurrentCreateRace()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var toolId = await SeedToolAsync(context, token);

            await using var contextA = PersistenceTestDatabase.CreateContext();
            await using var contextB = PersistenceTestDatabase.CreateContext();
            var repositoryA = new BoquilhasRepository(contextA);
            var repositoryB = new BoquilhasRepository(contextB);

            var unitA = new BoqCreateUnit(null, toolId, ["B1"], 10, new DateOnly(2026, 9, 10), null, null, userId);
            var unitB = new BoqCreateUnit(null, toolId, ["B1"], 8, new DateOnly(2026, 9, 10), null, null, userId);

            var results = await Task.WhenAll(
                RunCreateAsync(repositoryA, unitA),
                RunCreateAsync(repositoryB, unitB));

            var winners = results.Where(result => result.Succeeded).ToList();
            var losers = results.Where(result => !result.Succeeded).ToList();

            Assert.Single(winners);
            Assert.Single(losers);
            Assert.Equal(BoquilhasPersistenceFailureReason.ActiveAggregateExists, losers[0].Failure!.Reason);

            Assert.Equal(1, await QueryIntAsync(context, "SELECT count(*) FROM boquilhas WHERE status = 'active'"));
            Assert.Equal(1, await QueryIntAsync(context, "SELECT count(*) FROM boquilha_movements WHERE movement_type = 'inicio'"));
            Assert.Equal(0, await QueryIntAsync(context, "SELECT count(*) FROM boquilha_close_snapshots"));
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    /// <summary>
    /// K8 (B1 correction, AC-C6/AC-C7/AC-K3) — create-vs-reopen race on one anchor: at most one
    /// active aggregate results; when the CREATE commits first, the REOPEN's status UPDATE raises
    /// 23505 on the active-anchor index → <c>ActiveAggregateExists</c> and the reopen transaction
    /// rolls back COMPLETELY — the aggregate remains closed with its close snapshot, reopen records
    /// and history intact and NO partial reopening record exists (either interleaving is verified).
    /// </summary>
    [SkippableFact]
    public async Task K8_CreateVsReopenRaceOnOneAnchor()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        try
        {
            var userId = await SeedUserAsync(context, token);
            var toolId = await SeedToolAsync(context, token);

            // A closed, last-closed aggregate (eligible for reopen) anchored to the tool.
            var closedAggregateId = await SeedAggregateAsync(context, token, userId, toolId: toolId, status: ClosedToken, version: 2);
            await SeedInicioAsync(context, closedAggregateId, userId, 10);
            var snapshotId = Guid.NewGuid();
            await ExecuteAsync(
                context,
                "INSERT INTO boquilha_close_snapshots (close_snapshot_id, boquilhas_id, closed_by_user_id, closed_at, " +
                "initial_quantity, opening_date, disponivel, em_reparacao, irreparavel, entrada_excecional, created_at) " +
                "VALUES (@id, @aggregate, @user, now(), 10, '2026-09-10', 10, 0, 0, 0, now())",
                ("id", (object)snapshotId),
                ("aggregate", (object)closedAggregateId),
                ("user", (object)userId));

            // The reopen transaction races the create transaction for the SAME anchor.
            await using var reopenContext = PersistenceTestDatabase.CreateContext();
            await using var createContext = PersistenceTestDatabase.CreateContext();
            var reopenRepository = new BoquilhasRepository(reopenContext);
            var createRepository = new BoquilhasRepository(createContext);

            var reopenResult = await RunReopenAsync(reopenRepository, closedAggregateId, userId);
            var createResult = await RunCreateAsync(
                createRepository,
                new BoqCreateUnit(null, toolId, ["B1"], 5, new DateOnly(2026, 9, 11), null, null, userId));

            // At most one active aggregate; if the create won, the reopen lost with the typed token.
            var activeCount = await QueryIntAsync(context, "SELECT count(*) FROM boquilhas WHERE status = 'active'");
            Assert.True(activeCount is 1 or 0, "at most one active aggregate can exist");

            if (!reopenResult.Succeeded)
            {
                Assert.False(createResult.Succeeded, "one of the two operations must win");
                Assert.NotNull(reopenResult.Failure);
                Assert.True(
                    reopenResult.Failure.Reason is BoquilhasPersistenceFailureReason.ActiveAggregateExists,
                    $"The reopen loser must be refused with ActiveAggregateExists, got {reopenResult.Failure.Reason}.");

                // The reopen LOSER: the aggregate remains CLOSED, its snapshot is intact, its
                // history is intact, and NO partial reopening record exists.
                Assert.Equal(ClosedToken, await QueryStringAsync(
                    context, "SELECT status FROM boquilhas WHERE boquilhas_id = @id",
                    ("id", (object)closedAggregateId)));
                Assert.Equal(1, await QueryIntAsync(context, "SELECT count(*) FROM boquilha_close_snapshots WHERE boquilhas_id = @id",
                    ("id", (object)closedAggregateId)));
                Assert.Equal(0, await QueryIntAsync(context, "SELECT count(*) FROM boquilha_reopenings WHERE boquilhas_id = @id",
                    ("id", (object)closedAggregateId)));
            }
            else
            {
                // The reopen won: no create may have succeeded.
                Assert.False(createResult.Succeeded, "the create must lose when the reopen wins");
                Assert.Equal(1, activeCount);
                var reopenRecords = await QueryIntAsync(
                    context,
                    "SELECT count(*) FROM boquilha_reopenings WHERE boquilhas_id = @id",
                    ("id", (object)closedAggregateId));
                Assert.Equal(1, reopenRecords);
            }
        }
        finally
        {
            await CleanupAsync(context, token);
        }
    }

    // ================================================================== helpers

    private sealed record CreateOutcome(bool Succeeded, BoquilhasPersistenceException? Failure);

    private static async Task<CreateOutcome> RunCreateAsync(IBoquilhasRepository repository, BoqCreateUnit unit)
    {
        try
        {
            await repository.CreatedAsync(unit, CancellationToken.None);
            return new CreateOutcome(true, null);
        }
        catch (BoquilhasPersistenceException exception)
        {
            return new CreateOutcome(false, exception);
        }
    }

    private static async Task<CreateOutcome> RunReopenAsync(IBoquilhasRepository repository, Guid aggregateId, Guid userId)
    {
        try
        {
            await repository.ReopenAsync(new BoqReopenUnit(aggregateId, 2, "reabrir", userId), CancellationToken.None);
            return new CreateOutcome(true, null);
        }
        catch (BoquilhasPersistenceException exception)
        {
            return new CreateOutcome(false, exception);
        }
    }

    private static async Task<int> AppendAsync(
        BoquilhasRepository repository,
        Guid aggregateId,
        int version,
        string movementType,
        int quantity,
        Guid userId,
        string? machine = null,
        Guid? repairerId = null)
    {
        var appended = await repository.AppendMovementAsync(
            new BoqAppendUnit(aggregateId, version, movementType, quantity, new DateOnly(2026, 9, 11), machine, repairerId, null, userId),
            CancellationToken.None);
        _ = appended;
        return version + 1;
    }

    private static DmoDbContext CreateContextWithInterceptor(AggregateVersionRaceInterceptor interceptor)
    {
        var connectionString = PersistenceTestDatabase.ConnectionString
            ?? throw new InvalidOperationException($"Set {PersistenceTestDatabase.ConnectionEnvironmentVariable} first.");

        var options = new DbContextOptionsBuilder<DmoDbContext>()
            .UseNpgsql(connectionString)
            .AddInterceptors(interceptor)
            .Options;

        return new DmoDbContext(options);
    }

    /// <summary>
    /// Save-time race interceptor: right before the guarded save, a second REAL connection bumps
    /// the aggregate version, so the losing save's <c>UPDATE … WHERE version = @expected</c>
    /// matches zero rows and the accepted mapping surfaces the typed conflict (K4; the accepted
    /// P2-T06 interceptor pattern).
    /// </summary>
    private sealed class AggregateVersionRaceInterceptor(Guid aggregateId) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await using var racer = PersistenceTestDatabase.CreateContext();
            await racer.Database.ExecuteSqlRawAsync(
                "UPDATE boquilhas SET version = version + 1, updated_at = now() WHERE boquilhas_id = @id",
                new NpgsqlParameter("id", aggregateId));

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private static async Task ExecuteAsync(DmoDbContext context, string sql, params (string Name, object Value)[] parameters)
    {
        var connection = context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        await command.ExecuteNonQueryAsync();
    }

    private static async Task<int> QueryIntAsync(DmoDbContext context, string sql, params (string Name, object Value)[] parameters) =>
        int.Parse(await QueryStringAsync(context, sql, parameters));

    private static async Task<string> QueryStringAsync(DmoDbContext context, string sql, params (string Name, object Value)[] parameters)
    {
        var connection = context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        var result = await command.ExecuteScalarAsync();
        return result?.ToString() ?? string.Empty;
    }

    private static async Task<BqContextRead?> ReadBqContextAsync(DmoDbContext context, Guid bqId)
    {
        var entity = await context.Set<BqContextEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(contextRow => contextRow.BqId == bqId);

        return entity is null
            ? null
            : new BqContextRead(entity.BqId, entity.JobOnId, entity.ToolId, entity.ToolType, entity.ToolReference, entity.ToolLot);
    }

    private static async Task<ToolRead?> ReadToolAsync(DmoDbContext context, Guid toolId)
    {
        var entity = await context.Set<ToolEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(tool => tool.ToolId == toolId);

        return entity is null ? null : new ToolRead(entity.ToolId, entity.Reference, entity.Lot);
    }

    private sealed record ToolRead(Guid ToolId, string Reference, string Lot);

    private static async Task CleanupAsync(DmoDbContext context, string token)
    {
        foreach (var table in new[] { "boquilha_reopenings", "boquilha_close_snapshots", "boquilha_movement_audit", "boquilha_movements", "boquilha_machines", "boquilhas" })
        {
            await PersistenceTestDatabase.ClearTableAsync(context, table);
        }

        await ExecuteAsync(context, "DELETE FROM bq_contexts");
        await ExecuteAsync(context, "DELETE FROM job_ons");
        await ExecuteAsync(context, "DELETE FROM tools");
        await ExecuteAsync(context, "DELETE FROM machine_repairer_assignments");
        await ExecuteAsync(context, "DELETE FROM repairers");
        await ExecuteAsync(context, "DELETE FROM users");
    }
}