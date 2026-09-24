using DMO.Application.Boquilhas;
using DMO.Application.Persistence;
using DMO.Application.Repositories;
using DMO.Domain.Boquilhas;
using DMO.Domain.Tools;
using DMO.Infrastructure.Persistence.Entities;
using DMO.Infrastructure.Persistence.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace DMO.Infrastructure.Persistence;

/// <summary>
/// <see cref="IBoquilhasRepository"/> implementation over the single application persistence
/// context: the register identity, the movement history, the filtered list/history queries and
/// the guarded movement edit.
/// </summary>
/// <remarks>
/// <para>
/// Authority: P2-T07 OWNER CLARIFICATION. Boquilhas is a historical movement register associated
/// with a REAL production: the register row (<c>boquilhas</c>) merely identifies the production/BQ
/// context (one register per <c>bq_id</c>, plain UNIQUE key) and manufactures NO quantity event;
/// movements are recorded while the production runs AND after it has ended (the production stays
/// the historical context — no end-date rejection exists anywhere). The ledger is a historical
/// fact set: the outstanding repair quantity is derived by replay at read time
/// (Σ Saída − Σ Entrada − Σ Entrada sem reparação) and never stored.
/// </para>
/// <para>
/// <b>Superseded (Owner clarification):</b> NO lifecycle state machine exists — no status
/// active/closed, no close/reopen operations, no close snapshots/reopening rows, no
/// one-active-aggregate-per-anchor invariant, NO ACTIVE partial unique indexes and NO
/// <c>23505 → ActiveAggregateExists</c> mapping (and no replace-with-another-lock): the problem
/// those solved no longer exists. Appends are unversioned inserts (the derived sum cannot be
/// corrupted by races). Edits keep the per-movement optimistic-concurrency token + the
/// before/after audit row, all in one transaction (no second quantity event).
/// </para>
/// <para>
/// Constraint mapping (binding rules): 23514 → the same validator token (never a 500); 23505 on
/// the register's <c>bq_id</c> unique key → <c>Refused(RegisterExists)</c>; 23503 on the
/// anchor/repairer FKs → the typed anchor tokens. The History/list traversal follows the accepted
/// read-only entity-set composition pattern over <c>bq_contexts</c>/<c>job_ons</c>.
/// </para>
/// </remarks>
public sealed class BoquilhasRepository : IBoquilhasRepository
{
    /// <summary>The POSIX clock used for every backend timestamp of every Boquilhas write.</summary>
    private static readonly TimeProvider Clock = TimeProvider.System;

    private readonly DmoDbContext _context;

