using DMO.Application.Boquilhas;
using DMO.Application.Persistence;
using DMO.Application.Repositories;
using DMO.Domain.Boquilhas;
using DMO.Domain.Controlo;
using DMO.Domain.Tools;
using DMO.IntegrationTests.JobOn;

namespace DMO.IntegrationTests.Boquilhas;

/// <summary>
/// P2-T07 test composition: the accepted P2-T04/JO store plus in-memory implementations of the
/// Boquilhas repository contract, the bq-context traversal seam and the consumed P2-T05 reads
/// (repairer register + machine assignments), with the contracted persistence semantics.
/// </summary>
/// <remarks>
/// <para>
/// The P2-T07 HTTP-class tests exercise the real <see cref="BoquilhasService"/> over this store
/// (with the REAL Job On/Tool services over the accepted <see cref="P2T04TestStore"/>), so they
/// prove the transport, the policies and the orchestration without requiring a disposable
/// PostgreSQL database. The schema itself is proven separately by the env-gated DB-class tests.
/// </para>
/// <para>
/// Every write is built in locals and committed only at the end, so a forced failure leaves
/// NOTHING behind �?" the same unit-of-work the real repository implements with database
/// transactions. The in-transaction replay/validation semantics mirror
/// <c>BoquilhasRepository</c> exactly (the balance-relative refusals and the Entrada facts are
/// computed from the before-state ledger; the one-active-anchor scans are re-asserted).</para>
/// </remarks>
internal sealed class P2T07TestStore :
    IBoquilhasRepository,
    IBoquilhasContextRead,
    IRepairerRepository,
    IMachineRepairerAssignmentRepository
{
    /// <summary>The accepted P2-T04 store backing the real Job On/Tool services.</summary>
    public P2T04TestStore JobOnToolStore { get; } = new();

    /// <summary>The traversal mirror: canonical Tools seeded through the Boquilhas arrangement.</summary>
    private readonly Dictionary<Guid, DMO.Domain.Tools.Tool> _tools = [];

    /// <summary>The traversal mirror: REAL bq_contexts rows seeded through the Job On arrangement.</summary>
    private readonly Dictionary<Guid, BqContextRead> _bqContexts = [];

    private readonly Dictionary<Guid, BoquilhaAggregate> _aggregates = [];
    private readonly Dictionary<Guid, IReadOnlyList<MovementAuditEntry>> _audits = [];
    private readonly Dictionary<Guid, Repairer> _repairers = [];
    private readonly Dictionary<string, MachineRepairerAssignment> _assignments = [];

    /// <summary>The number of stored aggregates.</summary>
    public int AggregateCount => _aggregates.Count;

    /// <summary>When set, an aggregate create fails after the row would have been written.</summary>
    public bool FailCreate { get; set; }

    /// <summary>When set, a movement append fails after the row would have been written.</summary>
    public bool FailAppend { get; set; }

    /// <summary>When set, a close fails after the snapshot would have been written.</summary>
    public bool FailClose { get; set; }

    /// <summary>When set, a reopen fails after the reopen record would have been written.</summary>
    public bool FailReopen { get; set; }

    // ---------------------------------------------------------------- arrangement helpers

    /// <summary>Seeds a canonical Tool and returns it (accepted P2-T04 store arrangement).</summary>
    public DMO.Domain.Tools.Tool SeedTool(
        ToolType type,
        string reference,
        string lot,
        Processo? processo = null,
        int? quantity = null,
        params string[] machines)
    {
        var tool = JobOnToolStore.SeedTool(type, reference, lot, processo, quantity, machines);
        _tools[tool.ToolId.Value] = tool;
        return tool;
    }

    /// <summary>
    /// Seeds a production occurrence with its REAL BQ context (frozen triple) through the accepted
    /// P2-T04 arrangement, and registers the context in the traversal mirror. Returns the
    /// occurrence.
    /// </summary>
    public DMO.Domain.JobOn.JobOn SeedJobOnWithBqContext(
        string reference,
        string productionNumber,
        string machine,
        Guid toolId,
        string toolReference,
        string toolLot,
        DateOnly? productionDate = null)
    {
        var contextId = Guid.NewGuid();
        var context = new ToolContext(
            ToolContextType.Bq,
            contextId,
            DMO.Domain.JobOn.JobOnId.New(),
            ToolId.From(toolId),
            new ToolContextSnapshot(ToolType.Bq, toolReference, toolLot));

        var jobOn = JobOnToolStore.SeedJobOn(
            reference,
            productionNumber,
            machine,
            productionDate,
            copiedFromJobOnId: null,
            contexts: [context]);

        // The traversal mirror resolves the STORED Job On identity (arrangement consistency).
        _bqContexts[contextId] = new BqContextRead(
            contextId,
            jobOn.JobOnId.Value,
            toolId,
            "BQ",
            toolReference,
            toolLot);

        return jobOn;
    }

    /// <summary>Seeds a repairer into the consumed register (P2-T05 shape).</summary>
    public Repairer SeedRepairer(string name)
    {
        var repairer = new Repairer(RepairerId.New(), name, Version: 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        _repairers[repairer.RepairerId.Value] = repairer;
        return repairer;
    }

    /// <summary>Sets the current assignment of ONE machine (P2-T05 shape; independent machines).</summary>
    public void SeedAssignment(string machine, Guid repairerId)
    {
        var now = DateTimeOffset.UtcNow;
        _assignments[machine] = new MachineRepairerAssignment(
            Guid.NewGuid(),
            MachineCode.From(machine),
            RepairerId.From(repairerId),
            Version: 1,
            now,
            now);
    }

    /// <summary>Bumps the aggregate version directly, to arrange a stale-version scenario.</summary>
    public void BumpAggregateVersion(Guid boquilhasId)
    {
        if (!_aggregates.TryGetValue(boquilhasId, out var aggregate))
        {
            return;
        }

        _aggregates[boquilhasId] = aggregate with { Version = aggregate.Version + 1 };
    }

    /// <summary>Removes an aggregate directly, to arrange a vanished-row scenario.</summary>
    public void RemoveAggregate(Guid boquilhasId) => _aggregates.Remove(boquilhasId);

    /// <summary>Closes an aggregate directly, to arrange a closed-state scenario.</summary>
    public void CloseAggregateDirectly(Guid boquilhasId)
    {
        if (!_aggregates.TryGetValue(boquilhasId, out var aggregate))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var balance = aggregate.Balance;
        var snapshot = new CloseSnapshot(
            Guid.NewGuid(),
            boquilhasId,
            aggregate.CreatedByUserId,
            now,
            aggregate.Inicio?.Quantity ?? 0,
            aggregate.OpeningDate,
            balance.Disponivel,
            balance.EmReparacao,
            balance.Irreparavel,
            balance.EntradaExcecional,
            aggregate.UtilisationPercent,
            now);

        _aggregates[boquilhasId] = aggregate with
        {
            Status = BoquilhaStatus.Closed,
            Version = aggregate.Version + 1,
            CloseSnapshots = [.. aggregate.CloseSnapshots, snapshot],
        };
    }

    // ---------------------------------------------------------------- reads

    Task<BoquilhaAggregate?> IBoquilhasRepository.GetByIdAsync(Guid boquilhasId, CancellationToken cancellationToken) =>
        Task.FromResult(_aggregates.TryGetValue(boquilhasId, out var aggregate) ? aggregate : null);

    public Task<IReadOnlyList<BoquilhaListItem>> ListAsync(
        BoquilhasListQuery query,
        CancellationToken cancellationToken)
    {
        var state = string.IsNullOrWhiteSpace(query.State) ? "active" : query.State;
        var rows = new List<BoquilhaListItem>();

        foreach (var aggregate in _aggregates.Values)
        {
            if (!string.Equals(
                    BoquilhaStatusTokens.ToToken(aggregate.Status),
                    state,
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (query.Reference is { } reference && TraversalReference(aggregate) != reference)
            {
                continue;
            }

            if (query.Lot is { } lot && TraversalLot(aggregate) != lot)
            {
                continue;
            }

            if (query.Machine is not null
                && !aggregate.Machines.Any(machine => machine.Value == query.Machine))
            {
                continue;
            }

            var balance = aggregate.Balance;
            rows.Add(new BoquilhaListItem(
                aggregate.BoquilhasId.Value,
                aggregate.Version,
                BoquilhaStatusTokens.ToToken(aggregate.Status),
                aggregate.BqId,
                aggregate.ToolId,
                TraversalReference(aggregate),
                TraversalLot(aggregate),
                aggregate.Machines.Select(machine => machine.Value).ToList(),
                aggregate.OpeningDate,
                aggregate.Inicio?.Quantity ?? 0,
                balance.Disponivel,
                balance.EmReparacao,
                balance.Irreparavel,
                balance.EntradaExcecional));
        }

        var ordered = rows
            .OrderBy(row => row.OpeningDate)
            .ThenBy(row => row.BoquilhasId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return Task.FromResult<IReadOnlyList<BoquilhaListItem>>(ordered);
    }

    public Task<int> CountListAsync(BoquilhasListQuery query, CancellationToken cancellationToken) =>
        CountAsync(ListAsync(new BoquilhasListQuery(
            query.State,
            query.Reference,
            query.Lot,
            query.Machine,
            Page: 1,
            PageSize: int.MaxValue), cancellationToken));

    private static async Task<int> CountAsync(Task<IReadOnlyList<BoquilhaListItem>> matched) =>
        (await matched).Count;

    public Task<IReadOnlyList<HistoryItem>> GetHistoryAsync(
        BoquilhasHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var rows = new List<HistoryItem>();

        foreach (var aggregate in _aggregates.Values)
        {
            if (!string.IsNullOrWhiteSpace(query.State)
                && !string.Equals(
                    BoquilhaStatusTokens.ToToken(aggregate.Status),
                    query.State,
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (query.Reference is { } reference && TraversalReference(aggregate) != reference)
            {
                continue;
            }

            if (query.Lot is { } lot && TraversalLot(aggregate) != lot)
            {
                continue;
            }

            if (query.Machine is not null
                && !aggregate.Machines.Any(machine => machine.Value == query.Machine))
            {
                continue;
            }

            if (query.BusinessDateFrom is { } from
                && !aggregate.Movements.Any(movement => movement.BusinessDate >= from))
            {
                continue;
            }

            if (query.BusinessDateTo is { } to
                && !aggregate.Movements.Any(movement => movement.BusinessDate <= to))
            {
                continue;
            }

            if (query.MovementType is { } movementType
                && !aggregate.Movements.Any(movement =>
                    string.Equals(MovementKindTokens.ToToken(movement.Kind), movementType, StringComparison.Ordinal)))
            {
                continue;
            }

            if (query.RepairerId is { } repairerId
                && !aggregate.Movements.Any(movement => movement.RepairerId == repairerId))
            {
                continue;
            }

            var balance = aggregate.Balance;
            rows.Add(new HistoryItem(
                aggregate.BoquilhasId.Value,
                aggregate.Version,
                BoquilhaStatusTokens.ToToken(aggregate.Status),
                aggregate.BqId,
                aggregate.ToolId,
                TraversalReference(aggregate),
                TraversalLot(aggregate),
                aggregate.Machines.Select(machine => machine.Value).ToList(),
                aggregate.OpeningDate,
                aggregate.Inicio?.Quantity ?? 0,
                balance.Disponivel,
                balance.EmReparacao,
                balance.Irreparavel,
                balance.EntradaExcecional,
                aggregate.Movements.Count,
                aggregate.LastClose?.ClosedAt,
                aggregate.LastClose?.ClosedByUserId,
                aggregate.Movements.Count == 0
                    ? null
                    : aggregate.Movements.Max(movement => movement.RecordedAt)));
        }

        var ordered = rows
            .OrderByDescending(row => row.OpeningDate)
            .ThenByDescending(row => row.BoquilhasId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return Task.FromResult<IReadOnlyList<HistoryItem>>(ordered);
    }

    public async Task<int> CountHistoryAsync(BoquilhasHistoryQuery query, CancellationToken cancellationToken)
    {
        var all = await GetHistoryAsync(new BoquilhasHistoryQuery(
            query.State,
            query.Reference,
            query.Lot,
            query.Machine,
            query.BusinessDateFrom,
            query.BusinessDateTo,
            query.MovementType,
            query.RepairerId,
            Page: 1,
            PageSize: int.MaxValue), cancellationToken);

        return all.Count;
    }

    public Task<bool> HasActiveAggregateForAnchorAsync(
        Guid? bqId,
        Guid? toolId,
        Guid excludeBoquilhasId,
        CancellationToken cancellationToken) =>
        Task.FromResult(_aggregates.Values.Any(aggregate =>
            aggregate.BoquilhasId.Value != excludeBoquilhasId
            && aggregate.IsActive
            && (bqId is not null ? aggregate.BqId == bqId : aggregate.ToolId == toolId)));

    public Task<Guid?> GetLastCloseSnapshotIdForAnchorAsync(
        Guid? bqId,
        Guid? toolId,
        CancellationToken cancellationToken)
    {
        CloseSnapshot? last = null;

        foreach (var aggregate in _aggregates.Values)
        {
            if (bqId is not null ? aggregate.BqId != bqId : aggregate.ToolId != toolId)
            {
                continue;
            }

            var close = aggregate.LastClose;
            if (close is null)
            {
                continue;
            }

            if (last is null
                || close.ClosedAt > last.ClosedAt
                || (close.ClosedAt == last.ClosedAt
                    && close.CloseSnapshotId.CompareTo(last.CloseSnapshotId) > 0))
            {
                last = close;
            }
        }

        return Task.FromResult(last?.CloseSnapshotId);
    }

    public Task<IReadOnlyList<MovementAuditEntry>> GetMovementAuditAsync(
        Guid movementId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MovementAuditEntry>>(
            _audits.TryGetValue(movementId, out var entries)
                ? entries.OrderBy(entry => entry.EditedAt).ToList()
                : []);

    // ---------------------------------------------------------------- writes

    public async Task<BoquilhaAggregate> CreatedAsync(BoqCreateUnit unit, CancellationToken cancellationToken)
    {
        if (FailCreate)
        {
            throw new InvalidOperationException("Forced create failure (test arrangement).");
        }

        if (await HasActiveAggregateForAnchorAsync(unit.BqId, unit.ToolId, Guid.Empty, cancellationToken))
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.ActiveAggregateExists,
                "Já existe um registo ativo para este contexto BQ; nada foi criado.");
        }

        if (unit.BqId is { } bqId && !_bqContexts.ContainsKey(bqId))
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.BqContextNotFound,
                "O contexto BQ indicado não existe; nada foi criado.");
        }

        if (unit.ToolId is { } toolId)
        {
            if (!_tools.TryGetValue(toolId, out var tool))
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.ToolNotFound,
                    "A ferramenta indicada não existe; nada foi criado.");
            }

            if (tool.Type != ToolType.Bq)
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.ToolTypeMismatch,
                    "A ferramenta indicada não é do tipo BQ; nada foi criado.");
            }
        }

        var now = DateTimeOffset.UtcNow;
        var aggregateId = Guid.NewGuid();
        var movement = new BoquilhaMovement(
            MovementId.New(),
            aggregateId,
            MovementKind.Inicio,
            unit.InitialQuantity,
            unit.OpeningDate,
            now,
            unit.CreatedByUserId,
            Machine: null,
            RepairerId: null,
            ExpectedReturnQuantity: null,
            ExcessReceivedQuantity: null,
            Observations: null,
            Version: 1,
            now,
            now);

        var aggregate = new BoquilhaAggregate(
            BoquilhasId.From(aggregateId),
            unit.BqId,
            unit.ToolId,
            BoquilhaStatus.Active,
            unit.OpeningDate,
            unit.UtilisationPercent,
            unit.Observations,
            unit.CreatedByUserId,
            Version: 1,
            now,
            now,
            unit.Machines.Select(MachineCode.From).ToList(),
            [movement],
            [],
            []);

        _aggregates[aggregateId] = aggregate;
        return aggregate;
    }

    public async Task<BoquilhaMovement> AppendMovementAsync(BoqAppendUnit unit, CancellationToken cancellationToken)
    {
        if (!_aggregates.TryGetValue(unit.BoquilhasId, out var aggregate))
        {
            throw new ConcurrencyConflictException("O registo já não existe; nada foi guardado.");
        }

        if (aggregate.Version != unit.ExpectedAggregateVersion)
        {
            throw new ConcurrencyConflictException(
                $"O registo foi alterado em concorrência (esperada {unit.ExpectedAggregateVersion}, atual {aggregate.Version}).");
        }

        var kind = MovementKindTokens.Parse(unit.MovementType)
            ?? throw new ArgumentOutOfRangeException(nameof(unit.MovementType), unit.MovementType, "Tipo fora do conjunto fechado.");

        if (!aggregate.IsActive)
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.AggregateClosed,
                "O registo está fechado.");
        }

        if (kind == MovementKind.Inicio)
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.OnlyOneInicio,
                "Só existe um Início por registo.");
        }

        ValidateAgainstLedger(aggregate, kind, unit.Quantity, unit.Machine, unit.RepairerId, aggregate.Movements);

        int? expectedReturn = null;
        int? excessReceived = null;

        if (kind == MovementKind.Entrada)
        {
            expectedReturn = BalanceProjection.ExpectedReturn(aggregate.Movements);
            excessReceived = Math.Max(0, unit.Quantity - expectedReturn.Value);
        }

        if (FailAppend)
        {
            throw new InvalidOperationException("Forced append failure (test arrangement).");
        }

        var now = DateTimeOffset.UtcNow;
        var movement = new BoquilhaMovement(
            MovementId.New(),
            unit.BoquilhasId,
            kind,
            unit.Quantity,
            unit.BusinessDate,
            now,
            unit.RecordedByUserId,
            unit.Machine,
            unit.RepairerId,
            expectedReturn,
            excessReceived,
            unit.Observations,
            Version: 1,
            now,
            now);

        _aggregates[unit.BoquilhasId] = aggregate with
        {
            Version = aggregate.Version + 1,
            Movements = [.. aggregate.Movements, movement],
        };

        return movement;
    }

    public Task<BoqEditResult> EditMovementAsync(BoqEditUnit unit, CancellationToken cancellationToken)
    {
        if (!_aggregates.TryGetValue(unit.BoquilhasId, out var aggregate))
        {
            throw new ConcurrencyConflictException("O registo já não existe; nada foi guardado.");
        }

        if (aggregate.Version != unit.ExpectedAggregateVersion)
        {
            throw new ConcurrencyConflictException(
                $"O registo foi alterado em concorrência (esperada {unit.ExpectedAggregateVersion}, atual {aggregate.Version}).");
        }

        if (!aggregate.IsActive)
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.AggregateClosed,
                "O registo está fechado.");
        }

        var movement = aggregate.Movements.FirstOrDefault(candidate => candidate.MovementId.Value == unit.MovementId)
            ?? throw new ConcurrencyConflictException("O movimento já não existe.");

        if (movement.Version != unit.ExpectedMovementVersion)
        {
            throw new ConcurrencyConflictException(
                $"O movimento foi alterado em concorrência (esperada {unit.ExpectedMovementVersion}, atual {movement.Version}).");
        }

        var beforeState = aggregate.Movements
            .Where(candidate => candidate.MovementId.Value != unit.MovementId)
            .ToList();

        ValidateAgainstLedger(aggregate, movement.Kind, unit.Quantity, unit.Machine, unit.RepairerId, beforeState);

        int? expectedReturn = null;
        int? excessReceived = null;

        if (movement.Kind == MovementKind.Entrada)
        {
            expectedReturn = BalanceProjection.ExpectedReturn(beforeState);
            excessReceived = Math.Max(0, unit.Quantity - expectedReturn.Value);
        }

        var now = DateTimeOffset.UtcNow;
        var updated = movement with
        {
            Quantity = unit.Quantity,
            BusinessDate = unit.BusinessDate,
            Machine = unit.Machine,
            RepairerId = unit.RepairerId,
            ExpectedReturnQuantity = expectedReturn,
            ExcessReceivedQuantity = excessReceived,
            Observations = unit.Observations,
            Version = movement.Version + 1,
            UpdatedAt = now,
        };

        _audits[unit.MovementId] =
        [
            .. _audits.TryGetValue(unit.MovementId, out var existing) ? existing : [],
            new MovementAuditEntry(
                Guid.NewGuid(),
                unit.MovementId,
                unit.EditedByUserId,
                now,
                movement.Quantity,
                unit.Quantity,
                movement.BusinessDate,
                unit.BusinessDate,
                movement.Machine,
                unit.Machine,
                movement.RepairerId,
                unit.RepairerId,
                movement.Observations,
                unit.Observations,
                now),
        ];

        _aggregates[unit.BoquilhasId] = aggregate with
        {
            Version = aggregate.Version + 1,
            Movements = [.. beforeState, updated],
        };

        return Task.FromResult(new BoqEditResult(updated, aggregate.Version + 1));
    }

    public Task<BoqCloseResult> CloseAsync(BoqCloseUnit unit, CancellationToken cancellationToken)
    {
        if (!_aggregates.TryGetValue(unit.BoquilhasId, out var aggregate))
        {
            throw new ConcurrencyConflictException("O registo já não existe; nada foi guardado.");
        }

        if (aggregate.Version != unit.ExpectedVersion)
        {
            throw new ConcurrencyConflictException(
                $"O registo foi alterado em concorrência (esperada {unit.ExpectedVersion}, atual {aggregate.Version}).");
        }

        if (!aggregate.IsActive)
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.AlreadyClosed,
                "O registo já está fechado.");
        }

        if (FailClose)
        {
            throw new InvalidOperationException("Forced close failure (test arrangement).");
        }

        var now = DateTimeOffset.UtcNow;
        var balance = aggregate.Balance;
        var snapshot = new CloseSnapshot(
            Guid.NewGuid(),
            unit.BoquilhasId,
            unit.ClosedByUserId,
            now,
            aggregate.Inicio?.Quantity ?? 0,
            aggregate.OpeningDate,
            balance.Disponivel,
            balance.EmReparacao,
            balance.Irreparavel,
            balance.EntradaExcecional,
            aggregate.UtilisationPercent,
            now);

        _aggregates[unit.BoquilhasId] = aggregate with
        {
            Status = BoquilhaStatus.Closed,
            Version = aggregate.Version + 1,
            CloseSnapshots = [.. aggregate.CloseSnapshots, snapshot],
        };

        return Task.FromResult(new BoqCloseResult(snapshot, aggregate.Version + 1));
    }

    public async Task<BoqReopenResult> ReopenAsync(BoqReopenUnit unit, CancellationToken cancellationToken)
    {
        if (!_aggregates.TryGetValue(unit.BoquilhasId, out var aggregate))
        {
            throw new ConcurrencyConflictException("O registo já não existe; nada foi guardado.");
        }

        if (aggregate.Version != unit.ExpectedVersion)
        {
            throw new ConcurrencyConflictException(
                $"O registo foi alterado em concorrência (esperada {unit.ExpectedVersion}, atual {aggregate.Version}).");
        }

        if (string.IsNullOrWhiteSpace(unit.Reason))
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.ConstraintViolation,
                "O motivo da reabertura é obrigatório.",
                validatorToken: BoquilhasValidationErrors.ReopenReasonRequired);
        }

        if (!aggregate.IsClosed)
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.NotClosed,
                "O registo não está fechado.");
        }

        if (aggregate.LastClose is null
            || aggregate.LastClose.CloseSnapshotId
            != await GetLastCloseSnapshotIdForAnchorAsync(aggregate.BqId, aggregate.ToolId, cancellationToken))
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.NotLastClosed,
                "Este registo não é o último fechado do contexto.");
        }

        if (await HasActiveAggregateForAnchorAsync(
                aggregate.BqId,
                aggregate.ToolId,
                unit.BoquilhasId,
                cancellationToken))
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.ActiveAggregateExists,
                "Já existe um registo ativo para este contexto.");
        }

        if (FailReopen)
        {
            throw new InvalidOperationException("Forced reopen failure (test arrangement).");
        }

        var now = DateTimeOffset.UtcNow;
        var reopen = new ReopeningRecord(
            Guid.NewGuid(),
            unit.BoquilhasId,
            aggregate.LastClose.CloseSnapshotId,
            unit.ReopenedByUserId,
            now,
            unit.Reason.Trim(),
            now);

        _aggregates[unit.BoquilhasId] = aggregate with
        {
            Status = BoquilhaStatus.Active,
            Version = aggregate.Version + 1,
            Reopenings = [.. aggregate.Reopenings, reopen],
        };

        return new BoqReopenResult(reopen, aggregate.Version + 1);
    }

    public Task<BoquilhaAggregate> UpdateOpeningFactsAsync(BoqOpeningFactsUnit unit, CancellationToken cancellationToken)
    {
        if (!_aggregates.TryGetValue(unit.BoquilhasId, out var aggregate))
        {
            throw new ConcurrencyConflictException("O registo já não existe; nada foi guardado.");
        }

        if (aggregate.Version != unit.ExpectedVersion)
        {
            throw new ConcurrencyConflictException(
                $"O registo foi alterado em concorrência (esperada {unit.ExpectedVersion}, atual {aggregate.Version}).");
        }

        if (!aggregate.IsActive)
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.AggregateClosed,
                "O registo está fechado.");
        }

        var updated = aggregate with
        {
            OpeningDate = unit.OpeningDate,
            UtilisationPercent = unit.UtilisationPercent,
            Observations = unit.Observations,
            Machines = unit.Machines.Select(MachineCode.From).ToList(),
            Version = aggregate.Version + 1,
        };

        _aggregates[unit.BoquilhasId] = updated;
        return Task.FromResult(updated);
    }

    // ---------------------------------------------------------------- consumed reads (P2-T05)

    Task<Repairer?> IRepairerRepository.GetByIdAsync(Guid repairerId, CancellationToken cancellationToken) =>
        Task.FromResult(_repairers.TryGetValue(repairerId, out var repairer) ? repairer : null);

    Task<IReadOnlyList<Repairer>> IRepairerRepository.ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Repairer>>(
            _repairers.Values.OrderBy(repairer => repairer.Name, StringComparer.Ordinal).ToList());

    public Task<Repairer> CreatedAsync(Repairer repairer, CancellationToken cancellationToken)
    {
        _repairers[repairer.RepairerId.Value] = repairer;
        return Task.FromResult(repairer);
    }

    public Task<Repairer> RenamedAsync(Repairer repairer, CancellationToken cancellationToken)
    {
        _repairers[repairer.RepairerId.Value] = repairer;
        return Task.FromResult(repairer);
    }

    public Task<MachineRepairerAssignment?> GetByMachineAsync(string machine, CancellationToken cancellationToken) =>
        Task.FromResult(_assignments.TryGetValue(machine, out var assignment) ? assignment : null);

    Task<IReadOnlyList<MachineRepairerAssignment>> IMachineRepairerAssignmentRepository.ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MachineRepairerAssignment>>(_assignments.Values.ToList());

    public Task<MachineRepairerAssignment> SetAsync(
        MachineRepairerAssignment assignment,
        CancellationToken cancellationToken)
    {
        _assignments[assignment.Machine.Value] = assignment;
        return Task.FromResult(assignment);
    }

    public Task ClearedAsync(string machine, int expectedVersion, CancellationToken cancellationToken)
    {
        _assignments.Remove(machine);
        return Task.CompletedTask;
    }

    // ---------------------------------------------------------------- IBoquilhasContextRead

    public Task<BqContextRead?> GetBqContextAsync(Guid bqId, CancellationToken cancellationToken) =>
        Task.FromResult(_bqContexts.TryGetValue(bqId, out var context) ? context : null);

    // ---------------------------------------------------------------- helpers

    private static void ValidateAgainstLedger(
        BoquilhaAggregate aggregate,
        MovementKind kind,
        int quantity,
        string? machine,
        Guid? repairerId,
        IReadOnlyList<BoquilhaMovement> beforeState)
    {
        if (kind == MovementKind.Saida)
        {
            if (machine is null)
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.ConstraintViolation,
                    "Uma Saída externa requer a máquina de contexto.",
                    validatorToken: BoquilhasValidationErrors.MachineRequired);
            }

            if (repairerId is null)
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.ConstraintViolation,
                    "Uma Saída externa requer o reparador final selecionado.",
                    validatorToken: BoquilhasValidationErrors.RepairerRequired);
            }
        }

        if (machine is not null
            && !aggregate.Machines.Any(candidate => candidate.Value == machine))
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.ConstraintViolation,
                $"A máquina '{machine}' não pertence ao conjunto do registo.",
                validatorToken: BoquilhasValidationErrors.MachineNotInAggregate);
        }

        if (kind is MovementKind.Saida or MovementKind.Irreparavel)
        {
            var before = BalanceProjection.Replay(beforeState);

            if (kind == MovementKind.Saida && quantity > before.Disponivel)
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.SaidaExceedsAvailable,
                    "A quantidade de Saída excede o Disponível.");
            }

            if (kind == MovementKind.Irreparavel && quantity > before.EmReparacao)
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.IrreparavelExceedsInRepair,
                    "A quantidade de Irreparável excede o Em reparação.");
            }
        }
    }

    private string? TraversalReference(BoquilhaAggregate aggregate) =>
        aggregate.IsProductionLinked
            ? _bqContexts.TryGetValue(aggregate.BqId!.Value, out var context) ? context.ToolReference : null
            : _tools.TryGetValue(aggregate.ToolId!.Value, out var tool) ? tool.Reference : null;

    private string? TraversalLot(BoquilhaAggregate aggregate) =>
        aggregate.IsProductionLinked
            ? _bqContexts.TryGetValue(aggregate.BqId!.Value, out var context) ? context.ToolLot : null
            : _tools.TryGetValue(aggregate.ToolId!.Value, out var tool) ? tool.Lot : null;
}