using DMO.Application.JobOn;
using DMO.Application.Persistence;
using DMO.Application.Repositories;
using DMO.Application.Session;
using DMO.Application.Tools;
using DMO.Domain.Boquilhas;
using DMO.Domain.Controlo;
using DMO.Domain.Tools;
using JobOnFindProductionsQuery = DMO.Application.JobOn.FindProductionsQuery;

namespace DMO.Application.Boquilhas;

/// <summary>
/// The Boquilhas application service: aggregate opening (production-linked | standalone), movement
/// ledger append/edit, derived balance reads, close/reopen on the SAME <c>boquilhas_id</c>,
/// opening-facts updates and the local Histórico — composing the closed P2-T04/P2-T05 application
/// contracts.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §9 (service composition + decision rules), §17–§19 (movements/
/// balance/edit), §21 (repairer resolution reads), §22–§24 (creation, close/reopen, Histórico).
/// <para>
/// Decision rules of the service (exact, §9.1): (1) balance-relative validation is computed by
/// replay over the ledger (the single pure <see cref="BalanceProjection.Replay"/> helper, §18) as a
/// UX pre-check AND again authoritatively by the repository inside the same write transaction —
/// never from a stored total; (2) edit validation replays with the edited movement's current values
/// excluded, then validates the new values exactly like an append (§19.2) — guaranteeing a single
/// net event; (3) the Web layer never queries the database directly; the service never calls a
/// P2-T05/P2-T04/P2-T06-gated HTTP route (no cross-module HTTP calls). Every persisted-state refusal
/// is a typed <see cref="BoquilhasResult"/>; actor/time facts are backend-authored
/// (<c>ICurrentAccountContext</c>/backend clock) and never client-supplied.</para>
/// </remarks>
public sealed class BoquilhasService : IBoquilhasService
{
    private readonly IBoquilhasRepository _boquilhas;
    private readonly IJobOnService _jobOns;
    private readonly IToolService _tools;
    private readonly IRepairerRepository _repairers;
    private readonly IMachineRepairerAssignmentRepository _assignments;
    private readonly IBoquilhasContextRead _bqContexts;
    private readonly ICurrentAccountContext _currentAccount;

    /// <summary>Creates the service over the Boquilhas repository and the consumed application contracts.</summary>
    public BoquilhasService(
        IBoquilhasRepository boquilhas,
        IJobOnService jobOns,
        IToolService tools,
        IRepairerRepository repairers,
        IMachineRepairerAssignmentRepository assignments,
        IBoquilhasContextRead bqContexts,
        ICurrentAccountContext currentAccount)
    {
        ArgumentNullException.ThrowIfNull(boquilhas);
        ArgumentNullException.ThrowIfNull(jobOns);
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentNullException.ThrowIfNull(repairers);
        ArgumentNullException.ThrowIfNull(assignments);
        ArgumentNullException.ThrowIfNull(bqContexts);
        ArgumentNullException.ThrowIfNull(currentAccount);
        _boquilhas = boquilhas;
        _jobOns = jobOns;
        _tools = tools;
        _repairers = repairers;
        _assignments = assignments;
        _bqContexts = bqContexts;
        _currentAccount = currentAccount;
    }

    // ------------------------------------------------------------------ reads (§9.1)

    /// <inheritdoc />
    public async Task<BoquilhasResult> GetListAsync(
        BoquilhasListQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var errors = BoquilhasValidator.Validate(query);
        if (errors.Count > 0)
        {
            return new BoquilhasResult.ValidationFailed(errors);
        }

        var rows = await _boquilhas.ListAsync(query, cancellationToken);

        // Total = the backend count for the SAME predicate + filters (§24.2 discipline, applied to
        // the Registo grid too); never a fabricated grand total.
        var total = await _boquilhas.CountListAsync(query, cancellationToken);

        return new BoquilhasResult.ListFound(
            rows.Select(row => new BoquilhaListItemReadModel(
                row.BoquilhasId,
                row.Version,
                row.State,
                row.BqId,
                row.ToolId,
                row.Reference,
                row.Lot,
                row.Machines,
                row.OpeningDate,
                row.InitialQuantity,
                row.Disponivel,
                row.EmReparacao,
                row.Irreparavel,
                row.EntradaExcecional))
                .ToList(),
            total);
    }

    /// <inheritdoc />
    public async Task<BoquilhasResult> GetHistoryAsync(
        BoquilhasHistoryQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var errors = BoquilhasValidator.Validate(query);
        if (errors.Count > 0)
        {
            return new BoquilhasResult.ValidationFailed(errors);
        }

        var rows = await _boquilhas.GetHistoryAsync(query, cancellationToken);

        // Total = backend-counted rows for the same predicate + filters (§24.2).
        var total = await _boquilhas.CountHistoryAsync(query, cancellationToken);

        return new BoquilhasResult.HistoryFound(
            rows.Select(row => new HistoryItemReadModel(
                row.BoquilhasId,
                row.Version,
                row.State,
                row.BqId,
                row.ToolId,
                row.Reference,
                row.Lot,
                row.Machines,
                row.OpeningDate,
                row.InitialQuantity,
                row.Disponivel,
                row.EmReparacao,
                row.Irreparavel,
                row.EntradaExcecional,
                row.MovementCount,
                row.ClosedAt,
                row.ClosedByUserId,
                row.LastMovementAt))
                .ToList(),
            total);
    }

