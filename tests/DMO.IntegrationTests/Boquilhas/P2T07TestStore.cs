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
/// The register model of the OWNER CLARIFICATION: <c>boquilhas_id</c> is the register identity of
/// one REAL production/BQ context (one register per <c>bq_id</c>, in-memory enforced), created
/// WITHOUT any quantity movement; movements are the closed three types; the outstanding is derived
/// by replay, never stored; appends are unversioned; the edit is the guarded same-row UPDATE plus
/// the audit row (one transaction, no second quantity event); there is NO lifecycle state machine.
/// Every write is built in locals and committed only at the end, so a forced failure leaves
/// NOTHING behind — the same unit-of-work the real repository implements with database
/// transactions.</para>
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

    private readonly Dictionary<Guid, BoquilhaRegister> _registers = [];
    private readonly Dictionary<Guid, IReadOnlyList<MovementAuditEntry>> _audits = [];
    private readonly Dictionary<Guid, Repairer> _repairers = [];
    private readonly Dictionary<string, MachineRepairerAssignment> _assignments = [];
    private readonly Dictionary<Guid, JobOnFacts> _jobOnFacts = [];

    /// <summary>The number of stored registers.</summary>
    public int RegisterCount => _registers.Count;

    /// <summary>When set, a register create fails after the row would have been written.</summary>
    public bool FailCreate { get; set; }

    /// <summary>When set, a movement append fails after the row would have been written.</summary>
    public bool FailAppend { get; set; }

    /// <summary>When set, a movement edit fails after the row would have been written.</summary>
    public bool FailEdit { get; set; }

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
    /// Seeds a production and a REAL <c>bq_contexts</c> row for it (frozen triple), returning the
    /// <c>(jobOnId, bqId)</c> pair. The BQ slot is arranged through the Job On store (the real
    /// service composition creates it through <c>IJobOnService.UpdateAsync</c>).
    /// </summary>
    public (Guid JobOnId, Guid BqId) SeedProductionWithBq(
        string reference,
        string productionNumber,
        Guid toolId,
        string machine = "B1",
        DateOnly? productionDate = null)
    {
        var tool = _tools[toolId];
        var bqId = Guid.NewGuid();

        var context = new ToolContext(
            ToolContextType.Bq,
            bqId,
            DMO.Domain.JobOn.JobOnId.New(),
            ToolId.From(toolId),
            new ToolContextSnapshot(tool.Type, tool.Reference, tool.Lot));

        var jobOn = JobOnToolStore.SeedJobOn(
            reference,
            productionNumber,
            machine,
            productionDate,
            copiedFromJobOnId: null,
            contexts: [context]);

        _bqContexts[bqId] = new BqContextRead(
            bqId,
            jobOn.JobOnId.Value,
            toolId,
            DMO.Application.Tools.ToolTokens.ToToken(tool.Type),
            tool.Reference,
            tool.Lot);

        _jobOnFacts[jobOn.JobOnId.Value] = new JobOnFacts(
            reference,
            productionNumber,
            MachineCode.From(machine),
            productionDate);

        return (jobOn.JobOnId.Value, bqId);
    }

    /// <summary>Directly seeds a register with a ledger (test arrangement; server semantics not bypassed).</summary>
    public BoquilhaRegister SeedRegister(
        Guid bqId,
        params (MovementKind Kind, int Quantity, DateOnly BusinessDate, string? Machine, Guid? RepairerId)[] movements)
    {
        var created = DateTimeOffset.Parse("2026-09-01T08:00:00Z");
        var registerId = Guid.NewGuid();

        var rows = new List<BoquilhaMovement>();

        foreach (var (kind, quantity, businessDate, machine, repairerId) in movements)
        {
            var recordedAt = created.AddMinutes(rows.Count + 1);
            rows.Add(new BoquilhaMovement(
                MovementId.From(Guid.NewGuid()),
                registerId,
                kind,
                quantity,
                businessDate,
                recordedAt,
                P2T07TestHost.ActorUserId,
                machine,
                repairerId,
                Observations: null,
                Version: 1,
                created.AddMinutes(rows.Count + 1),
                created.AddMinutes(rows.Count + 1)));
        }

        var register = new BoquilhaRegister(
            BoquilhasId.From(registerId),
            bqId,
            P2T07TestHost.ActorUserId,
            created,
            rows);

        _registers[registerId] = register;
        _audits[registerId] = [];

        return register;
    }

    /// <summary>Seeds a repairer-register row (consumed; the Boquilhas service never administers).</summary>
    public Repairer SeedRepairer(string name, Guid? repairerId = null)
    {
        var repairer = new Repairer(
            RepairerId.From(repairerId ?? Guid.NewGuid()),
            name,
            Version: 1,
            CreatedAt: DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            UpdatedAt: DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

        _repairers[repairer.RepairerId.Value] = repairer;
        return repairer;
    }

    /// <summary>Seeds one machine → repairer assignment (consumed; never administered here).</summary>
    public MachineRepairerAssignment SeedAssignment(string machine, Guid repairerId)
    {
        var assignment = new MachineRepairerAssignment(
            Guid.NewGuid(),
            MachineCode.From(machine),
            RepairerId.From(repairerId),
            Version: 1,
            CreatedAt: DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            UpdatedAt: DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

        _assignments[machine] = assignment;
        return assignment;
    }

    /// <summary>Changes the current assignment (a later assignment change must never rewrite old movements).</summary>
    public MachineRepairerAssignment ChangeAssignment(string machine, Guid repairerId) =>
        SeedAssignment(machine, repairerId);

    /// <summary>Removes a production occurrence's mirror facts (arranged deleted-row scenarios).</summary>
    public void RemoveProduction(Guid jobOnId)
    {
        JobOnToolStore.RemoveJobOn(jobOnId);
        _jobOnFacts.Remove(jobOnId);
    }

    // ---- IBoquilhasContextRead (narrow bq_id traversal seam; read-only) -----------------------

    public Task<BqContextRead?> GetBqContextAsync(Guid bqId, CancellationToken cancellationToken) =>
        Task.FromResult(ResolveContext(bqId));

    /// <summary>
    /// Resolves a REAL <c>bq_contexts</c> row: seeded mirrors first, then the LIVE rows created
    /// through the real <c>IJobOnService.UpdateAsync</c> association flow (single source of truth
    /// = the Job On test store's context map, read-only).
    /// </summary>
    private BqContextRead? ResolveContext(Guid bqId)
    {
        if (_bqContexts.TryGetValue(bqId, out var seed))
        {
            return seed;
        }

        foreach (var jobOnId in JobOnToolStore.JobOnIds())
        {
            foreach (var context in JobOnToolStore.ContextsOf(jobOnId))
            {
                if (context.ContextType != ToolContextType.Bq || context.ContextId != bqId)
                {
                    continue;
                }

                return new BqContextRead(
                    bqId,
                    jobOnId,
                    context.ToolId.Value,
                    DMO.Application.Tools.ToolTokens.ToToken(context.Frozen.Type),
                    context.Frozen.Reference,
                    context.Frozen.Lot);
            }
        }

        return null;
    }

    // ---- consumed P2-T05 reads (explicit interface members: the register/assignment contracts share
// the ListAsync/GetByIdAsync signatures with the Boquilhas repository and resolve by interface) ---

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

    // ---- IBoquilhasRepository: reads -----------------------------------------------------------

    public Task<BoquilhaRegister?> GetByIdAsync(Guid boquilhasId, CancellationToken cancellationToken) =>
        Task.FromResult(_registers.TryGetValue(boquilhasId, out var register) ? register : null);

    public Task<IReadOnlyList<RegisterListItem>> ListAsync(
        BoquilhasListQuery query,
        CancellationToken cancellationToken)
    {
        var rows = _registers.Values
            .OrderBy(register => register.CreatedAt)
            .ThenBy(register => register.BoquilhasId.Value)
            .Select(ToListItem)
            .ToList();

        if (query.Reference is { } reference)
        {
            rows = rows.Where(row => row.Reference == reference).ToList();
        }

        if (query.Lot is { } lot)
        {
            rows = rows.Where(row => row.Lot == lot).ToList();
        }

        var page = rows
            .Skip((Math.Max(query.Page, 1) - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        IReadOnlyList<RegisterListItem> result = page;
        return Task.FromResult(result);
    }

    public Task<int> CountListAsync(BoquilhasListQuery query, CancellationToken cancellationToken)
    {
        var all = _registers.Values
            .Select(ToListItem)
            .ToList();

        if (query.Reference is { } reference)
        {
            all = all.Where(row => row.Reference == reference).ToList();
        }

        if (query.Lot is { } lot)
        {
            all = all.Where(row => row.Lot == lot).ToList();
        }

        return Task.FromResult(all.Count);
    }

    public Task<IReadOnlyList<HistoryMovementItem>> GetHistoryAsync(
        BoquilhasHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var rows = new List<HistoryMovementItem>();

        foreach (var register in _registers.Values)
        {
            var item = ToListItem(register);

            foreach (var movement in register.Ledger)
            {
                var row = new HistoryMovementItem(
                    movement.MovementId.Value,
                    register.BoquilhasId.Value,
                    MovementKindTokens.ToToken(movement.Kind),
                    movement.Quantity,
                    movement.BusinessDate,
                    movement.RecordedAt,
                    movement.RecordedByUserId,
                    movement.Machine,
                    movement.RepairerId,
                    movement.Observations,
                    item.Reference,
                    item.Lot,
                    item.ProductionNumber,
                    item.ProductionMachine);

                rows.Add(row);
            }
        }

        if (query.Reference is { } reference)
        {
            rows = rows.Where(row => row.Reference == reference).ToList();
        }

        if (query.Lot is { } lot)
        {
            rows = rows.Where(row => row.Lot == lot).ToList();
        }

        if (query.Machine is { } machine)
        {
            rows = rows.Where(row => row.Machine == machine).ToList();
        }

        if (query.BusinessDateFrom is { } from)
        {
            rows = rows.Where(row => row.BusinessDate >= from).ToList();
        }

        if (query.BusinessDateTo is { } to)
        {
            rows = rows.Where(row => row.BusinessDate <= to).ToList();
        }

        if (query.MovementType is { } movementType)
        {
            rows = rows.Where(row => row.MovementType == movementType).ToList();
        }

        if (query.RepairerId is { } repairerId)
        {
            rows = rows.Where(row => row.RepairerId == repairerId).ToList();
        }

        var page = rows
            .OrderBy(row => row.RecordedAt)
            .ThenBy(row => row.MovementId)
            .Skip((Math.Max(query.Page, 1) - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        IReadOnlyList<HistoryMovementItem> result = page;
        return Task.FromResult(result);
    }

    public Task<int> CountHistoryAsync(BoquilhasHistoryQuery query, CancellationToken cancellationToken)
    {
        var all = _registers.Values
            .SelectMany(register => register.Ledger.Select(movement => new { register, movement }))
            .Where(pair => query.Reference is null || ToListItem(pair.register).Reference == query.Reference)
            .Where(pair => query.Lot is null || ToListItem(pair.register).Lot == query.Lot)
            .Where(pair => query.Machine is null || pair.movement.Machine == query.Machine)
            .Where(pair => query.BusinessDateFrom is null || pair.movement.BusinessDate >= query.BusinessDateFrom)
            .Where(pair => query.BusinessDateTo is null || pair.movement.BusinessDate <= query.BusinessDateTo)
            .Where(pair => query.MovementType is null || MovementKindTokens.ToToken(pair.movement.Kind) == query.MovementType)
            .Where(pair => query.RepairerId is null || pair.movement.RepairerId == query.RepairerId)
            .ToList();

        return Task.FromResult(all.Count);
    }

    public Task<IReadOnlyList<MovementAuditEntry>> GetMovementAuditAsync(
        Guid movementId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<MovementAuditEntry> result = _audits.Values
            .SelectMany(entries => entries)
            .Where(entry => entry.MovementId == movementId)
            .OrderBy(entry => entry.EditedAt)
            .ThenBy(entry => entry.MovementAuditId)
            .ToList();

        return Task.FromResult(result);
    }

    // ---- IBoquilhasRepository: writes -----------------------------------------------------------

    public async Task<BoquilhaRegister> CreatedAsync(
        BoqCreateUnit unit,
        CancellationToken cancellationToken)
    {
        // Production association first (the service re-checks the same fact).
        if (ResolveContext(unit.BqId) is null)
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.BqContextNotFound,
                "O contexto BQ indicado não existe; nada foi criado.");
        }

        if (_registers.Values.Any(register => register.BqId == unit.BqId))
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.RegisterExists,
                "Já existe um registo de Boquilhas para esta produção; nada foi criado.");
        }

        var register = new BoquilhaRegister(
            BoquilhasId.From(Guid.NewGuid()),
            unit.BqId,
            unit.CreatedByUserId,
            DateTimeOffset.UtcNow,
            []);

        if (FailCreate)
        {
            throw new InvalidOperationException("Forced register-create failure.");
        }

        _registers[register.BoquilhasId.Value] = register;
        _audits[register.BoquilhasId.Value] = [];

        return register;
    }

    public async Task<BoquilhaMovement> AppendMovementAsync(
        BoqAppendUnit unit,
        CancellationToken cancellationToken)
    {
        if (!_registers.TryGetValue(unit.BoquilhasId, out var register))
        {
            throw new ConcurrencyConflictException($"O registo '{unit.BoquilhasId}' já não existe; nada foi guardado.");
        }

        if (ResolveContext(register.BqId) is null)
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.BqContextNotFound,
                "O contexto BQ referenciado deixou de existir; nada foi guardado.");
        }

        var kind = MovementKindTokens.Parse(unit.MovementType)
            ?? throw new ArgumentOutOfRangeException(nameof(unit.MovementType), unit.MovementType, "Closed set.");

        if (kind == MovementKind.Saida)
        {
            if (unit.Machine is null)
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.ConstraintViolation,
                    "Uma Saída requer a máquina de contexto; nada foi guardado.",
                    validatorToken: BoquilhasValidationErrors.MachineRequired);
            }

            if (unit.RepairerId is null)
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.ConstraintViolation,
                    "Uma Saída requer o reparador usado no movimento; nada foi guardado.",
                    validatorToken: BoquilhasValidationErrors.RepairerRequired);
            }
        }

        if (unit.Machine is not null && !MachineCode.IsKnown(unit.Machine))
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.ConstraintViolation,
                $"A máquina '{unit.Machine}' não é uma das seis máquinas operacionais; nada foi guardado.",
                validatorToken: BoquilhasValidationErrors.MachineUnknown);
        }

        if (unit.RepairerId is { } repairerId && !_repairers.ContainsKey(repairerId))
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.RepairerNotFound,
                "O reparador indicado não existe no registo de reparadores; nada foi guardado.");
        }

        var movement = new BoquilhaMovement(
            MovementId.From(Guid.NewGuid()),
            unit.BoquilhasId,
            kind,
            unit.Quantity,
            unit.BusinessDate,
            DateTimeOffset.UtcNow,
            unit.RecordedByUserId,
            unit.Machine,
            unit.RepairerId,
            unit.Observations,
            Version: 1,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        if (FailAppend)
        {
            throw new InvalidOperationException("Forced movement-append failure.");
        }

        _registers[unit.BoquilhasId] = register with
        {
            Movements = register.Movements.Concat([movement]).ToList(),
        };

        return movement;
    }

    public async Task<BoqEditResult> EditMovementAsync(
        BoqEditUnit unit,
        CancellationToken cancellationToken)
    {
        if (!_registers.TryGetValue(unit.BoquilhasId, out var register))
        {
            throw new ConcurrencyConflictException($"O registo '{unit.BoquilhasId}' já não existe; nada foi guardado.");
        }

        var current = register.Movements.FirstOrDefault(movement => movement.MovementId.Value == unit.MovementId)
            ?? throw new ConcurrencyConflictException($"O movimento '{unit.MovementId}' já não existe; nada foi guardado.");

        if (current.Version != unit.ExpectedMovementVersion)
        {
            throw new ConcurrencyConflictException(
                $"O movimento '{unit.MovementId}' foi alterado em concorrência " +
                $"(versão esperada {unit.ExpectedMovementVersion}, atual {current.Version}); " +
                "recarregue e tente novamente.");
        }

        if (current.Kind == MovementKind.Saida)
        {
            if (unit.Machine is null)
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.ConstraintViolation,
                    "Uma Saída requer a máquina de contexto; nada foi guardado.",
                    validatorToken: BoquilhasValidationErrors.MachineRequired);
            }

            if (unit.RepairerId is null)
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.ConstraintViolation,
                    "Uma Saída requer o reparador usado no movimento; nada foi guardado.",
                    validatorToken: BoquilhasValidationErrors.RepairerRequired);
            }
        }

        if (unit.Machine is not null && !MachineCode.IsKnown(unit.Machine))
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.ConstraintViolation,
                $"A máquina '{unit.Machine}' não é uma das seis máquinas operacionais; nada foi guardado.",
                validatorToken: BoquilhasValidationErrors.MachineUnknown);
        }

        if (unit.RepairerId is { } repairerId && !_repairers.ContainsKey(repairerId))
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.RepairerNotFound,
                "O reparador indicado não existe no registo de reparadores; nada foi guardado.");
        }

        var edited = current with
        {
            Quantity = unit.Quantity,
            BusinessDate = unit.BusinessDate,
            Machine = unit.Machine,
            RepairerId = unit.RepairerId,
            Observations = unit.Observations,
            Version = current.Version + 1,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var auditEntry = new MovementAuditEntry(
            Guid.NewGuid(),
            unit.MovementId,
            unit.EditedByUserId,
            DateTimeOffset.UtcNow,
            current.Quantity,
            unit.Quantity,
            current.BusinessDate,
            unit.BusinessDate,
            current.Machine,
            unit.Machine,
            current.RepairerId,
            unit.RepairerId,
            current.Observations,
            unit.Observations,
            DateTimeOffset.UtcNow);

        if (FailEdit)
        {
            throw new InvalidOperationException("Forced movement-edit failure.");
        }

        // The SAME movement_id replaces the row (row count unchanged) + the ONE audit row — no
        // second quantity event.
        _registers[unit.BoquilhasId] = register with
        {
            Movements = register.Movements
                .Select(movement => movement.MovementId == edited.MovementId ? edited : movement)
                .ToList(),
        };

        _audits[unit.BoquilhasId] = _audits.TryGetValue(unit.BoquilhasId, out var before)
            ? before.Concat([auditEntry]).ToList()
            : [auditEntry];

        return new BoqEditResult(edited);
    }

    // ------------------------------------------------------------------ helpers

    private RegisterListItem ToListItem(BoquilhaRegister register) =>
        new(
            register.BoquilhasId.Value,
            register.BqId,
            _bqContexts.TryGetValue(register.BqId, out var context) ? context.ToolReference : null,
            _bqContexts.TryGetValue(register.BqId, out var bqContext) ? bqContext.ToolLot : null,
            ProductionNumberOf(register.BqId),
            ProductionMachineOf(register.BqId),
            ProductionDateOf(register.BqId),
            register.Outstanding,
            register.Movements.Count,
            register.Movements.Count == 0 ? null : register.Ledger.Max(movement => movement.RecordedAt));

    private string? ProductionNumberOf(Guid bqId) =>
        _bqContexts.TryGetValue(bqId, out var context)
            && _jobOnFacts.TryGetValue(context.JobOnId, out var facts)
            ? facts.ProductionNumber
            : null;

    private string? ProductionMachineOf(Guid bqId) =>
        _bqContexts.TryGetValue(bqId, out var context)
            && _jobOnFacts.TryGetValue(context.JobOnId, out var facts)
            ? facts.Machine.Value
            : null;

    private DateOnly? ProductionDateOf(Guid bqId) =>
        _bqContexts.TryGetValue(bqId, out var context)
            && _jobOnFacts.TryGetValue(context.JobOnId, out var facts)
            ? facts.ProductionDate
            : null;

    private sealed record JobOnFacts(
        string Reference,
        string ProductionNumber,
        MachineCode Machine,
        DateOnly? ProductionDate);
}