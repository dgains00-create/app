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
/// context: the aggregate/movement ledger persistence, the filtered list queries, the atomic
/// lifecycle writes and the race-safe one-active-per-anchor enforcement.
/// </summary>
/// <remarks>
/// <para>
/// Every multi-row write opens its own transaction and is all-or-nothing (a forced mid-transaction
/// failure — including a 23505 on an active-anchor partial unique index — leaves ZERO rows of the
/// operation: no aggregate without its Início, no status flip without a snapshot, no movement
/// update without its audit row, §10 rule 1).</para>
/// <para>
/// <b>One-active-anchor backstop (§7.2, B1 correction):</b> the application pre-check
/// (<see cref="HasActiveAggregateForAnchorAsync"/> inside the create/reopen transactions) is the
/// normal-path refusal for UX only — NOT the concurrency authority. The database partial unique
/// indexes <c>IX_boquilhas_active_bq_id</c>/<c>IX_boquilhas_active_tool_id</c> are the race-safe
/// authority: a competing create/reopen that commits an ACTIVE row on the same anchor between the
/// pre-check and this transaction's COMMIT raises <c>23505 unique_violation</c> on the relevant
/// index; the <b>entire transaction rolls back</b> and the violation maps to
/// <c>Refused(ActiveAggregateExists)</c> — no partial aggregate, no orphan Início, no partial
/// reopening record, never a 500. <b>No other 23505 source</b> maps to this domain result.</para>
/// <para>
/// Constraint mapping (binding rules, §8.2): 23514 → the same validator token (never a 500);
/// 23503 on the anchor/repairer FKs → the typed anchor tokens. Balance-relative validation is
/// computed by replay over the ledger loaded <b>inside</b> the write transaction (the single pure
/// <see cref="BalanceProjection.Replay"/> helper). The History/ficha traversal follows the accepted
/// read-only entity-set composition pattern (P2-T06 §2 precedent, review observation N1): the
/// reference/lot/machine filters and facts are composed in SQL over <c>bq_contexts</c>/<c>tools</c>/
/// <c>job_ons</c> with no foreign writes.</para>
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

    private IQueryable<BoquilhaEntity> Boquilhas => _context.Set<BoquilhaEntity>();

    private IQueryable<BoquilhaMachineEntity> Machines => _context.Set<BoquilhaMachineEntity>();

    private IQueryable<BoquilhaMovementEntity> Movements => _context.Set<BoquilhaMovementEntity>();

    private IQueryable<BoquilhaMovementAuditEntity> MovementAudits => _context.Set<BoquilhaMovementAuditEntity>();

    private IQueryable<BoquilhaCloseSnapshotEntity> CloseSnapshots => _context.Set<BoquilhaCloseSnapshotEntity>();

    private IQueryable<BoquilhaReopeningEntity> Reopenings => _context.Set<BoquilhaReopeningEntity>();

    private IQueryable<BqContextEntity> BqContexts => _context.Set<BqContextEntity>();

    private IQueryable<ToolEntity> Tools => _context.Set<ToolEntity>();

    private IQueryable<JobOnEntity> JobOns => _context.Set<JobOnEntity>();

    private IQueryable<RepairerEntity> Repairers => _context.Set<RepairerEntity>();

    // ================================================================== reads

    /// <inheritdoc />
    public async Task<BoquilhaAggregate?> GetByIdAsync(Guid boquilhasId, CancellationToken cancellationToken)
    {
        var entity = await Boquilhas
            .AsNoTracking()
            .FirstOrDefaultAsync(aggregate => aggregate.BoquilhasId == boquilhasId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        return await ProjectAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BoquilhaListItem>> ListAsync(
        BoquilhasListQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var filtered = ApplyListPredicates(query);

        var page = await filtered
            .OrderBy(aggregate => aggregate.CreatedAt)
            .ThenBy(aggregate => aggregate.BoquilhasId)
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
    public async Task<IReadOnlyList<HistoryItem>> GetHistoryAsync(
        BoquilhasHistoryQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var filtered = ApplyHistoryPredicates(query);

        var page = await filtered
            .OrderByDescending(aggregate => aggregate.CreatedAt)
            .ThenByDescending(aggregate => aggregate.BoquilhasId)
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
    public async Task<bool> HasActiveAggregateForAnchorAsync(
        Guid? bqId,
        Guid? toolId,
        Guid excludeBoquilhasId,
        CancellationToken cancellationToken)
    {
        // The anchor is exactly one of the two (check the caller's truthfulness): a production-linked
        // scan NEVER fires the tool predicate and vice versa (§7.2 per-mode serialization).
        var query = Boquilhas
            .AsNoTracking()
            .Where(aggregate => aggregate.Status == BoquilhaStatusTokens.ToToken(BoquilhaStatus.Active))
            .Where(aggregate => aggregate.BoquilhasId != excludeBoquilhasId);

        query = bqId is not null
            ? query.Where(aggregate => aggregate.BqId == bqId)
            : query.Where(aggregate => aggregate.ToolId == toolId);

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid?> GetLastCloseSnapshotIdForAnchorAsync(
        Guid? bqId,
        Guid? toolId,
        CancellationToken cancellationToken)
    {
        var query = from aggregate in Boquilhas.AsNoTracking()
                    join snapshot in CloseSnapshots.AsNoTracking()
                        on aggregate.BoquilhasId equals snapshot.BoquilhasId
                    select new { aggregate.BqId, aggregate.ToolId, snapshot.CloseSnapshotId, snapshot.ClosedAt };

        // The anchor is exactly one of the two (exclusive-anchor truthfulness): the production-linked
        // scan never fires the tool predicate and vice versa (§7.2 per-mode serialization).
        query = bqId is not null
            ? query.Where(entry => entry.BqId == bqId)
            : query.Where(entry => entry.ToolId == toolId);

        var last = await query
            .OrderByDescending(entry => entry.ClosedAt)
            .ThenByDescending(entry => entry.CloseSnapshotId)
            .Select(entry => (Guid?)entry.CloseSnapshotId)
            .FirstOrDefaultAsync(cancellationToken);

        return last;
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

    // ================================================================== create (§22.2)

    /// <inheritdoc />
    public async Task<BoquilhaAggregate> CreatedAsync(
        BoqCreateUnit unit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var now = Clock.GetUtcNow();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Application pre-check (normal-path refusal for UX only — NOT the concurrency
            // authority; the partial unique indexes are the race-safe backstop, §7.2).
            if (await HasActiveAggregateForAnchorAsync(unit.BqId, unit.ToolId, Guid.Empty, cancellationToken))
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.ActiveAggregateExists,
                    "Já existe um registo ativo para este contexto BQ; nada foi criado.");
            }

            // Anchor truthfulness, authoritatively inside the transaction (AC-I2/I3).
            if (unit.BqId is { } bqId)
            {
                var contextExists = await BqContexts
                    .AsNoTracking()
                    .AnyAsync(context => context.BqId == bqId, cancellationToken);

                if (!contextExists)
                {
                    throw new BoquilhasPersistenceException(
                        BoquilhasPersistenceFailureReason.BqContextNotFound,
                        "O contexto BQ indicado não existe; nada foi criado.");
                }
            }

            if (unit.ToolId is { } toolId)
            {
                var tool = await Tools
                    .AsNoTracking()
                    .FirstOrDefaultAsync(candidate => candidate.ToolId == toolId, cancellationToken);

                if (tool is null)
                {
                    throw new BoquilhasPersistenceException(
                        BoquilhasPersistenceFailureReason.ToolNotFound,
                        "A ferramenta indicada não existe; nada foi criado.");
                }

                if (!string.Equals(tool.ToolType, "BQ", StringComparison.Ordinal))
                {
                    throw new BoquilhasPersistenceException(
                        BoquilhasPersistenceFailureReason.ToolTypeMismatch,
                        $"Uma ferramenta {tool.ToolType} não é uma ferramenta BQ; nada foi criado.");
                }
            }

            // ONE transaction: the aggregate row + ALL machine rows + the Início movement (§10).
            var aggregateId = Guid.NewGuid();
            var movementId = Guid.NewGuid();

            _context.Set<BoquilhaEntity>().Add(new BoquilhaEntity
            {
                BoquilhasId = aggregateId,
                BqId = unit.BqId,
                ToolId = unit.ToolId,
                Status = BoquilhaStatusTokens.ToToken(BoquilhaStatus.Active),
                OpeningDate = unit.OpeningDate,
                UtilisationPercent = unit.UtilisationPercent,
                Observations = unit.Observations,
                CreatedByUserId = unit.CreatedByUserId,
                Version = 1,
                CreatedAt = now,
                UpdatedAt = now,
            });

            foreach (var machine in unit.Machines)
            {
                _context.Set<BoquilhaMachineEntity>().Add(new BoquilhaMachineEntity
                {
                    BoquilhaMachineId = Guid.NewGuid(),
                    BoquilhasId = aggregateId,
                    Machine = machine,
                });
            }

            _context.Set<BoquilhaMovementEntity>().Add(new BoquilhaMovementEntity
            {
                MovementId = movementId,
                BoquilhasId = aggregateId,
                MovementType = MovementKindTokens.ToToken(MovementKind.Inicio),
                Quantity = unit.InitialQuantity,
                BusinessDate = unit.OpeningDate,
                RecordedAt = now,
                RecordedByUserId = unit.CreatedByUserId,
                Machine = null,
                RepairerId = null,
                ExpectedReturnQuantity = null,
                ExcessReceivedQuantity = null,
                Observations = null,
                Version = 1,
                CreatedAt = now,
                UpdatedAt = now,
            });

            await SaveAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetByIdAsync(aggregateId, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"The created aggregate '{aggregateId}' could not be read back after commit.");
        }
        catch (DbUpdateException exception) when (TryMapWriteFailure(exception, out var failure))
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            _context.ChangeTracker.Clear();
            throw failure;
        }
    }

    // ================================================================== append (§17)

    /// <inheritdoc />
    public async Task<BoquilhaMovement> AppendMovementAsync(
        BoqAppendUnit unit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var now = Clock.GetUtcNow();
        var kind = MovementKindTokens.Parse(unit.MovementType) ?? throw new ArgumentOutOfRangeException(
            nameof(unit.MovementType), unit.MovementType, "The movement type is not in the closed set.");

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var aggregate = await LoadTrackedAggregateAsync(unit.BoquilhasId, cancellationToken);
            AssertAggregateVersion(aggregate, unit.BoquilhasId, unit.ExpectedAggregateVersion, cancellationToken);

            if (aggregate.Status != BoquilhaStatusTokens.ToToken(BoquilhaStatus.Active))
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.AggregateClosed,
                    "O registo está fechado; reabra-o antes de registar movimentos.");
            }

            if (kind == MovementKind.Inicio)
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.OnlyOneInicio,
                    "O Início é criado com o registo; não pode ser registado um segundo Início.");
            }

            var ledgerEntities = await LoadLedgerAsync(unit.BoquilhasId, cancellationToken);
            var ledger = ledgerEntities.Select(Project).ToList();

            ValidateMovementFacts(kind, unit.Quantity, unit.Machine, unit.RepairerId, ledger, unit.BoquilhasId);

            // Entrada facts (S6/Q-EXCESS): computed by replay INSIDE the append transaction.
            int? expectedReturn = null;
            int? excessReceived = null;

            if (kind == MovementKind.Entrada)
            {
                expectedReturn = BalanceProjection.ExpectedReturn(ledger);
                excessReceived = Math.Max(0, unit.Quantity - expectedReturn.Value);
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
                ExpectedReturnQuantity = expectedReturn,
                ExcessReceivedQuantity = excessReceived,
                Observations = unit.Observations,
                Version = 1,
                CreatedAt = now,
                UpdatedAt = now,
            });

            // Every guarded write bumps the aggregate version exactly once (§11.3): a concurrent
            // append/edit/close/reopen always loses with stale-version and no movement can land on
            // a closed trace.
            aggregate.Version += 1;
            aggregate.UpdatedAt = now;

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
                expectedReturn,
                excessReceived,
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

    // ================================================================== edit (§19)

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
            var aggregate = await LoadTrackedAggregateAsync(unit.BoquilhasId, cancellationToken);
            AssertAggregateVersion(aggregate, unit.BoquilhasId, unit.ExpectedAggregateVersion, cancellationToken);

            if (aggregate.Status != BoquilhaStatusTokens.ToToken(BoquilhaStatus.Active))
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.AggregateClosed,
                    "O registo está fechado; reabra-o antes de editar movimentos.");
            }

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

            // §19.2: replay with the edited row's CURRENT values excluded ("before state"), then
            // validate the NEW values exactly like an append — a single net quantity event.
            var ledgerEntities = await LoadLedgerAsync(unit.BoquilhasId, cancellationToken);
            var beforeState = ledgerEntities
                .Where(movement => movement.MovementId != unit.MovementId)
                .Select(Project)
                .ToList();

            ValidateMovementFacts(kind, unit.Quantity, unit.Machine, unit.RepairerId, beforeState, unit.BoquilhasId);

            int? expectedReturn = null;
            int? excessReceived = null;

            if (kind == MovementKind.Entrada)
            {
                expectedReturn = BalanceProjection.ExpectedReturn(beforeState);
                excessReceived = Math.Max(0, unit.Quantity - expectedReturn.Value);
            }

            // The exact before values of every editable field (audited; §19.1).
            var beforeQuantity = movementEntity.Quantity;
            var beforeBusinessDate = movementEntity.BusinessDate;
            var beforeMachine = movementEntity.Machine;
            var beforeRepairerId = movementEntity.RepairerId;
            var beforeObservations = movementEntity.Observations;

            // Guarded UPDATE of the SAME row: only the editable columns; movement_type and
            // recorded_at are never written (AC-E6).
            movementEntity.Quantity = unit.Quantity;
            movementEntity.BusinessDate = unit.BusinessDate;
            movementEntity.Machine = unit.Machine;
            movementEntity.RepairerId = unit.RepairerId;
            movementEntity.ExpectedReturnQuantity = expectedReturn;
            movementEntity.ExcessReceivedQuantity = excessReceived;
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

            // The aggregate version guard (a concurrent close/reopen/opening-fact/edit/append
            // loses with stale-version; §11.4).
            aggregate.Version += 1;
            aggregate.UpdatedAt = now;

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
                    expectedReturn,
                    excessReceived,
                    unit.Observations,
                    movementEntity.Version,
                    movementEntity.CreatedAt,
                    now),
                aggregate.Version);
        }
        catch (DbUpdateException exception) when (TryMapWriteFailure(exception, out var failure))
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            _context.ChangeTracker.Clear();
            throw failure;
        }
    }

    // ================================================================== close (§23.2)

    /// <inheritdoc />
    public async Task<BoqCloseResult> CloseAsync(
        BoqCloseUnit unit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var now = Clock.GetUtcNow();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var aggregate = await LoadTrackedAggregateAsync(unit.BoquilhasId, cancellationToken);
            AssertAggregateVersion(aggregate, unit.BoquilhasId, unit.ExpectedVersion, cancellationToken);

            if (aggregate.Status != BoquilhaStatusTokens.ToToken(BoquilhaStatus.Active))
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.AlreadyClosed,
                    "O registo já está fechado.");
            }

            var ledgerEntities = await LoadLedgerAsync(unit.BoquilhasId, cancellationToken);
            var ledger = ledgerEntities.Select(Project).ToList();
            var balance = BalanceProjection.Replay(ledger);

            var snapshot = new BoquilhaCloseSnapshotEntity
            {
                CloseSnapshotId = Guid.NewGuid(),
                BoquilhasId = unit.BoquilhasId,
                ClosedByUserId = unit.ClosedByUserId,
                ClosedAt = now,
                InitialQuantity = ledger
                    .Where(movement => movement.Kind == MovementKind.Inicio)
                    .Sum(movement => movement.Quantity),
                OpeningDate = aggregate.OpeningDate,
                Disponivel = balance.Disponivel,
                EmReparacao = balance.EmReparacao,
                Irreparavel = balance.Irreparavel,
                EntradaExcecional = balance.EntradaExcecional,
                UtilisationPercent = aggregate.UtilisationPercent,
                CreatedAt = now,
            };

            _context.Set<BoquilhaCloseSnapshotEntity>().Add(snapshot);

            aggregate.Status = BoquilhaStatusTokens.ToToken(BoquilhaStatus.Closed);
            aggregate.Version += 1;
            aggregate.UpdatedAt = now;

            await SaveAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new BoqCloseResult(
                new CloseSnapshot(
                    snapshot.CloseSnapshotId,
                    snapshot.BoquilhasId,
                    snapshot.ClosedByUserId,
                    snapshot.ClosedAt,
                    snapshot.InitialQuantity,
                    snapshot.OpeningDate,
                    snapshot.Disponivel,
                    snapshot.EmReparacao,
                    snapshot.Irreparavel,
                    snapshot.EntradaExcecional,
                    snapshot.UtilisationPercent,
                    snapshot.CreatedAt),
                aggregate.Version);
        }
        catch (DbUpdateException exception) when (TryMapWriteFailure(exception, out var failure))
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            _context.ChangeTracker.Clear();
            throw failure;
        }
    }

    // ================================================================== reopen (§23.3)

    /// <inheritdoc />
    public async Task<BoqReopenResult> ReopenAsync(
        BoqReopenUnit unit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var now = Clock.GetUtcNow();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var aggregate = await LoadTrackedAggregateAsync(unit.BoquilhasId, cancellationToken);
            AssertAggregateVersion(aggregate, unit.BoquilhasId, unit.ExpectedVersion, cancellationToken);

            if (string.IsNullOrWhiteSpace(unit.Reason))
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.ConstraintViolation,
                    "O motivo da reabertura é obrigatório; nada foi reaberto.",
                    validatorToken: BoquilhasValidationErrors.ReopenReasonRequired);
            }

            if (aggregate.Status != BoquilhaStatusTokens.ToToken(BoquilhaStatus.Closed))
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.NotClosed,
                    "O registo não está fechado; apenas registos fechados podem ser reabertos.");
            }

            // §23.3 step 7: THIS aggregate must hold the most recent close among aggregates sharing
            // the anchor (max closed_at, tie-break close_snapshot_id).
            var anchorLastClose = await GetLastCloseSnapshotIdForAnchorAsync(
                aggregate.BqId,
                aggregate.ToolId,
                cancellationToken);

            var lastClose = await CloseSnapshots
                .AsNoTracking()
                .Where(snapshot => snapshot.BoquilhasId == unit.BoquilhasId)
                .OrderByDescending(snapshot => snapshot.ClosedAt)
                .ThenByDescending(snapshot => snapshot.CloseSnapshotId)
                .Select(snapshot => (Guid?)snapshot.CloseSnapshotId)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastClose is null || anchorLastClose != lastClose)
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.NotLastClosed,
                    "Este registo não é o último registo fechado deste contexto BQ; apenas o último fechado pode ser reaberto.");
            }

            // §23.3 step 8: application pre-check (normal-path refusal for UX only — NOT the
            // concurrency authority; the partial unique index of the status UPDATE is the
            // race-safe backstop).
            if (await HasActiveAggregateForAnchorAsync(
                    aggregate.BqId,
                    aggregate.ToolId,
                    unit.BoquilhasId,
                    cancellationToken))
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.ActiveAggregateExists,
                    "Já existe um registo ativo para este contexto BQ; nada foi reaberto.");
            }

            var reopenId = Guid.NewGuid();

            _context.Set<BoquilhaReopeningEntity>().Add(new BoquilhaReopeningEntity
            {
                ReopenId = reopenId,
                BoquilhasId = unit.BoquilhasId,
                CloseSnapshotId = lastClose.Value,
                ReopenedByUserId = unit.ReopenedByUserId,
                ReopenedAt = now,
                Reason = unit.Reason.Trim(),
                CreatedAt = now,
            });

            // The status UPDATE into 'active' is the write that the active-anchor partial unique
            // index protects: if a concurrent create/reopen committed an ACTIVE row on the same
            // anchor first, this UPDATE raises 23505 → entire transaction rolls back → the
            // aggregate remains closed with its snapshot and history intact (§23.3 backstop).
            aggregate.Status = BoquilhaStatusTokens.ToToken(BoquilhaStatus.Active);
            aggregate.Version += 1;
            aggregate.UpdatedAt = now;

            await SaveAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new BoqReopenResult(
                new ReopeningRecord(
                    reopenId,
                    unit.BoquilhasId,
                    lastClose.Value,
                    unit.ReopenedByUserId,
                    now,
                    unit.Reason.Trim(),
                    now),
                aggregate.Version);
        }
        catch (DbUpdateException exception) when (TryMapWriteFailure(exception, out var failure))
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            _context.ChangeTracker.Clear();
            throw failure;
        }
    }

    // ================================================================== opening facts (§23.4)

    /// <inheritdoc />
    public async Task<BoquilhaAggregate> UpdateOpeningFactsAsync(
        BoqOpeningFactsUnit unit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var now = Clock.GetUtcNow();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var aggregate = await LoadTrackedAggregateAsync(unit.BoquilhasId, cancellationToken);
            AssertAggregateVersion(aggregate, unit.BoquilhasId, unit.ExpectedVersion, cancellationToken);

            if (aggregate.Status != BoquilhaStatusTokens.ToToken(BoquilhaStatus.Active))
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.AggregateClosed,
                    "O registo está fechado; reabra-o antes de alterar os dados de abertura.");
            }

            // Aggregate context only: opening_date / utilisation / observations. Never touches
            // movements and never changes balance (§23.4).
            aggregate.OpeningDate = unit.OpeningDate;
            aggregate.UtilisationPercent = unit.UtilisationPercent;
            aggregate.Observations = unit.Observations;
            aggregate.Version += 1;
            aggregate.UpdatedAt = now;

            // Machine-set replace (delete + insert child rows), one unit (§10).
            var existing = await Machines
                .Where(machine => machine.BoquilhasId == unit.BoquilhasId)
                .ToListAsync(cancellationToken);

            _context.Set<BoquilhaMachineEntity>().RemoveRange(existing);

            foreach (var machine in unit.Machines)
            {
                _context.Set<BoquilhaMachineEntity>().Add(new BoquilhaMachineEntity
                {
                    BoquilhaMachineId = Guid.NewGuid(),
                    BoquilhasId = unit.BoquilhasId,
                    Machine = machine,
                });
            }

            await SaveAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetByIdAsync(unit.BoquilhasId, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"The updated aggregate '{unit.BoquilhasId}' could not be read back after commit.");
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
    /// domain conflict (409 stale-version), never a 500 (P2-T04 §15.1 correction precedent).</summary>
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

    /// <summary>Loads the aggregate row TRACKED inside the write transaction.</summary>
    private async Task<BoquilhaEntity> LoadTrackedAggregateAsync(
        Guid boquilhasId,
        CancellationToken cancellationToken)
    {
        return await Boquilhas.FirstOrDefaultAsync(
                aggregate => aggregate.BoquilhasId == boquilhasId,
                cancellationToken)
            ?? throw new ConcurrencyConflictException(
                $"O registo '{boquilhasId}' já não existe; nada foi guardado.");
    }

    private static void AssertAggregateVersion(
        BoquilhaEntity aggregate,
        Guid boquilhasId,
        int expectedVersion,
        CancellationToken cancellationToken)
    {
        if (aggregate.Version != expectedVersion)
        {
            throw new ConcurrencyConflictException(
                $"O registo '{boquilhasId}' foi alterado em concorrência " +
                $"(versão esperada {expectedVersion}, atual {aggregate.Version}); recarregue e tente novamente.");
        }
    }

    /// <summary>
    /// Loads the ledger of one aggregate in the deterministic replay order
    /// (<c>recorded_at ASC, movement_id ASC</c>, §18.2).
    /// </summary>
    private Task<List<BoquilhaMovementEntity>> LoadLedgerAsync(
        Guid boquilhasId,
        CancellationToken cancellationToken) =>
        Movements
            .Where(movement => movement.BoquilhasId == boquilhasId)
            .OrderBy(movement => movement.RecordedAt)
            .ThenBy(movement => movement.MovementId)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// The §17.2 rules re-asserted authoritatively inside the append/edit transaction: machine
    /// membership, repairer existence, the external-Saída required facts and the balance-relative
    /// refusals computed by replay over the supplied before-state ledger. Negative saldo is never a
    /// refusal (AC-B7).
    /// </summary>
    private void ValidateMovementFacts(
        MovementKind kind,
        int quantity,
        string? machine,
        Guid? repairerId,
        IReadOnlyList<BoquilhaMovement> beforeState,
        Guid boquilhasId)
    {
        if (kind == MovementKind.Saida)
        {
            if (machine is null)
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.ConstraintViolation,
                    "Uma Saída externa requer a máquina de contexto; nada foi guardado.",
                    validatorToken: BoquilhasValidationErrors.MachineRequired);
            }

            if (repairerId is null)
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.ConstraintViolation,
                    "Uma Saída externa requer o reparador final selecionado; nada foi guardado.",
                    validatorToken: BoquilhasValidationErrors.RepairerRequired);
            }
        }

        if (machine is not null)
        {
            var registered = Machines
                .AsNoTracking()
                .Any(candidate =>
                    candidate.BoquilhasId == boquilhasId
                    && candidate.Machine == machine);

            if (!registered)
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.ConstraintViolation,
                    $"A máquina '{machine}' não pertence ao conjunto de máquinas do registo; nada foi guardado.",
                    validatorToken: BoquilhasValidationErrors.MachineNotInAggregate);
            }
        }

        if (repairerId is { } suppliedRepairer
            && !Repairers.AsNoTracking().Any(repairer => repairer.RepairerId == suppliedRepairer))
        {
            throw new BoquilhasPersistenceException(
                BoquilhasPersistenceFailureReason.RepairerNotFound,
                "O reparador indicado não existe no registo de reparadores; nada foi guardado.");
        }

        if (kind == MovementKind.Saida || kind == MovementKind.Irreparavel)
        {
            var before = BalanceProjection.Replay(beforeState);

            if (kind == MovementKind.Saida && quantity > before.Disponivel)
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.SaidaExceedsAvailable,
                    $"A quantidade de Saída ({quantity}) excede o Disponível ({before.Disponivel}); nada foi guardado.");
            }

            if (kind == MovementKind.Irreparavel && quantity > before.EmReparacao)
            {
                throw new BoquilhasPersistenceException(
                    BoquilhasPersistenceFailureReason.IrreparavelExceedsInRepair,
                    $"A quantidade de Irreparável ({quantity}) excede o Em reparação ({before.EmReparacao}); nada foi guardado.");
            }
        }
    }

    // ================================================================== query composition

    /// <summary>
    /// The Registo predicates (route 4): state (ACTIVE by default), reference/lot traversal
    /// (frozen triple for linked / live Tool for standalone), machine-set membership — all backend
    /// SQL predicates.
    /// </summary>
    private IQueryable<BoquilhaEntity> ApplyListPredicates(BoquilhasListQuery query)
    {
        var state = string.IsNullOrWhiteSpace(query.State)
            ? BoquilhaStatusTokens.ToToken(BoquilhaStatus.Active)
            : query.State;

        var filtered = Boquilhas.AsNoTracking().Where(aggregate => aggregate.Status == state);

        return ApplyTraversalPredicates(filtered, query.Reference, query.Lot, query.Machine);
    }

    /// <summary>
    /// The Histórico predicates (route 18, §24.2): state (both when null), reference/lot
    /// traversal, machine-set membership, movement business-date period (EXISTS), movement type
    /// (EXISTS) and repairer (EXISTS) — every filter a backend SQL predicate.
    /// </summary>
    private IQueryable<BoquilhaEntity> ApplyHistoryPredicates(BoquilhasHistoryQuery query)
    {
        var filtered = Boquilhas.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.State))
        {
            filtered = filtered.Where(aggregate => aggregate.Status == query.State);
        }

        filtered = ApplyTraversalPredicates(filtered, query.Reference, query.Lot, query.Machine);

        if (query.BusinessDateFrom is { } from)
        {
            filtered = filtered.Where(aggregate => Movements.Any(movement =>
                movement.BoquilhasId == aggregate.BoquilhasId && movement.BusinessDate >= from));
        }

        if (query.BusinessDateTo is { } to)
        {
            filtered = filtered.Where(aggregate => Movements.Any(movement =>
                movement.BoquilhasId == aggregate.BoquilhasId && movement.BusinessDate <= to));
        }

        if (query.MovementType is { } movementType)
        {
            filtered = filtered.Where(aggregate => Movements.Any(movement =>
                movement.BoquilhasId == aggregate.BoquilhasId && movement.MovementType == movementType));
        }

        if (query.RepairerId is { } repairerId)
        {
            filtered = filtered.Where(aggregate => Movements.Any(movement =>
                movement.BoquilhasId == aggregate.BoquilhasId && movement.RepairerId == repairerId));
        }

        return filtered;
    }

    /// <summary>
    /// The shared reference/lot/machine traversal predicates: reference/lot through the frozen
    /// <c>bq_contexts</c> triple (production-linked) OR the live <c>tools</c> row (standalone);
    /// machine by membership of the aggregate's registered machine set.
    /// </summary>
    private IQueryable<BoquilhaEntity> ApplyTraversalPredicates(
        IQueryable<BoquilhaEntity> source,
        string? reference,
        string? lot,
        string? machine)
    {
        if (!string.IsNullOrWhiteSpace(reference))
        {
            source = source.Where(aggregate =>
                (aggregate.BqId != null
                    && BqContexts.Any(context =>
                        context.BqId == aggregate.BqId && context.ToolReference == reference))
                || (aggregate.ToolId != null
                    && Tools.Any(tool => tool.ToolId == aggregate.ToolId && tool.Reference == reference)));
        }

        if (!string.IsNullOrWhiteSpace(lot))
        {
            source = source.Where(aggregate =>
                (aggregate.BqId != null
                    && BqContexts.Any(context =>
                        context.BqId == aggregate.BqId && context.ToolLot == lot))
                || (aggregate.ToolId != null
                    && Tools.Any(tool => tool.ToolId == aggregate.ToolId && tool.Lot == lot)));
        }

        if (!string.IsNullOrWhiteSpace(machine))
        {
            source = source.Where(aggregate => Machines.Any(candidate =>
                candidate.BoquilhasId == aggregate.BoquilhasId && candidate.Machine == machine));
        }

        return source;
    }

    /// <summary>
    /// Composes the Registo rows of one page: the traversal facts, the machine sets, the
    /// replay-derived balance buckets and the Início quantity — batched reads, no N+1 (the accepted
    /// P2-T06 <c>ComposeRowsAsync</c> pattern).
    /// </summary>
    private async Task<IReadOnlyList<BoquilhaListItem>> ComposeListRowsAsync(
        IReadOnlyList<BoquilhaEntity> page,
        CancellationToken cancellationToken)
    {
        if (page.Count == 0)
        {
            return [];
        }

        var ids = page.Select(aggregate => aggregate.BoquilhasId).ToList();

        var machineSets = await Machines.AsNoTracking()
            .Where(machine => ids.Contains(machine.BoquilhasId))
            .OrderBy(machine => machine.BoquilhasId)
            .GroupBy(machine => machine.BoquilhasId)
            .Select(group => new
            {
                BoquilhasId = group.Key,
                Machines = group.Select(machine => machine.Machine).ToList(),
            })
            .ToDictionaryAsync(entry => entry.BoquilhasId, entry => entry.Machines, cancellationToken);

        var ledgers = await Movements.AsNoTracking()
            .Where(movement => ids.Contains(movement.BoquilhasId))
            .OrderBy(movement => movement.BoquilhasId)
            .ThenBy(movement => movement.RecordedAt)
            .ThenBy(movement => movement.MovementId)
            .ToListAsync(cancellationToken);

        var traversals = await LoadTraversalFactsAsync(page, cancellationToken);

        return page
            .Select(aggregate =>
            {
                var ledger = ledgers
                    .Where(movement => movement.BoquilhasId == aggregate.BoquilhasId)
                    .Select(Project)
                    .ToList();
                var balance = BalanceProjection.Replay(ledger);
                var traversal = traversals.TryGetValue(aggregate.BoquilhasId, out var facts) ? facts : null;

                return new BoquilhaListItem(
                    aggregate.BoquilhasId,
                    aggregate.Version,
                    aggregate.Status,
                    aggregate.BqId,
                    aggregate.ToolId,
                    traversal?.Reference,
                    traversal?.Lot,
                    machineSets.TryGetValue(aggregate.BoquilhasId, out var machines) ? machines : [],
                    aggregate.OpeningDate,
                    ledger
                        .Where(movement => movement.Kind == MovementKind.Inicio)
                        .Sum(movement => movement.Quantity),
                    balance.Disponivel,
                    balance.EmReparacao,
                    balance.Irreparavel,
                    balance.EntradaExcecional);
            })
            .ToList();
    }

    /// <summary>
    /// Composes the Histórico rows of one page: the traversal facts, the machine sets, the
    /// replay-derived balance buckets, the movement count / last movement and the last close
    /// (the exact §24.2 row shape).
    /// </summary>
    private async Task<IReadOnlyList<HistoryItem>> ComposeHistoryRowsAsync(
        IReadOnlyList<BoquilhaEntity> page,
        CancellationToken cancellationToken)
    {
        if (page.Count == 0)
        {
            return [];
        }

        var ids = page.Select(aggregate => aggregate.BoquilhasId).ToList();

        var machineSets = await Machines.AsNoTracking()
            .Where(machine => ids.Contains(machine.BoquilhasId))
            .GroupBy(machine => machine.BoquilhasId)
            .Select(group => new
            {
                BoquilhasId = group.Key,
                Machines = group.Select(machine => machine.Machine).ToList(),
            })
            .ToDictionaryAsync(entry => entry.BoquilhasId, entry => entry.Machines, cancellationToken);

        var ledgers = await Movements.AsNoTracking()
            .Where(movement => ids.Contains(movement.BoquilhasId))
            .OrderBy(movement => movement.BoquilhasId)
            .ThenBy(movement => movement.RecordedAt)
            .ThenBy(movement => movement.MovementId)
            .ToListAsync(cancellationToken);

        var lastCloses = await CloseSnapshots.AsNoTracking()
            .Where(snapshot => ids.Contains(snapshot.BoquilhasId))
            .OrderBy(snapshot => snapshot.BoquilhasId)
            .ThenByDescending(snapshot => snapshot.ClosedAt)
            .ThenByDescending(snapshot => snapshot.CloseSnapshotId)
            .ToListAsync(cancellationToken);

        var traversals = await LoadTraversalFactsAsync(page, cancellationToken);

        return page
            .Select(aggregate =>
            {
                var ledger = ledgers
                    .Where(movement => movement.BoquilhasId == aggregate.BoquilhasId)
                    .Select(Project)
                    .ToList();
                var balance = BalanceProjection.Replay(ledger);
                var lastClose = lastCloses.FirstOrDefault(snapshot =>
                    snapshot.BoquilhasId == aggregate.BoquilhasId);
                var traversal = traversals.TryGetValue(aggregate.BoquilhasId, out var facts) ? facts : null;

                return new HistoryItem(
                    aggregate.BoquilhasId,
                    aggregate.Version,
                    aggregate.Status,
                    aggregate.BqId,
                    aggregate.ToolId,
                    traversal?.Reference,
                    traversal?.Lot,
                    machineSets.TryGetValue(aggregate.BoquilhasId, out var machines) ? machines : [],
                    aggregate.OpeningDate,
                    ledger
                        .Where(movement => movement.Kind == MovementKind.Inicio)
                        .Sum(movement => movement.Quantity),
                    balance.Disponivel,
                    balance.EmReparacao,
                    balance.Irreparavel,
                    balance.EntradaExcecional,
                    ledger.Count,
                    lastClose?.ClosedAt,
                    lastClose?.ClosedByUserId,
                    ledger.Count == 0 ? null : ledger.Max(movement => movement.RecordedAt));
            })
            .ToList();
    }

    /// <summary>
    /// The traversal facts of one page: reference/lot through the frozen <c>bq_contexts</c> triple
    /// (production-linked) or the live <c>tools</c> row (standalone) — batched reads only.
    /// </summary>
    private async Task<Dictionary<Guid, TraversalFacts>> LoadTraversalFactsAsync(
        IReadOnlyList<BoquilhaEntity> page,
        CancellationToken cancellationToken)
    {
        var facts = new Dictionary<Guid, TraversalFacts>();

        var linked = page.Where(aggregate => aggregate.BqId is not null).ToList();
        if (linked.Count > 0)
        {
            var linkedIds = linked.Select(aggregate => aggregate.BqId!.Value).ToList();

            var contexts = await BqContexts.AsNoTracking()
                .Where(context => linkedIds.Contains(context.BqId))
                .Select(context => new { context.BqId, context.ToolReference, context.ToolLot })
                .ToListAsync(cancellationToken);

            foreach (var aggregate in linked)
            {
                var context = contexts.FirstOrDefault(candidate => candidate.BqId == aggregate.BqId);
                if (context is not null)
                {
                    facts[aggregate.BoquilhasId] = new TraversalFacts(context.ToolReference, context.ToolLot);
                }
            }
        }

        var standalone = page.Where(aggregate => aggregate.ToolId is not null).ToList();
        if (standalone.Count > 0)
        {
            var standaloneIds = standalone.Select(aggregate => aggregate.ToolId!.Value).ToList();

            var tools = await Tools.AsNoTracking()
                .Where(tool => standaloneIds.Contains(tool.ToolId))
                .Select(tool => new { tool.ToolId, tool.Reference, tool.Lot })
                .ToListAsync(cancellationToken);

            foreach (var aggregate in standalone)
            {
                var tool = tools.FirstOrDefault(candidate => candidate.ToolId == aggregate.ToolId);
                if (tool is not null)
                {
                    facts[aggregate.BoquilhasId] = new TraversalFacts(tool.Reference, tool.Lot);
                }
            }
        }

        return facts;
    }

    // ================================================================== projection / mapping

    private async Task<BoquilhaAggregate> ProjectAsync(
        BoquilhaEntity entity,
        CancellationToken cancellationToken)
    {
        var machines = await Machines.AsNoTracking()
            .Where(machine => machine.BoquilhasId == entity.BoquilhasId)
            .OrderBy(machine => machine.Machine)
            .Select(machine => machine.Machine)
            .ToListAsync(cancellationToken);

        var movements = await Movements.AsNoTracking()
            .Where(movement => movement.BoquilhasId == entity.BoquilhasId)
            .OrderBy(movement => movement.RecordedAt)
            .ThenBy(movement => movement.MovementId)
            .ToListAsync(cancellationToken);

        // The domain projection builds value objects, which is deliberately not a translatable
        // expression tree (the accepted JobOnRepository LoadContextsAsync precedent): entities are
        // materialized first and mapped in memory.
        var movementRows = movements.Select(Project).ToList();

        var snapshots = await CloseSnapshots.AsNoTracking()
            .Where(snapshot => snapshot.BoquilhasId == entity.BoquilhasId)
            .OrderBy(snapshot => snapshot.ClosedAt)
            .ThenBy(snapshot => snapshot.CloseSnapshotId)
            .Select(snapshot => new CloseSnapshot(
                snapshot.CloseSnapshotId,
                snapshot.BoquilhasId,
                snapshot.ClosedByUserId,
                snapshot.ClosedAt,
                snapshot.InitialQuantity,
                snapshot.OpeningDate,
                snapshot.Disponivel,
                snapshot.EmReparacao,
                snapshot.Irreparavel,
                snapshot.EntradaExcecional,
                snapshot.UtilisationPercent,
                snapshot.CreatedAt))
            .ToListAsync(cancellationToken);

        var reopenings = await Reopenings.AsNoTracking()
            .Where(reopen => reopen.BoquilhasId == entity.BoquilhasId)
            .OrderBy(reopen => reopen.ReopenedAt)
            .ThenBy(reopen => reopen.ReopenId)
            .Select(reopen => new ReopeningRecord(
                reopen.ReopenId,
                reopen.BoquilhasId,
                reopen.CloseSnapshotId,
                reopen.ReopenedByUserId,
                reopen.ReopenedAt,
                reopen.Reason,
                reopen.CreatedAt))
            .ToListAsync(cancellationToken);

        return new BoquilhaAggregate(
            BoquilhasId.From(entity.BoquilhasId),
            entity.BqId,
            entity.ToolId,
            BoquilhaStatusTokens.Parse(entity.Status) ?? throw new InvalidOperationException(
                $"Stored status '{entity.Status}' is not in the closed set."),
            entity.OpeningDate,
            entity.UtilisationPercent,
            entity.Observations,
            entity.CreatedByUserId,
            entity.Version,
            entity.CreatedAt,
            entity.UpdatedAt,
            machines.Select(MachineCode.From).ToList(),
            movementRows,
            snapshots,
            reopenings);
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
        movement.ExpectedReturnQuantity,
        movement.ExcessReceivedQuantity,
        movement.Observations,
        movement.Version,
        movement.CreatedAt,
        movement.UpdatedAt);

    /// <summary>
    /// Maps PostgreSQL constraint violations onto the typed Boquilhas failures (binding rules,
    /// §8.2): <b>23505 on <c>IX_boquilhas_active_bq_id</c>/<c>IX_boquilhas_active_tool_id</c> →
    /// <c>ActiveAggregateExists</c></b> (the exact scoped mapping of §7.2 — no other 23505 source
    /// maps to this domain result; the <c>boquilha_machines</c> unique key maps to the same
    /// validator token the validator raises first), 23514 on a validator-backed CHECK → the same
    /// validator token (never a 500), and 23503 on the anchor/repairer FKs → the typed anchor
    /// tokens. Everything else propagates unchanged (the accepted P2-T06 posture).
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
            case "23505": // unique_violation
                if (string.Equals(
                        postgresException.ConstraintName,
                        BoquilhaEntityConfiguration.ActiveBqIdUniqueIndexName,
                        StringComparison.Ordinal)
                    || string.Equals(
                        postgresException.ConstraintName,
                        BoquilhaEntityConfiguration.ActiveToolIdUniqueIndexName,
                        StringComparison.Ordinal))
                {
                    failure = new BoquilhasPersistenceException(
                        BoquilhasPersistenceFailureReason.ActiveAggregateExists,
                        "Já existe um registo ativo para este contexto BQ; nada foi guardado.",
                        exception);
                    return true;
                }

                if (string.Equals(
                        postgresException.ConstraintName,
                        BoquilhaMachineEntityConfiguration.BoquilhasMachineUniqueConstraintName,
                        StringComparison.Ordinal))
                {
                    failure = new BoquilhasPersistenceException(
                        BoquilhasPersistenceFailureReason.ConstraintViolation,
                        "A lista de máquinas contém valores duplicados; nada foi guardado.",
                        exception,
                        validatorToken: BoquilhasValidationErrors.MachineUnknown);
                    return true;
                }

                break;

            case "23514": // check_violation — the DB backstop of the validator rules (§7.4).
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
                        BoquilhaEntityConfiguration.ToolForeignKeyConstraintName,
                        StringComparison.Ordinal))
                {
                    failure = new BoquilhasPersistenceException(
                        BoquilhasPersistenceFailureReason.ToolNotFound,
                        "A ferramenta referenciada deixou de existir; nada foi guardado.",
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
    /// The 23514 constraint-name → validator-token table (§7.4): every validator-backed CHECK maps
    /// to the SAME token the validator raises first. Internal-invariant CHECKs that no application
    /// path can violate (status/version) are deliberately not mapped — the accepted P2-T06 posture
    /// (unreachable violations propagate; the shrink-set is exactly the validator-backed surface the
    /// tests exercise).
    /// </summary>
    private static bool TryMapCheckViolation(string? constraintName, out string token)
    {
        switch (constraintName)
        {
            case BoquilhaEntityConfiguration.AnchorExclusiveCheckConstraintName:
                token = BoquilhasValidationErrors.AnchorConflict;
                return true;

            case BoquilhaEntityConfiguration.UtilisationRangeCheckConstraintName:
                token = BoquilhasValidationErrors.UtilisationInvalid;
                return true;

            case BoquilhaEntityConfiguration.ObservationsCheckConstraintName:
                token = BoquilhasValidationErrors.ObservationsInvalid;
                return true;

            case BoquilhaMachineEntityConfiguration.MachineCheckConstraintName:
                token = BoquilhasValidationErrors.MachineUnknown;
                return true;

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

            case BoquilhaMovementEntityConfiguration.EntradaFactsCheckConstraintName:
                token = BoquilhasValidationErrors.QuantityNotPositive;
                return true;

            case BoquilhaMovementEntityConfiguration.ObservationsCheckConstraintName:
                token = BoquilhasValidationErrors.ObservationsInvalid;
                return true;

            case BoquilhaMovementAuditEntityConfiguration.QuantitiesCheckConstraintName:
                token = BoquilhasValidationErrors.QuantityNotPositive;
                return true;

            case BoquilhaCloseSnapshotEntityConfiguration.EntradaExcecionalCheckConstraintName:
                token = BoquilhasValidationErrors.QuantityNotPositive;
                return true;

            case BoquilhaCloseSnapshotEntityConfiguration.UtilisationCheckConstraintName:
                token = BoquilhasValidationErrors.UtilisationInvalid;
                return true;

            case BoquilhaReopeningEntityConfiguration.ReasonRequiredCheckConstraintName:
                token = BoquilhasValidationErrors.ReopenReasonRequired;
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

    private sealed record TraversalFacts(string Reference, string Lot);
}