    /// <inheritdoc />
    public async Task<BoquilhasResult> GetAsync(Guid boquilhasId, CancellationToken cancellationToken)
    {
        var aggregate = await _boquilhas.GetByIdAsync(boquilhasId, cancellationToken);
        if (aggregate is null)
        {
            return new BoquilhasResult.NotFound(boquilhasId);
        }

        return new BoquilhasResult.Ficha(await BuildFichaAsync(aggregate, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<BoquilhasResult> GetMovementAuditAsync(
        Guid boquilhasId,
        Guid movementId,
        CancellationToken cancellationToken)
    {
        var aggregate = await _boquilhas.GetByIdAsync(boquilhasId, cancellationToken);
        if (aggregate is null || !aggregate.Movements.Any(movement => movement.MovementId.Value == movementId))
        {
            return new BoquilhasResult.NotFound(movementId);
        }

        var entries = await _boquilhas.GetMovementAuditAsync(movementId, cancellationToken);

        return new BoquilhasResult.MovementAuditFound(
            movementId,
            entries.Select(entry => new MovementAuditItemReadModel(
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
                entry.AfterObservations))
                .ToList());
    }

    // ------------------------------------------------------------------ create (§22)

    /// <inheritdoc />
    public async Task<BoquilhasResult> CreateAsync(
        CreateBoquilhasCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = BoquilhasValidator.Validate(command);
        if (errors.Count > 0)
        {
            return new BoquilhasResult.ValidationFailed(errors);
        }

        // Anchor truthfulness: a production-linked aggregate anchors a REAL bq_contexts row (AC-I3);
        // a standalone aggregate anchors a real canonical BQ Tool (AC-I2). The repository repeats
        // both resolutions authoritatively inside the create transaction.
        if (command.BqId is { } bqId)
        {
            if (await _bqContexts.GetBqContextAsync(bqId, cancellationToken) is null)
            {
                return new BoquilhasResult.ValidationFailed(
                    [BoquilhasValidationErrors.BqContextNotFound]);
            }
        }

        if (command.ToolId is { } toolId)
        {
            var toolResult = await _tools.GetAsync(toolId, cancellationToken);
            if (toolResult is not ToolResult.Found(var ficha))
            {
                return new BoquilhasResult.ValidationFailed([BoquilhasValidationErrors.ToolNotFound]);
            }

            if (ficha.Type != ToolType.Bq)
            {
                return new BoquilhasResult.ValidationFailed(
                    [BoquilhasValidationErrors.ToolTypeMismatch]);
            }
        }

        // Application pre-check (normal-path refusal for UX only — NOT the concurrency authority;
        // the partial unique indexes of §7.2 are the race-safe backstop, §22.2).
        if (await HasActiveAggregateForAnchorAsync(command.BqId, command.ToolId, Guid.Empty, cancellationToken))
        {
            return Refuse(
                BoquilhasRefusalReason.ActiveAggregateExists,
                "Já existe um registo ativo para este contexto BQ; nada foi criado.");
        }

        var unit = new BoqCreateUnit(
            command.BqId,
            command.ToolId,
            command.Machines,
            command.InitialQuantity,
            command.OpeningDate,
            command.UtilisationPercent,
            Trimmed(command.Observations),
            command.CreatedByUserId);

        try
        {
            var created = await _boquilhas.CreatedAsync(unit, cancellationToken);

            return new BoquilhasResult.Created(created.BoquilhasId.Value, created.Version);
        }
        catch (ConcurrencyConflictException exception)
        {
            return Refuse(BoquilhasRefusalReason.StaleVersion, exception.Message);
        }
        catch (BoquilhasPersistenceException exception)
        {
            return Map(exception);
        }
    }

    // ------------------------------------------------------------------ append (§17)

    /// <inheritdoc />
    public async Task<BoquilhasResult> AppendMovementAsync(
        AppendMovementCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = BoquilhasValidator.Validate(command);
        if (errors.Count > 0)
        {
            return new BoquilhasResult.ValidationFailed(errors);
        }

        var aggregate = await _boquilhas.GetByIdAsync(command.BoquilhasId, cancellationToken);
        if (aggregate is null)
        {
            return new BoquilhasResult.NotFound(command.BoquilhasId);
        }

        if (aggregate.Version != command.ExpectedAggregateVersion)
        {
            return Refuse(
                BoquilhasRefusalReason.StaleVersion,
                $"O registo foi alterado depois de observado (versão esperada {command.ExpectedAggregateVersion}, atual {aggregate.Version}); nada foi guardado.");
        }

        if (!aggregate.IsActive)
        {
            return Refuse(
                BoquilhasRefusalReason.AggregateClosed,
                "O registo está fechado; reabra-o antes de registar movimentos.");
        }

        var movementKind = MovementKindTokens.Parse(command.MovementType)!.Value;

        if (movementKind == MovementKind.Inicio)
        {
            return Refuse(
                BoquilhasRefusalReason.OnlyOneInicio,
                "O Início é criado com o registo; não pode ser registado um segundo Início.");
        }

        // §17.2 steps 3–4 (UX pre-check; the repository re-asserts the same rules authoritatively
        // inside its transaction from the ledger loaded there).
        var refusal = ValidateMovementAgainstLedger(
            aggregate,
            beforeState: aggregate.Movements,
            kind: movementKind,
            command.Quantity,
            command.Machine,
            command.RepairerId);
        if (refusal is not null)
        {
            return refusal;
        }

        var repairerError = await ValidateRepairerAsync(command.RepairerId, cancellationToken);
        if (repairerError is not null)
        {
            return repairerError;
        }

        var unit = new BoqAppendUnit(
            command.BoquilhasId,
            command.ExpectedAggregateVersion,
            command.MovementType,
            command.Quantity,
            command.BusinessDate,
            command.Machine,
            command.RepairerId,
            Trimmed(command.Observations),
            await ResolveUserIdAsync(cancellationToken));

        try
        {
            // The same-aggregate version marker: every guarded write bumps the aggregate version
            // exactly once (so a concurrent append/edit/close/reopen always loses with
            // stale-version, §11).
            var movement = await _boquilhas.AppendMovementAsync(unit, cancellationToken);

            return new BoquilhasResult.MovementAppended(
                movement.MovementId.Value,
                movement.Version,
                aggregate.Version + 1);
        }
        catch (ConcurrencyConflictException exception)
        {
            return Refuse(BoquilhasRefusalReason.StaleVersion, exception.Message);
        }
        catch (BoquilhasPersistenceException exception)
        {
            return Map(exception);
        }
    }

    // ------------------------------------------------------------------ edit (§19)

    /// <inheritdoc />
    public async Task<BoquilhasResult> EditMovementAsync(
        EditMovementCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = BoquilhasValidator.Validate(command);
        if (errors.Count > 0)
        {
            return new BoquilhasResult.ValidationFailed(errors);
        }

        var aggregate = await _boquilhas.GetByIdAsync(command.BoquilhasId, cancellationToken);
        if (aggregate is null)
        {
            return new BoquilhasResult.NotFound(command.BoquilhasId);
        }

        if (aggregate.Version != command.ExpectedAggregateVersion)
        {
            return Refuse(
                BoquilhasRefusalReason.StaleVersion,
                $"O registo foi alterado depois de observado (versão esperada {command.ExpectedAggregateVersion}, atual {aggregate.Version}); nada foi guardado.");
        }

        if (!aggregate.IsActive)
        {
            return Refuse(
                BoquilhasRefusalReason.AggregateClosed,
                "O registo está fechado; reabra-o antes de editar movimentos.");
        }

        var movement = aggregate.Movements.FirstOrDefault(candidate => candidate.MovementId.Value == command.MovementId);
        if (movement is null)
        {
            return new BoquilhasResult.NotFound(command.MovementId);
        }

        if (movement.Version != command.ExpectedMovementVersion)
        {
            return Refuse(
                BoquilhasRefusalReason.StaleVersion,
                $"O movimento foi alterado depois de observado (versão esperada {command.ExpectedMovementVersion}, atual {movement.Version}); nada foi guardado.");
        }

        // §19.2: replay with the edited movement's CURRENT values excluded, then validate the NEW
        // values exactly like an append — a single net quantity event, no double balance effect.
        var beforeState = aggregate.Movements
            .Where(candidate => candidate.MovementId.Value != command.MovementId)
            .ToList();

        var refusal = ValidateMovementAgainstLedger(
            aggregate,
            beforeState,
            movement.Kind,
            command.Quantity,
            command.Machine,
            command.RepairerId);
        if (refusal is not null)
        {
            return refusal;
        }

        var repairerError = await ValidateRepairerAsync(command.RepairerId, cancellationToken);
        if (repairerError is not null)
        {
            return repairerError;
        }

        var unit = new BoqEditUnit(
            command.BoquilhasId,
            command.ExpectedAggregateVersion,
            command.MovementId,
            command.ExpectedMovementVersion,
            command.Quantity,
            command.BusinessDate,
            command.Machine,
            command.RepairerId,
            Trimmed(command.Observations),
            await ResolveUserIdAsync(cancellationToken));

        try
        {
            var result = await _boquilhas.EditMovementAsync(unit, cancellationToken);

            return new BoquilhasResult.MovementEdited(
                result.Movement.MovementId.Value,
                result.Movement.Version,
                result.AggregateVersion);
        }
        catch (ConcurrencyConflictException exception)
        {
            return Refuse(BoquilhasRefusalReason.StaleVersion, exception.Message);
        }
        catch (BoquilhasPersistenceException exception)
        {
            return Map(exception);
        }
    }

    // ------------------------------------------------------------------ close/reopen (§23)

    /// <inheritdoc />
    public async Task<BoquilhasResult> CloseAsync(
        CloseBoquilhasCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var aggregate = await _boquilhas.GetByIdAsync(command.BoquilhasId, cancellationToken);
        if (aggregate is null)
        {
            return new BoquilhasResult.NotFound(command.BoquilhasId);
        }

        if (aggregate.Version != command.ExpectedVersion)
        {
            return Refuse(
                BoquilhasRefusalReason.StaleVersion,
                $"O registo foi alterado depois de observado (versão esperada {command.ExpectedVersion}, atual {aggregate.Version}); nada foi guardado.");
        }

        if (!aggregate.IsActive)
        {
            return Refuse(
                BoquilhasRefusalReason.AlreadyClosed,
                "O registo já está fechado.");
        }

        try
        {
            var result = await _boquilhas.CloseAsync(
                new BoqCloseUnit(
                    command.BoquilhasId,
                    command.ExpectedVersion,
                    await ResolveUserIdAsync(cancellationToken)),
                cancellationToken);

            return new BoquilhasResult.Closed(
                command.BoquilhasId,
                result.AggregateVersion,
                result.Snapshot.ClosedAt);
        }
        catch (ConcurrencyConflictException exception)
        {
            return Refuse(BoquilhasRefusalReason.StaleVersion, exception.Message);
        }
        catch (BoquilhasPersistenceException exception)
        {
            return Map(exception);
        }
    }

    /// <inheritdoc />
    public async Task<BoquilhasResult> ReopenAsync(
        ReopenBoquilhasCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = BoquilhasValidator.Validate(command);
        if (errors.Count > 0)
        {
            return new BoquilhasResult.ValidationFailed(errors);
        }

        var aggregate = await _boquilhas.GetByIdAsync(command.BoquilhasId, cancellationToken);
        if (aggregate is null)
        {
            return new BoquilhasResult.NotFound(command.BoquilhasId);
        }

        if (aggregate.Version != command.ExpectedVersion)
        {
            return Refuse(
                BoquilhasRefusalReason.StaleVersion,
                $"O registo foi alterado depois de observado (versão esperada {command.ExpectedVersion}, atual {aggregate.Version}); nada foi guardado.");
        }

        if (!aggregate.IsClosed)
        {
            return Refuse(
                BoquilhasRefusalReason.NotClosed,
                "O registo não está fechado; apenas registos fechados podem ser reabertos.");
        }

        // §23.3 steps 6–8 (UX pre-checks; the repository re-asserts the same rules authoritatively
        // inside its transaction, and the partial unique index is the race-safe backstop).
        var eligibility = await ReopenEligibilityAsync(aggregate, cancellationToken);
        if (eligibility is not null)
        {
            return eligibility;
        }

        try
        {
            var result = await _boquilhas.ReopenAsync(
                new BoqReopenUnit(
                    command.BoquilhasId,
                    command.ExpectedVersion,
                    command.Reason.Trim(),
                    await ResolveUserIdAsync(cancellationToken)),
                cancellationToken);

            return new BoquilhasResult.Reopened(
                command.BoquilhasId,
                result.AggregateVersion,
                result.Reopen.ReopenedAt);
        }
        catch (ConcurrencyConflictException exception)
        {
            return Refuse(BoquilhasRefusalReason.StaleVersion, exception.Message);
        }
        catch (BoquilhasPersistenceException exception)
        {
            return Map(exception);
        }
    }

    /// <inheritdoc />
    public async Task<BoquilhasResult> UpdateOpeningFactsAsync(
        UpdateOpeningFactsCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = BoquilhasValidator.Validate(command);
        if (errors.Count > 0)
        {
            return new BoquilhasResult.ValidationFailed(errors);
        }

        var aggregate = await _boquilhas.GetByIdAsync(command.BoquilhasId, cancellationToken);
        if (aggregate is null)
        {
            return new BoquilhasResult.NotFound(command.BoquilhasId);
        }

        if (aggregate.Version != command.ExpectedVersion)
        {
            return Refuse(
                BoquilhasRefusalReason.StaleVersion,
                $"O registo foi alterado depois de observado (versão esperada {command.ExpectedVersion}, atual {aggregate.Version}); nada foi guardado.");
        }

        if (!aggregate.IsActive)
        {
            return Refuse(
                BoquilhasRefusalReason.AggregateClosed,
                "O registo está fechado; reabra-o antes de alterar os dados de abertura.");
        }

        try
        {
            var updated = await _boquilhas.UpdateOpeningFactsAsync(
                new BoqOpeningFactsUnit(
                    command.BoquilhasId,
                    command.ExpectedVersion,
                    command.OpeningDate,
                    command.UtilisationPercent,
                    Trimmed(command.Observations),
                    command.Machines),
                cancellationToken);

            return new BoquilhasResult.OpeningFactsUpdated(updated.BoquilhasId.Value, updated.Version);
        }
        catch (ConcurrencyConflictException exception)
        {
            return Refuse(BoquilhasRefusalReason.StaleVersion, exception.Message);
        }
        catch (BoquilhasPersistenceException exception)
        {
            return Map(exception);
        }
    }

    // ------------------------------------------------------------------ consumed reads (§21)

    /// <inheritdoc />
    public async Task<BoquilhasResult> GetMachineAssignmentsAsync(CancellationToken cancellationToken)
    {
        var assignments = await _assignments.ListAsync(cancellationToken);
        var register = await _repairers.ListAsync(cancellationToken);

        var rows = new List<MachineRepairerAssignmentReadModel>(MachineCode.All.Count);

        foreach (var machine in MachineCode.All)
        {
            var assignment = assignments.FirstOrDefault(candidate =>
                string.Equals(candidate.Machine.Value, machine.Value, StringComparison.Ordinal));

            if (assignment is null)
            {
                rows.Add(new MachineRepairerAssignmentReadModel(machine.Value, null, null, true));
                continue;
            }

            var repairer = register.FirstOrDefault(candidate =>
                candidate.RepairerId.Value == assignment.RepairerId.Value);

            rows.Add(new MachineRepairerAssignmentReadModel(
                machine.Value,
                assignment.RepairerId.Value,
                repairer?.Name,
                false));
        }

        return new BoquilhasResult.AssignmentsFound(rows);
    }

    /// <inheritdoc />
    public async Task<BoquilhasResult> GetRepairersAsync(CancellationToken cancellationToken)
    {
        var register = await _repairers.ListAsync(cancellationToken);

        return new BoquilhasResult.RepairersFound(
            register.Select(repairer => new RepairerReadModel(
                repairer.RepairerId.Value,
                repairer.Name)).ToList());
    }

    // ------------------------------------------------------------------ Job On composition (§22)

    /// <inheritdoc />
    public async Task<BoquilhasResult> FindProductionsAsync(
        JobOnFindProductionsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query.Reference))
        {
            return new BoquilhasResult.ValidationFailed([BoquilhasValidationErrors.ReferenceRequired]);
        }

        var result = await _jobOns.FindProductionsAsync(query, cancellationToken);

        return result switch
        {
            JobOnResult.ProductionsFound(var productions) =>
                new BoquilhasResult.ProductionsFound(productions),
            JobOnResult.ValidationFailed(var errors) =>
                new BoquilhasResult.ValidationFailed(errors),
            _ => throw new InvalidOperationException(
                $"Unexpected Job On result '{result.GetType().Name}' on the productions read."),
        };
    }

    /// <inheritdoc />
    public async Task<BoquilhasResult> GetJobOnAsync(Guid jobOnId, CancellationToken cancellationToken)
    {
        var result = await _jobOns.GetAsync(jobOnId, cancellationToken);

        return result switch
        {
            JobOnResult.Ficha(var ficha) => new BoquilhasResult.JobOnFichaFound(ficha),
            JobOnResult.NotFound(var id) => new BoquilhasResult.NotFound(id),
            JobOnResult.ValidationFailed(var errors) =>
                new BoquilhasResult.ValidationFailed(errors),
            _ => throw new InvalidOperationException(
                $"Unexpected Job On result '{result.GetType().Name}' on the ficha read."),
        };
    }

    /// <inheritdoc />
    public async Task<BoquilhasResult> AssociateBqAsync(
        AssociateBqCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // §22.3: compose ONLY IJobOnService — Keep every fact + Set the BQ slot. The bq_contexts
        // row is created by Job On's own application/repository code (the accepted P2-T05 §21.4
        // composition, BQ slot); the canonical Tool existence/type is validated by Job On itself
        // (TOOL_NOT_FOUND/TOOL_TYPE_MISMATCH) and by the FK backstop.
        var fichaResult = await _jobOns.GetAsync(command.JobOnId, cancellationToken);

        if (fichaResult is not JobOnResult.Ficha(var ficha))
        {
            return fichaResult switch
            {
                JobOnResult.NotFound(var id) => new BoquilhasResult.NotFound(id),
                JobOnResult.ValidationFailed(var errors) =>
                    new BoquilhasResult.ValidationFailed(errors),
                _ => throw new InvalidOperationException(
                    $"Unexpected Job On result '{fichaResult.GetType().Name}' on the association read."),
            };
        }

        if (ficha.Version != command.ExpectedJobOnVersion)
        {
            return Refuse(
                BoquilhasRefusalReason.StaleVersion,
                $"A produção foi alterada depois de observada (versão esperada {command.ExpectedJobOnVersion}, atual {ficha.Version}); nada foi associado.");
        }

        // Tool validation happens inside IJobOnService.UpdateAsync (ResolveToolAsync -> the closed
        // TOOL_NOT_FOUND / TOOL_TYPE_MISMATCH codes).
        var update = await _jobOns.UpdateAsync(
            new UpdateJobOnCommand(
                command.JobOnId,
                command.ExpectedJobOnVersion,
                ficha.Reference,
                ficha.ProductionNumber,
                ficha.Machine,
                ficha.ProductionDate,
                [new ToolAssociationChange(ToolContextType.Bq, ToolAssociationAction.Set, command.ToolId)],
                DateThresholdWarningAcknowledged: true),
            cancellationToken);

        if (update is not JobOnResult.Updated(var updatedJobOnId, var updatedVersion))
        {
            return update switch
            {
                JobOnResult.NotFound(var id) => new BoquilhasResult.NotFound(id),
                JobOnResult.ValidationFailed(var errors) =>
                    new BoquilhasResult.ValidationFailed(errors),
                JobOnResult.Refused(JobOnRefusalReason.StaleVersion, var staleMessage, _, _) =>
                    Refuse(BoquilhasRefusalReason.StaleVersion, staleMessage),
                JobOnResult.Refused(_, var message, _, _) =>
                    Refuse(BoquilhasRefusalReason.StaleVersion, message),
                _ => throw new InvalidOperationException(
                    $"Unexpected Job On result '{update.GetType().Name}' on the BQ-slot Set."),
            };
        }

        // Re-read so the response can name the REAL bq_id the Set created/updated in place.
        var after = await _jobOns.GetAsync(updatedJobOnId, cancellationToken);

        if (after is JobOnResult.Ficha(var afterFicha))
        {
            var bq = afterFicha.Contexts.FirstOrDefault(context =>
                context.ContextType == ToolContextType.Bq);

            if (bq is not null)
            {
                return new BoquilhasResult.BqAssociated(updatedJobOnId, bq.ContextId, updatedVersion);
            }
        }

        throw new InvalidOperationException("The BQ slot Set completed without a bq_contexts row.");
    }

    // ------------------------------------------------------------------ ficha composition

    /// <summary>
    /// Builds the ficha read model: the aggregate facts plus the anchor traversal (frozen
    /// <c>bq_contexts</c> triple for production-linked aggregates / live canonical Tool projection
    /// for standalone aggregates) and the real production context via <c>bq_id → job_ons</c> — the
    /// read-only composition pattern (review N1 resolution; no foreign writes, no cross-module
    /// HTTP).
    /// </summary>
    private async Task<BoquilhasFichaReadModel> BuildFichaAsync(
        BoquilhaAggregate aggregate,
        CancellationToken cancellationToken)
    {
        AnchorContextReadModel? anchor = null;
        ProductionContextReadModel? production = null;
        string? reference = null;
        string? lot = null;

        if (aggregate.BqId is { } bqId)
        {
            var context = await _bqContexts.GetBqContextAsync(bqId, cancellationToken);
            if (context is not null)
            {
                anchor = new AnchorContextReadModel(
                    context.ToolId,
                    context.ToolType,
                    context.ToolReference,
                    context.ToolLot,
                    LiveToolReference: null,
                    LiveToolLot: null,
                    Processo: null,
                    ToolQuantity: null,
                    CompatibleMachines: []);
                reference = context.ToolReference;
                lot = context.ToolLot;

                var jobOnResult = await _jobOns.GetAsync(context.JobOnId, cancellationToken);
                if (jobOnResult is JobOnResult.Ficha(var jobOnFicha))
                {
                    production = new ProductionContextReadModel(
                        jobOnFicha.Reference,
                        jobOnFicha.ProductionNumber,
                        jobOnFicha.Machine,
                        jobOnFicha.ProductionDate);
                }
            }
        }
        else if (aggregate.ToolId is { } toolId)
        {
            var toolResult = await _tools.GetAsync(toolId, cancellationToken);
            if (toolResult is ToolResult.Found(var toolFicha))
            {
                anchor = new AnchorContextReadModel(
                    toolFicha.ToolId,
                    ToolTokens.ToToken(toolFicha.Type),
                    string.Empty,
                    string.Empty,
                    toolFicha.Reference,
                    toolFicha.Lot,
                    ToolTokens.ToToken(toolFicha.Processo),
                    toolFicha.Quantity,
                    toolFicha.CompatibleMachines.Select(machine => machine.Value).ToList());
                reference = toolFicha.Reference;
                lot = toolFicha.Lot;
            }
        }

        var balance = aggregate.Balance;
        var saldo = 0;

        var movements = aggregate.Ledger
            .Select(movement =>
            {
                if (movement.Kind == MovementKind.Entrada)
                {
                    saldo += movement.Quantity;
                }
                else if (movement.Kind == MovementKind.Saida)
                {
                    saldo -= movement.Quantity;
                }

                return new MovementReadModel(
                    movement.MovementId.Value,
                    MovementKindTokens.ToToken(movement.Kind),
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
                    saldo);
            })
            .ToList();

        return new BoquilhasFichaReadModel(
            aggregate.BoquilhasId.Value,
            aggregate.Version,
            BoquilhaStatusTokens.ToToken(aggregate.Status),
            aggregate.BqId,
            aggregate.ToolId,
            reference,
            lot,
            aggregate.Machines.Select(machine => machine.Value).ToList(),
            aggregate.OpeningDate,
            aggregate.UtilisationPercent,
            aggregate.Observations,
            aggregate.CreatedByUserId,
            aggregate.CreatedAt,
            anchor,
            production,
            new BalanceReadModel(balance.Disponivel, balance.EmReparacao, balance.Irreparavel, balance.EntradaExcecional),
            movements,
            aggregate.LastClose is { } close
                ? new CloseSnapshotReadModel(
                    close.CloseSnapshotId,
                    close.ClosedByUserId,
                    close.ClosedAt,
                    close.InitialQuantity,
                    close.OpeningDate,
                    close.Disponivel,
                    close.EmReparacao,
                    close.Irreparavel,
                    close.EntradaExcecional,
                    close.UtilisationPercent)
                : null,
            aggregate.LastReopen is { } reopen
                ? new ReopeningReadModel(
                    reopen.ReopenId,
                    reopen.CloseSnapshotId,
                    reopen.ReopenedByUserId,
                    reopen.ReopenedAt,
                    reopen.Reason)
                : null);
    }

    // ------------------------------------------------------------------ decision helpers

    /// <summary>
    /// §17.2 rules 3–4 over a replay: the Saída/Irreparável balance refusals computed from the
    /// supplied before-state ledger, the machine membership rule and the external-Saída required
    /// facts. Returns a typed result to return, or <c>null</c> when the movement is valid.
    /// </summary>
    private static BoquilhasResult? ValidateMovementAgainstLedger(
        BoquilhaAggregate aggregate,
        IReadOnlyList<BoquilhaMovement> beforeState,
        MovementKind kind,
        int quantity,
        string? machine,
        Guid? repairerId)
    {
        if (kind == MovementKind.Saida)
        {
            if (machine is null)
            {
                return new BoquilhasResult.ValidationFailed([BoquilhasValidationErrors.MachineRequired]);
            }

            if (repairerId is null)
            {
                return new BoquilhasResult.ValidationFailed([BoquilhasValidationErrors.RepairerRequired]);
            }
        }

        if (machine is not null
            && !aggregate.Machines.Any(candidate =>
                string.Equals(candidate.Value, machine, StringComparison.Ordinal)))
        {
            return new BoquilhasResult.ValidationFailed(
                [BoquilhasValidationErrors.MachineNotInAggregate]);
        }

        if (kind == MovementKind.Saida || kind == MovementKind.Irreparavel)
        {
            var before = BalanceProjection.Replay(beforeState);

            if (kind == MovementKind.Saida && quantity > before.Disponivel)
            {
                return Refuse(
                    BoquilhasRefusalReason.SaidaExceedsAvailable,
                    $"A quantidade de Saída ({quantity}) excede o Disponível ({before.Disponivel}); nada foi guardado.");
            }

            if (kind == MovementKind.Irreparavel && quantity > before.EmReparacao)
            {
                return Refuse(
                    BoquilhasRefusalReason.IrreparavelExceedsInRepair,
                    $"A quantidade de Irreparável ({quantity}) excede o Em reparação ({before.EmReparacao}); nada foi guardado.");
            }
        }

        return null;
    }

    /// <summary>Repairer existence against the consumed register read (never administered here).</summary>
    private async Task<BoquilhasResult?> ValidateRepairerAsync(
        Guid? repairerId,
        CancellationToken cancellationToken)
    {
        if (repairerId is null)
        {
            return null;
        }

        if (await _repairers.GetByIdAsync(repairerId.Value, cancellationToken) is null)
        {
            return new BoquilhasResult.ValidationFailed(
                [BoquilhasValidationErrors.RepairerNotFound]);
        }

        return null;
    }

    /// <summary>
    /// §23.3 steps 7–8 (UX pre-checks): this aggregate must hold the most recent close among
    /// aggregates sharing its anchor, and no OTHER active aggregate may share the anchor. The
    /// repository re-asserts both authoritatively inside the reopen transaction (and the partial
    /// unique index is the race-safe backstop).
    /// </summary>
    private async Task<BoquilhasResult?> ReopenEligibilityAsync(
        BoquilhaAggregate aggregate,
        CancellationToken cancellationToken)
    {
        var anchorLastClose = await _boquilhas.GetLastCloseSnapshotIdForAnchorAsync(
            aggregate.BqId,
            aggregate.ToolId,
            cancellationToken);

        if (aggregate.LastClose is null || anchorLastClose != aggregate.LastClose.CloseSnapshotId)
        {
            return Refuse(
                BoquilhasRefusalReason.NotLastClosed,
                "Este registo não é o último registo fechado deste contexto BQ; apenas o último fechado pode ser reaberto.");
        }

        if (await HasActiveAggregateForAnchorAsync(
                aggregate.BqId,
                aggregate.ToolId,
                aggregate.BoquilhasId.Value,
                cancellationToken))
        {
            return Refuse(
                BoquilhasRefusalReason.ActiveAggregateExists,
                "Já existe um registo ativo para este contexto BQ; nada foi reaberto.");
        }

        return null;
    }

    private Task<bool> HasActiveAggregateForAnchorAsync(
        Guid? bqId,
        Guid? toolId,
        Guid excludeBoquilhasId,
        CancellationToken cancellationToken) =>
        _boquilhas.HasActiveAggregateForAnchorAsync(bqId, toolId, excludeBoquilhasId, cancellationToken);

    /// <summary>
    /// The backend actor of every Boquilhas write: the current USER account id
    /// (<c>ICurrentAccountContext</c>, never client-supplied). Every Boquilhas route is USER-gated
    /// (ADMIN gains nothing), so a non-USER account here is an internal invariant violation and
    /// fails closed.
    /// </summary>
    private async Task<Guid> ResolveUserIdAsync(CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);

        return current is CurrentAccount.User(var account)
            ? account.AccountId
            : throw new InvalidOperationException(
                "A Boquilhas write requires an authenticated USER account; none is present.");
    }