    /// <summary>Creates the repository over the application persistence context.</summary>
    public BoquilhasRepository(DmoDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    // ================================================================== entity-set access

    private IQueryable<BoquilhaEntity> Registers => _context.Set<BoquilhaEntity>();

    private IQueryable<BoquilhaMovementEntity> Movements => _context.Set<BoquilhaMovementEntity>();

    private IQueryable<BoquilhaMovementAuditEntity> MovementAudits => _context.Set<BoquilhaMovementAuditEntity>();

    private IQueryable<BqContextEntity> BqContexts => _context.Set<BqContextEntity>();

    private IQueryable<JobOnEntity> JobOns => _context.Set<JobOnEntity>();

    private IQueryable<RepairerEntity> Repairers => _context.Set<RepairerEntity>();

    // ================================================================== reads

    /// <inheritdoc />
    public async Task<BoquilhaRegister?> GetByIdAsync(Guid boquilhasId, CancellationToken cancellationToken)
    {
        var entity = await Registers
            .AsNoTracking()
            .FirstOrDefaultAsync(register => register.BoquilhasId == boquilhasId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        return await ProjectAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RegisterListItem>> ListAsync(
        BoquilhasListQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var filtered = ApplyListPredicates(query);

        var page = await filtered
            .OrderBy(register => register.CreatedAt)
            .ThenBy(register => register.BoquilhasId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return await ComposeListRowsAsync(page, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountListAsync(BoquilhasListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await ApplyListPredicates(query).CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HistoryMovementItem>> GetHistoryAsync(
        BoquilhasHistoryQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var filtered = ApplyHistoryPredicates(query);

        var page = await filtered
            .OrderBy(movement => movement.RecordedAt)
            .ThenBy(movement => movement.MovementId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return await ComposeHistoryRowsAsync(page, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountHistoryAsync(
        BoquilhasHistoryQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await ApplyHistoryPredicates(query).CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MovementAuditEntry>> GetMovementAuditAsync(
        Guid movementId,
        CancellationToken cancellationToken)
    {
        return await MovementAudits
            .AsNoTracking()
            .Where(entry => entry.MovementId == movementId)
            .OrderBy(entry => entry.EditedAt)
            .ThenBy(entry => entry.MovementAuditId)
            .Select(entry => new MovementAuditEntry(
                entry.MovementAuditId,
                entry.MovementId,
                entry.EditedByUserId,
                entry.EditedAt,
                entry.BeforeQuantity,
                entry.AfterQuantity,
                entry.BeforeBusinessDate,
                entry.AfterBusinessDate,
                entry.BeforeMachine,
                entry.AfterMachine,
                entry.BeforeRepairerId,
                entry.AfterRepairerId,
                entry.BeforeObservations,
                entry.AfterObservations,
                entry.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    // ================================================================== create — the register identity (route 4)

    /// <inheritdoc />
    public async Task<BoquilhaRegister> CreatedAsync(
        BoqCreateUnit unit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var now = Clock.GetUtcNow();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Production association, authoritatively inside the transaction: the register anchors
            // a REAL bq_contexts row (never a fake production/bq id).
            var contextExists = await BqContexts
                .AsNoTracking()
                .AnyAsync(context => context.BqId == unit.BqId, cancellationToken);

            if (!contextExists)
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.BqContextNotFound,
                    "O contexto BQ indicado não existe; nada foi criado.");
            }

            // The single register identity row — NO quantity movement is manufactured to
            // establish existence (§OWNER: "Do not manufacture stock through register creation").
            var registerId = Guid.NewGuid();

            _context.Set<BoquilhaEntity>().Add(new BoquilhaEntity
            {
                BoquilhasId = registerId,
                BqId = unit.BqId,
                CreatedByUserId = unit.CreatedByUserId,
                CreatedAt = now,
            });

            await SaveAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetByIdAsync(registerId, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"The created register '{registerId}' could not be read back after commit.");
        }
        catch (DbUpdateException exception) when (TryMapWriteFailure(exception, out var failure))
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            _context.ChangeTracker.Clear();
            throw failure;
        }
    }

    // ================================================================== append (route 5)

    /// <inheritdoc />
    public async Task<BoquilhaMovement> AppendMovementAsync(
        BoqAppendUnit unit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var now = Clock.GetUtcNow();

        // The closed-set parse maps an unknown token to the typed persistence refusal (the same
        // token the validator raises first) — the DB CHECK would reject the row anyway (the
        // accepted binding rule: validator input → validator token, never a 500).
        var kind = MovementKindTokens.Parse(unit.MovementType)
            ?? throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.ConstraintViolation,
                $"O tipo de movimento '{unit.MovementType}' não é um dos três tipos operacionais; nada foi guardado.",
                validatorToken: BoquilhasValidationErrors.MovementTypeInvalid);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var register = await Registers
                .FirstOrDefaultAsync(candidate => candidate.BoquilhasId == unit.BoquilhasId, cancellationToken)
                ?? throw new ConcurrencyConflictException(
                    $"O registo '{unit.BoquilhasId}' já não existe; nada foi guardado.");
            _ = register;

            // A Saída records WHO repairs and ON WHICH line: machine + repairer required. Entradas
            // (repaired OR unrepaired returns) are plain factual returns — never validated against
            // a derived quantity and never refused because the production has ended (the
            // production stays the historical context).
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

            if (unit.RepairerId is { } suppliedRepairer
                && !Repairers.AsNoTracking().Any(repairer => repairer.RepairerId == suppliedRepairer))
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.RepairerNotFound,
                    "O reparador indicado não existe no registo de reparadores; nada foi guardado.");
            }

            var movementId = Guid.NewGuid();

            _context.Set<BoquilhaMovementEntity>().Add(new BoquilhaMovementEntity
            {
                MovementId = movementId,
                BoquilhasId = unit.BoquilhasId,
                MovementType = MovementKindTokens.ToToken(kind),
                Quantity = unit.Quantity,
                BusinessDate = unit.BusinessDate,
                RecordedAt = now,
                RecordedByUserId = unit.RecordedByUserId,
                Machine = unit.Machine,
                RepairerId = unit.RepairerId,
                Observations = unit.Observations,
                Version = 1,
                CreatedAt = now,
                UpdatedAt = now,
            });

            await SaveAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new BoquilhaMovement(
                MovementId.From(movementId),
                unit.BoquilhasId,
                kind,
                unit.Quantity,
                unit.BusinessDate,
                now,
                unit.RecordedByUserId,
                unit.Machine,
                unit.RepairerId,
                unit.Observations,
                Version: 1,
                now,
                now);
        }
        catch (DbUpdateException exception) when (TryMapWriteFailure(exception, out var failure))
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            _context.ChangeTracker.Clear();
            throw failure;
        }
    }

    // ================================================================== edit (route 6)

    /// <inheritdoc />
    public async Task<BoqEditResult> EditMovementAsync(
        BoqEditUnit unit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var now = Clock.GetUtcNow();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var register = await Registers
                .FirstOrDefaultAsync(candidate => candidate.BoquilhasId == unit.BoquilhasId, cancellationToken)
                ?? throw new ConcurrencyConflictException(
                    $"O registo '{unit.BoquilhasId}' já não existe; nada foi guardado.");
            _ = register;

            var movementEntity = await Movements
                .FirstOrDefaultAsync(movement => movement.MovementId == unit.MovementId, cancellationToken)
                ?? throw new ConcurrencyConflictException(
                    $"O movimento '{unit.MovementId}' já não existe; nada foi guardado.");

            if (movementEntity.BoquilhasId != unit.BoquilhasId)
            {
                throw new ConcurrencyConflictException(
                    $"O movimento '{unit.MovementId}' não pertence ao registo indicado.");
            }

            if (movementEntity.Version != unit.ExpectedMovementVersion)
            {
                throw new ConcurrencyConflictException(
                    $"O movimento '{unit.MovementId}' foi alterado em concorrência " +
                    $"(versão esperada {unit.ExpectedMovementVersion}, atual {movementEntity.Version}); " +
                    "recarregue e tente novamente.");
            }

            var kind = MovementKindTokens.Parse(movementEntity.MovementType) ?? throw new ArgumentOutOfRangeException(
                nameof(movementEntity.MovementType), movementEntity.MovementType, "Stored movement type is not in the closed set.");

            // The stored type is immutable; a Saída keeps requiring its machine/repairer facts.
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

            if (unit.RepairerId is { } suppliedRepairer
                && !Repairers.AsNoTracking().Any(repairer => repairer.RepairerId == suppliedRepairer))
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.RepairerNotFound,
                    "O reparador indicado não existe no registo de reparadores; nada foi guardado.");
            }

            // The exact before values of every editable field (audited); movement_type and
            // recorded_at are never written (immutable by contract and by the preserved edit rule).
            var beforeQuantity = movementEntity.Quantity;
            var beforeBusinessDate = movementEntity.BusinessDate;
            var beforeMachine = movementEntity.Machine;
            var beforeRepairerId = movementEntity.RepairerId;
            var beforeObservations = movementEntity.Observations;

            movementEntity.Quantity = unit.Quantity;
            movementEntity.BusinessDate = unit.BusinessDate;
            movementEntity.Machine = unit.Machine;
            movementEntity.RepairerId = unit.RepairerId;
            movementEntity.Observations = unit.Observations;
            movementEntity.Version += 1;
            movementEntity.UpdatedAt = now;

            // The one audit row per edit, in the same transaction — audit history, never a second
            // quantity event.
            _context.Set<BoquilhaMovementAuditEntity>().Add(new BoquilhaMovementAuditEntity
            {
                MovementAuditId = Guid.NewGuid(),
                MovementId = unit.MovementId,
                EditedByUserId = unit.EditedByUserId,
                EditedAt = now,
                BeforeQuantity = beforeQuantity,
                AfterQuantity = unit.Quantity,
                BeforeBusinessDate = beforeBusinessDate,
                AfterBusinessDate = unit.BusinessDate,
                BeforeMachine = beforeMachine,
                AfterMachine = unit.Machine,
                BeforeRepairerId = beforeRepairerId,
                AfterRepairerId = unit.RepairerId,
                BeforeObservations = beforeObservations,
                AfterObservations = unit.Observations,
                CreatedAt = now,
            });

            await SaveAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new BoqEditResult(
                new BoquilhaMovement(
                    MovementId.From(unit.MovementId),
                    unit.BoquilhasId,
                    kind,
                    unit.Quantity,
                    unit.BusinessDate,
                    movementEntity.RecordedAt,
                    movementEntity.RecordedByUserId,
                    unit.Machine,
                    unit.RepairerId,
                    unit.Observations,
                    movementEntity.Version,
                    movementEntity.CreatedAt,
                    now));
        }
        catch (DbUpdateException exception) when (TryMapWriteFailure(exception, out var failure))
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            _context.ChangeTracker.Clear();
            throw failure;
        }
    }

    // ================================================================== shared write machinery

    /// <summary>The accepted SaveAsync pattern: EF concurrency-token races surface as the typed
    /// domain conflict (409 stale-version), never a 500 (the P2-T04 §15.1 correction precedent).</summary>
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw ConcurrencyConflictExceptionMapping.ToDomainConflict(exception);
        }
    }

    // ================================================================== query composition

    /// <summary>The register list predicates (route 1): optional reference/lot traversal through
    /// the REAL frozen BQ triple.</summary>
    private IQueryable<BoquilhaEntity> ApplyListPredicates(BoquilhasListQuery query)
    {
        var filtered = Registers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Reference))
        {
            filtered = filtered.Where(register => BqContexts.Any(context =>
                context.BqId == register.BqId && context.ToolReference == query.Reference));
        }

        if (!string.IsNullOrWhiteSpace(query.Lot))
        {
            filtered = filtered.Where(register => BqContexts.Any(context =>
                context.BqId == register.BqId && context.ToolLot == query.Lot));
        }

        return filtered;
    }

    /// <summary>
    /// The Histórico predicates (route 12): movement-level rows — reference/lot traversal through
    /// the register's BQ context, machine/type/repairer/period filters over the movement facts —
    /// every filter a backend SQL predicate.
    /// </summary>
    private IQueryable<BoquilhaMovementEntity> ApplyHistoryPredicates(BoquilhasHistoryQuery query)
    {
        var filtered = Movements.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Reference))
        {
            filtered = filtered.Where(movement => Registers.Any(register =>
                register.BoquilhasId == movement.BoquilhasId
                && BqContexts.Any(context =>
                    context.BqId == register.BqId && context.ToolReference == query.Reference)));
        }

        if (!string.IsNullOrWhiteSpace(query.Lot))
        {
            filtered = filtered.Where(movement => Registers.Any(register =>
                register.BoquilhasId == movement.BoquilhasId
                && BqContexts.Any(context =>
                    context.BqId == register.BqId && context.ToolLot == query.Lot)));
        }

        if (query.Machine is { } machine)
        {
            filtered = filtered.Where(movement => movement.Machine == machine);
        }

        if (query.BusinessDateFrom is { } from)
        {
            filtered = filtered.Where(movement => movement.BusinessDate >= from);
        }

        if (query.BusinessDateTo is { } to)
        {
            filtered = filtered.Where(movement => movement.BusinessDate <= to);
        }

        if (query.MovementType is { } movementType)
        {
            filtered = filtered.Where(movement => movement.MovementType == movementType);
        }

        if (query.RepairerId is { } repairerId)
        {
            filtered = filtered.Where(movement => movement.RepairerId == repairerId);
        }

        return filtered;
    }

    /// <summary>
    /// Composes the register list rows: the production context traversal, the movement count /
    /// last movement and the derived outstanding — batched reads, no N+1 (the accepted
    /// <c>ComposeRowsAsync</c> pattern).
    /// </summary>
    private async Task<IReadOnlyList<RegisterListItem>> ComposeListRowsAsync(
        IReadOnlyList<BoquilhaEntity> page,
        CancellationToken cancellationToken)
    {
        if (page.Count == 0)
        {
            return [];
        }

        var ids = page.Select(register => register.BoquilhasId).ToList();

        var ledgers = await Movements.AsNoTracking()
            .Where(movement => ids.Contains(movement.BoquilhasId))
            .OrderBy(movement => movement.BoquilhasId)
            .ThenBy(movement => movement.RecordedAt)
            .ThenBy(movement => movement.MovementId)
            .ToListAsync(cancellationToken);

        var traversals = await LoadTraversalFactsAsync(page, cancellationToken);

        return page
            .Select(register =>
            {
                var ledger = ledgers
                    .Where(movement => movement.BoquilhasId == register.BoquilhasId)
                    .Select(Project)
                    .ToList();
                var traversal = traversals.TryGetValue(register.BoquilhasId, out var facts) ? facts : null;

                return new RegisterListItem(
                    register.BoquilhasId,
                    register.BqId,
                    traversal?.Reference,
                    traversal?.Lot,
                    traversal?.ProductionNumber,
                    traversal?.ProductionMachine,
                    traversal?.ProductionDate,
                    OutstandingProjection.Replay(ledger),
                    ledger.Count,
                    ledger.Count == 0 ? null : ledger.Max(movement => movement.RecordedAt));
            })
            .ToList();
    }

    /// <summary>
    /// Composes the Histórico rows: each movement with the register's production context.
    /// </summary>
    private async Task<IReadOnlyList<HistoryMovementItem>> ComposeHistoryRowsAsync(
        IReadOnlyList<BoquilhaMovementEntity> page,
        CancellationToken cancellationToken)
    {
        if (page.Count == 0)
        {
            return [];
        }

        var registerIds = page.Select(movement => movement.BoquilhasId).Distinct().ToList();
        var registers = await Registers.AsNoTracking()
            .Where(register => registerIds.Contains(register.BoquilhasId))
            .ToListAsync(cancellationToken);
        var traversals = await LoadTraversalFactsAsync(registers, cancellationToken);

        return page
            .Select(movement =>
            {
                var traversal = traversals.TryGetValue(movement.BoquilhasId, out var facts) ? facts : null;

                return new HistoryMovementItem(
                    movement.MovementId,
                    movement.BoquilhasId,
                    movement.MovementType,
                    movement.Quantity,
                    movement.BusinessDate,
                    movement.RecordedAt,
                    movement.RecordedByUserId,
                    movement.Machine,
                    movement.RepairerId,
                    movement.Observations,
                    traversal?.Reference,
                    traversal?.Lot,
                    traversal?.ProductionNumber,
                    traversal?.ProductionMachine);
            })
            .ToList();
    }

    /// <summary>
    /// The production-context traversal facts of one page: the frozen BQ triple and the REAL Job On
    /// production facts through <c>bq_id → bq_contexts → job_ons</c> — batched reads only.
    /// </summary>
    private async Task<Dictionary<Guid, TraversalFacts>> LoadTraversalFactsAsync(
        IReadOnlyList<BoquilhaEntity> page,
        CancellationToken cancellationToken)
    {
        var facts = new Dictionary<Guid, TraversalFacts>();

        if (page.Count == 0)
        {
            return facts;
        }

        var bqIds = page.Select(register => register.BqId).ToList();

        var contexts = await BqContexts.AsNoTracking()
            .Where(context => bqIds.Contains(context.BqId))
            .Select(context => new { context.BqId, context.JobOnId, context.ToolReference, context.ToolLot })
            .ToListAsync(cancellationToken);

        var jobOnIds = contexts.Select(context => context.JobOnId).Distinct().ToList();

        var productions = await JobOns.AsNoTracking()
            .Where(occurrence => jobOnIds.Contains(occurrence.JobOnId))
            .Select(occurrence => new
            {
                occurrence.JobOnId,
                occurrence.Reference,
                occurrence.ProductionNumber,
                occurrence.Machine,
                occurrence.ProductionDate,
            })
            .ToListAsync(cancellationToken);

        foreach (var register in page)
        {
            var context = contexts.FirstOrDefault(candidate => candidate.BqId == register.BqId);
            if (context is null)
            {
                continue;
            }

            var production = productions.FirstOrDefault(candidate => candidate.JobOnId == context.JobOnId);

            facts[register.BoquilhasId] = new TraversalFacts(
                context.ToolReference,
                context.ToolLot,
                production?.Reference,
                production?.ProductionNumber,
                production?.Machine,
                production?.ProductionDate);
        }

        return facts;
    }

    // ================================================================== projection / mapping

    private async Task<BoquilhaRegister> ProjectAsync(
        BoquilhaEntity entity,
        CancellationToken cancellationToken)
    {
        var movements = await Movements.AsNoTracking()
            .Where(movement => movement.BoquilhasId == entity.BoquilhasId)
            .OrderBy(movement => movement.RecordedAt)
            .ThenBy(movement => movement.MovementId)
            .ToListAsync(cancellationToken);

        // The domain projection builds value objects, which is deliberately not a translatable
        // expression tree (the accepted JobOnRepository LoadContextsAsync precedent).
        var movementRows = movements.Select(Project).ToList();

        return new BoquilhaRegister(
            BoquilhasId.From(entity.BoquilhasId),
            entity.BqId,
            entity.CreatedByUserId,
            entity.CreatedAt,
            movementRows);
    }

    private static BoquilhaMovement Project(BoquilhaMovementEntity movement) => new(
        MovementId.From(movement.MovementId),
        movement.BoquilhasId,
        MovementKindTokens.Parse(movement.MovementType) ?? throw new InvalidOperationException(
            $"Stored movement type '{movement.MovementType}' is not in the closed set."),
        movement.Quantity,
        movement.BusinessDate,
        movement.RecordedAt,
        movement.RecordedByUserId,
        movement.Machine,
        movement.RepairerId,
        movement.Observations,
        movement.Version,
        movement.CreatedAt,
        movement.UpdatedAt);

    /// <summary>
    /// Maps PostgreSQL constraint violations onto the typed Boquilhas failures (binding rules):
    /// <b>23505 on the register's <c>bq_id</c> unique key → <c>RegisterExists</c></b> (one register
    /// per production/BQ context — the ONLY 23505 mapped to a domain refusal; the ACTIVE partial
    /// unique indexes and their ActiveAggregateExists mapping are superseded and removed), 23514 on
    /// a validator-backed CHECK → the same validator token (never a 500), and 23503 on the
    /// anchor/repairer FKs → the typed anchor tokens. Everything else propagates unchanged.
    /// </summary>
    private static bool TryMapWriteFailure(DbUpdateException exception, out BoquilhasPersistenceException failure)
    {
        failure = null!;

        var postgresException = exception.InnerException as PostgresException
            ?? exception.InnerException?.InnerException as PostgresException;

        if (postgresException is null)
        {
            return false;
        }

        switch (postgresException.SqlState)
        {
            case "23505": // unique_violation — the one-register-per-BQ-context key.
                if (string.Equals(
                        postgresException.ConstraintName,
                        BoquilhaEntityConfiguration.BqIdUniqueConstraintName,
                        StringComparison.Ordinal))
                {
                    failure = new BoquilhasPersistenceException(
                        BoquilhasPersistenceFailureReason.RegisterExists,
                        "Já existe um registo de Boquilhas para esta produção; nada foi criado.",
                        exception);
                    return true;
                }

                break;

            case "23514": // check_violation — the DB backstop of the validator rules.
                if (TryMapCheckViolation(postgresException.ConstraintName, out var token))
                {
                    failure = new BoquilhasPersistenceException(
                        BoquilhasPersistenceFailureReason.ConstraintViolation,
                        $"A operação violou uma regra de validação ({token}); nada foi guardado.",
                        exception,
                        validatorToken: token);
                    return true;
                }

                break;

            case "23503": // foreign_key_violation — every P2-T07 FK is RESTRICT.
                if (string.Equals(
                        postgresException.ConstraintName,
                        BoquilhaEntityConfiguration.BqContextForeignKeyConstraintName,
                        StringComparison.Ordinal))
                {
                    failure = new BoquilhasPersistenceException(
                        BoquilhasPersistenceFailureReason.BqContextNotFound,
                        "O contexto BQ referenciado deixou de existir; nada foi guardado.",
                        exception);
                    return true;
                }

                if (string.Equals(
                        postgresException.ConstraintName,
                        BoquilhaMovementEntityConfiguration.RepairerForeignKeyConstraintName,
                        StringComparison.Ordinal))
                {
                    failure = new BoquilhasPersistenceException(
                        BoquilhasPersistenceFailureReason.RepairerNotFound,
                        "O reparador referenciado deixou de existir; nada foi guardado.",
                        exception);
                    return true;
                }

                break;
        }

        return false;
    }

    /// <summary>
    /// The 23514 constraint-name → validator-token table: every validator-backed CHECK maps to the
    /// SAME token the validator raises first. Internal-invariant CHECKs that no application path
    /// can violate (version) are deliberately not mapped — the accepted P2-T06 posture.
    /// </summary>
    private static bool TryMapCheckViolation(string? constraintName, out string token)
    {
        switch (constraintName)
        {
            case BoquilhaMovementEntityConfiguration.MovementTypeCheckConstraintName:
                token = BoquilhasValidationErrors.MovementTypeInvalid;
                return true;

            case BoquilhaMovementEntityConfiguration.QuantityCheckConstraintName:
                token = BoquilhasValidationErrors.QuantityNotPositive;
                return true;

            case BoquilhaMovementEntityConfiguration.MachineCheckConstraintName:
                token = BoquilhasValidationErrors.MachineUnknown;
                return true;

            case BoquilhaMovementEntityConfiguration.SaidaRequiredCheckConstraintName:
                token = BoquilhasValidationErrors.MachineRequired;
                return true;

            case BoquilhaMovementEntityConfiguration.ObservationsCheckConstraintName:
                token = BoquilhasValidationErrors.ObservationsInvalid;
                return true;

            default:
                token = string.Empty;
                return false;
        }
    }

    private static async Task SafeRollbackAsync(
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // Already completed: nothing to undo.
        }
        catch (PostgresException)
        {
            // The transaction is already aborted by the server.
        }
    }

    private sealed record TraversalFacts(
        string Reference,
        string Lot,
        string? ProductionReference,
        string? ProductionNumber,
        string? ProductionMachine,
        DateOnly? ProductionDate);
}