    // ------------------------------------------------------------------ mapping

    private static BoquilhasResult Map(BoquilhasPersistenceException exception) =>
        exception.Reason switch
        {
            BoquilhasPersistenceFailureReason.ActiveAggregateExists => Refuse(
                BoquilhasRefusalReason.ActiveAggregateExists,
                "Já existe um registo ativo para este contexto BQ; nada foi guardado."),
            BoquilhasPersistenceFailureReason.AlreadyClosed => Refuse(
                BoquilhasRefusalReason.AlreadyClosed,
                "O registo já está fechado."),
            BoquilhasPersistenceFailureReason.NotClosed => Refuse(
                BoquilhasRefusalReason.NotClosed,
                "O registo não está fechado; apenas registos fechados podem ser reabertos."),
            BoquilhasPersistenceFailureReason.NotLastClosed => Refuse(
                BoquilhasRefusalReason.NotLastClosed,
                "Este registo não é o último registo fechado deste contexto BQ."),
            BoquilhasPersistenceFailureReason.AggregateClosed => Refuse(
                BoquilhasRefusalReason.AggregateClosed,
                "O registo está fechado; reabra-o antes de continuar."),
            BoquilhasPersistenceFailureReason.OnlyOneInicio => Refuse(
                BoquilhasRefusalReason.OnlyOneInicio,
                "O Início é criado com o registo; não pode ser registado um segundo Início."),
            BoquilhasPersistenceFailureReason.SaidaExceedsAvailable => Refuse(
                BoquilhasRefusalReason.SaidaExceedsAvailable,
                "A quantidade de Saída excede o Disponível; nada foi guardado."),
            BoquilhasPersistenceFailureReason.IrreparavelExceedsInRepair => Refuse(
                BoquilhasRefusalReason.IrreparavelExceedsInRepair,
                "A quantidade de Irreparável excede o Em reparação; nada foi guardado."),
            BoquilhasPersistenceFailureReason.ConstraintViolation => new BoquilhasResult.ValidationFailed(
                [exception.ValidatorToken ?? BoquilhasValidationErrors.FilterInvalid]),
            BoquilhasPersistenceFailureReason.BqContextNotFound => new BoquilhasResult.ValidationFailed(
                [BoquilhasValidationErrors.BqContextNotFound]),
            BoquilhasPersistenceFailureReason.ToolNotFound => new BoquilhasResult.ValidationFailed(
                [BoquilhasValidationErrors.ToolNotFound]),
            BoquilhasPersistenceFailureReason.ToolTypeMismatch => new BoquilhasResult.ValidationFailed(
                [BoquilhasValidationErrors.ToolTypeMismatch]),
            BoquilhasPersistenceFailureReason.RepairerNotFound => new BoquilhasResult.ValidationFailed(
                [BoquilhasValidationErrors.RepairerNotFound]),
            _ => throw new ArgumentOutOfRangeException(
                nameof(exception.Reason), exception.Reason, "Unknown Boquilhas persistence failure."),
        };

    private static BoquilhasResult Refuse(BoquilhasRefusalReason reason, string message) =>
        new BoquilhasResult.Refused(reason, message);

    private static string? Trimmed(string? value) =>
        value is null ? null : value.Trim();
}