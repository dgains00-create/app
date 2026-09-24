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
/// The Boquilhas application service: the production movement register — register identity
/// creation (REAL BQ context XOR the transitional pré-JobOn Tool anchor, P2-T07 §34), the
/// three-type movement ledger, edit/audit on the SAME movement, the derived outstanding, the local
/// Histórico and the human-confirmed §34 association — composing the closed P2-T04/P2-T05
/// application contracts.
/// </summary>
/// <remarks>
/// Authority: P2-T07 OWNER CLARIFICATION (the production movement register), §34 (the transitional
/// pré-JobOn register on the canonical <c>tool_id</c>; the association presented when a <c>bq_id</c>
/// whose <c>bq_contexts.tool_id</c> equals the pending anchor arrives; the SAME
/// <c>boquilhas_id</c> passes to <c>bq_id → jobon_id</c> only after human confirmation — the same
/// accepted Peso associate pattern of P2-T05 §4.4: the anchor must match, no inference rule),
/// preserving: the production association (every settled register belongs to a REAL Job On/BQ
/// context; movements remain valid AFTER the production end date — the production stays the
/// historical context), the movement vocabulary (Saída / Entrada / Entrada sem reparação), the
/// derived outstanding formula (Σ Saída − Σ Entrada − Σ Entrada sem reparação; negative is a valid
/// visible projection), the edit/audit single-event semantics, the immutable dates and the
/// repairer historical preservation.
/// <para>
/// There is NO lifecycle: no close/reopen/opening-facts operation, no status, no one-active rule
/// and no PERMANENT standalone anchor — the only standalone-like state is the transitional §34
/// pré-JobOn anchor (pending registers are presented with their matching candidates and are never
/// auto-associated). The register creation never manufactures a quantity event. The
/// Web layer never queries the database directly; the service never calls a P2-T05/P2-T04/P2-T06-
/// gated HTTP route. Every persisted-state refusal is a typed <see cref="BoquilhasResult"/>;
/// actor/time facts are backend-authored and never client-supplied.</para>
/// </remarks>
public sealed class BoquilhasService : IBoquilhasService
{
    private readonly IBoquilhasRepository _boquilhas;
    private readonly IJobOnService _jobOns;
    private readonly IToolRepository _tools;
    private readonly IRepairerRepository _repairers;
    private readonly IMachineRepairerAssignmentRepository _assignments;
    private readonly IBoquilhasContextRead _bqContexts;
    private readonly ICurrentAccountContext _currentAccount;

    /// <summary>Creates the service over the Boquilhas repository and the consumed application contracts.</summary>
    public BoquilhasService(
        IBoquilhasRepository boquilhas,
        IJobOnService jobOns,
        IToolRepository tools,
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

    // ------------------------------------------------------------------ reads

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
        var total = await _boquilhas.CountListAsync(query, cancellationToken);

        return new BoquilhasResult.ListFound(
            rows.Select(row => new RegisterListItemReadModel(
                row.BoquilhasId,
                row.BqId,
                row.ToolId,
                row.Reference,
                row.Lot,
                row.ProductionNumber,
                row.ProductionMachine,
                row.ProductionDate,
                row.Outstanding,
                row.MovementCount,
                row.LastMovementAt))
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
        var total = await _boquilhas.CountHistoryAsync(query, cancellationToken);

        return new BoquilhasResult.HistoryFound(
            rows.Select(row => new HistoryMovementItemReadModel(
                row.MovementId,
                row.BoquilhasId,
                row.MovementType,
                row.Quantity,
                row.BusinessDate,
                row.RecordedAt,
                row.RecordedByUserId,
                row.Machine,
                row.RepairerId,
                row.Observations,
                row.Reference,
                row.Lot,
                row.ProductionNumber,
                row.ProductionMachine))
                .ToList(),
            total);
    }

    /// <inheritdoc />
    public async Task<BoquilhasResult> GetAsync(Guid boquilhasId, CancellationToken cancellationToken)
    {
        var register = await _boquilhas.GetByIdAsync(boquilhasId, cancellationToken);
        if (register is null)
        {
            return new BoquilhasResult.NotFound(boquilhasId);
        }

        return new BoquilhasResult.Ficha(await BuildFichaAsync(register, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<BoquilhasResult> GetMovementAuditAsync(
        Guid boquilhasId,
        Guid movementId,
        CancellationToken cancellationToken)
    {
        var register = await _boquilhas.GetByIdAsync(boquilhasId, cancellationToken);
        if (register is null || !register.Movements.Any(movement => movement.MovementId.Value == movementId))
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

    // ------------------------------------------------------------------ register creation (route 4)

    /// <inheritdoc />
    public async Task<BoquilhasResult> CreateAsync(
        CreateBoquilhaRegisterCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = BoquilhasValidator.Validate(command);
        if (errors.Count > 0)
        {
            return new BoquilhasResult.ValidationFailed(errors);
        }

        // EXACTLY ONE anchor. Production association: the register anchors a REAL bq_contexts row
        // (never a fake production/bq id). Pré-JobOn (§34.1): the provisional anchor is a REAL
        // canonical BQ Tool — never a minted identity, never a fake Job On. The repository
        // re-asserts the anchor existence authoritatively inside the create transaction.
        if (command.BqId is { } bqId)
        {
            if (await _bqContexts.GetBqContextAsync(bqId, cancellationToken) is null)
            {
                return new BoquilhasResult.ValidationFailed(
                    [BoquilhasValidationErrors.BqContextNotFound]);
            }
        }
        else
        {
            var tool = await _tools.GetByIdAsync(command.PendingToolId!.Value, cancellationToken);
            if (tool is null)
            {
                return new BoquilhasResult.ValidationFailed(
                    [BoquilhasValidationErrors.ToolNotFound]);
            }

            if (tool.Type != ToolType.Bq)
            {
                return new BoquilhasResult.ValidationFailed(
                    [BoquilhasValidationErrors.ToolTypeMismatch]);
            }
        }

        try
        {
            var created = await _boquilhas.CreatedAsync(
                new BoqCreateUnit(command.BqId, command.PendingToolId, command.CreatedByUserId),
                cancellationToken);

            return new BoquilhasResult.RegisterCreated(created.BoquilhasId.Value);
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

    // ------------------------------------------------------------------ association (P2-T07 §34.1 rules 2–3)

    /// <inheritdoc />
    public async Task<BoquilhasResult> AssociateAsync(
        AssociateBoquilhasCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = BoquilhasValidator.Validate(command);
        if (errors.Count > 0)
        {
            return new BoquilhasResult.ValidationFailed(errors);
        }

        var register = await _boquilhas.GetByIdAsync(command.BoquilhasId, cancellationToken);
        if (register is null)
        {
            return new BoquilhasResult.NotFound(command.BoquilhasId);
        }

        // Association is offered only while pending (§34.1 rule 2): a production-linked register
        // is never silently re-associated — the provisional anchor is never kept as a concurrent
        // operational authority.
        if (!register.IsPending)
        {
            return Refuse(
                BoquilhasRefusalReason.AlreadyAssociated,
                "Este registo já está associado a uma produção; a associação só é oferecida enquanto o registo está pendente.");
        }

        if (register.Version != command.ExpectedVersion)
        {
            return Refuse(
                BoquilhasRefusalReason.StaleVersion,
                $"O registo foi alterado depois de observado (versão esperada {command.ExpectedVersion}, atual {register.Version}); nada foi associado.");
        }

        // The candidate bq_id must exist (BQ_CONTEXT_NOT_FOUND) and must resolve to the register's
        // pending tool_id (ASSOCIATION_MISMATCH) — the SAME canonical UUID proves the SAME Tool
        // (§34.1 rule 2; the anchor must match; no inference rule, never a guess by
        // reference/lote/máquina/texto).
        var target = await _bqContexts.GetBqContextAsync(command.BqId, cancellationToken);
        if (target is null)
        {
            return new BoquilhasResult.ValidationFailed(
                [BoquilhasValidationErrors.BqContextNotFound]);
        }

        if (target.ToolId != register.ToolId)
        {
            return Refuse(
                BoquilhasRefusalReason.AssociationMismatch,
                "O contexto BQ indicado não corresponde ao tool_id pendente deste registo; a associação foi recusada (a aplicação nunca adivinha).");
        }

        try
        {
            // The repository performs the guarded SAME-row UPDATE (bq_id set, provisional tool
            // anchor cleared, version + 1) in one transaction — no new aggregate, no copied
            // movements, no fake Job On.
            var associated = await _boquilhas.AssociatedAsync(
                command.BoquilhasId,
                command.BqId,
                command.ExpectedVersion,
                cancellationToken);

            return new BoquilhasResult.Associated(
                associated.BoquilhasId.Value,
                associated.Version,
                associated.BqId!.Value);
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
    public async Task<BoquilhasResult> GetAssociationCandidatesAsync(
        Guid boquilhasId,
        CancellationToken cancellationToken)
    {
        var register = await _boquilhas.GetByIdAsync(boquilhasId, cancellationToken);
        if (register is null)
        {
            return new BoquilhasResult.NotFound(boquilhasId);
        }

        // A production-linked register has no candidates: the association point is past. The
        // truthful empty is returned (the surface presents no association panel for it).
        if (!register.IsPending)
        {
            return new BoquilhasResult.AssociationCandidatesFound([]);
        }

        var candidates = await _boquilhas.ListBqAssociationCandidatesAsync(
            register.ToolId!.Value,
            cancellationToken);

        return new BoquilhasResult.AssociationCandidatesFound(
            candidates.Select(candidate => new BqAssociationCandidateReadModel(
                candidate.BqId,
                candidate.JobOnId,
                candidate.Reference,
                candidate.ProductionNumber,
                candidate.Machine,
                candidate.ProductionDate))
                .ToList());
    }

    /// <inheritdoc />
    public async Task<BoquilhasResult> GetPendingRegistersAsync(
        Guid bqId,
        CancellationToken cancellationToken)
    {
        // The §34.1 rule-2 read in the Job-On-incoming direction: the context arrives as bq_id →
        // tool_id (NEVER "which Job On do I belong to?" — no global scan); the pending registers
        // of THAT canonical Tool are the specific query result.
        var context = await _bqContexts.GetBqContextAsync(bqId, cancellationToken);
        if (context is null)
        {
            return new BoquilhasResult.ValidationFailed(
                [BoquilhasValidationErrors.BqContextNotFound]);
        }

        var pending = await _boquilhas.ListPendingRegistersAsync(context.ToolId, cancellationToken);

        return new BoquilhasResult.PendingRegistersFound(
            pending.Select(candidate => new PendingRegisterCandidateReadModel(
                candidate.BoquilhasId,
                candidate.ToolId,
                candidate.ToolReference,
                candidate.ToolLot,
                candidate.Version,
                candidate.CreatedAt,
                candidate.MovementCount,
                candidate.Outstanding))
                .ToList());
    }

    // ------------------------------------------------------------------ append (route 5)

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

        var register = await _boquilhas.GetByIdAsync(command.BoquilhasId, cancellationToken);
        if (register is null)
        {
            return new BoquilhasResult.NotFound(command.BoquilhasId);
        }

        // The repairer is the consumed canonical register read (never administered here) and the
        // historical fact is frozen on the movement row.
        var repairerError = await ValidateRepairerAsync(command.RepairerId, cancellationToken);
        if (repairerError is not null)
        {
            return repairerError;
        }

        var unit = new BoqAppendUnit(
            command.BoquilhasId,
            command.MovementType,
            command.Quantity,
            command.BusinessDate,
            command.Machine,
            command.RepairerId,
            Trimmed(command.Observations),
            await ResolveUserIdAsync(cancellationToken));

        try
        {
            var movement = await _boquilhas.AppendMovementAsync(unit, cancellationToken);

            return new BoquilhasResult.MovementAppended(movement.MovementId.Value, movement.Version);
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

    // ------------------------------------------------------------------ edit (route 6)

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

        var register = await _boquilhas.GetByIdAsync(command.BoquilhasId, cancellationToken);
        if (register is null)
        {
            return new BoquilhasResult.NotFound(command.BoquilhasId);
        }

        var movement = register.Movements.FirstOrDefault(candidate => candidate.MovementId.Value == command.MovementId);
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

        // The stored type is immutable; a Saída keeps requiring its machine/repairer facts
        // (the repository re-asserts the same rule in-transaction from the stored type).
        if (movement.Kind == MovementKind.Saida)
        {
            if (command.Machine is null)
            {
                return new BoquilhasResult.ValidationFailed([BoquilhasValidationErrors.MachineRequired]);
            }

            if (command.RepairerId is null)
            {
                return new BoquilhasResult.ValidationFailed([BoquilhasValidationErrors.RepairerRequired]);
            }
        }

        var repairerError = await ValidateRepairerAsync(command.RepairerId, cancellationToken);
        if (repairerError is not null)
        {
            return repairerError;
        }

        var unit = new BoqEditUnit(
            command.BoquilhasId,
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
                result.Movement.Version);
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

    // ------------------------------------------------------------------ consumed reads

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

    // ------------------------------------------------------------------ Job On composition

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

        // Compose ONLY IJobOnService — Keep every fact + Set the BQ slot. The bq_contexts row is
        // created by Job On's own application/repository code (the accepted P2-T05 §21.4
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
    /// Builds the register ficha: the register facts plus the production context via the REAL
    /// <c>bq_id → bq_contexts → job_ons</c> chain (frozen triple + production facts) — the accepted
    /// read-only composition pattern (no foreign writes, no cross-module HTTP) — or, while the
    /// register is pending (§34), the canonical Tool facts of the provisional anchor.
    /// </summary>
    private async Task<RegisterFichaReadModel> BuildFichaAsync(
        BoquilhaRegister register,
        CancellationToken cancellationToken)
    {
        AnchorContextReadModel? anchor = null;
        PendingToolFactsReadModel? pendingTool = null;
        ProductionContextReadModel? production = null;
        string? reference = null;
        string? lot = null;

        if (register.IsPending)
        {
            // The transitional pré-JobOn state: the canonical BQ Tool row IS the anchor (§34.1).
            if (register.ToolId is { } toolId)
            {
                var tool = await _tools.GetByIdAsync(toolId, cancellationToken);
                if (tool is not null)
                {
                    pendingTool = new PendingToolFactsReadModel(
                        tool.ToolId.Value,
                        ToolTokens.ToToken(tool.Type),
                        tool.Reference,
                        tool.Lot);
                    reference = tool.Reference;
                    lot = tool.Lot;
                }
            }
        }
        else if (register.BqId is { } bqId)
        {
            var context = await _bqContexts.GetBqContextAsync(bqId, cancellationToken);
            if (context is not null)
            {
                anchor = new AnchorContextReadModel(
                    context.ToolId,
                    context.ToolType,
                    context.ToolReference,
                    context.ToolLot);
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

        var saldo = 0;

        var movements = register.Ledger
            .Select(movement =>
            {
                saldo += movement.Kind switch
                {
                    MovementKind.Saida => movement.Quantity,
                    _ => -movement.Quantity,
                };

                return new MovementReadModel(
                    movement.MovementId.Value,
                    MovementKindTokens.ToToken(movement.Kind),
                    movement.Quantity,
                    movement.BusinessDate,
                    movement.RecordedAt,
                    movement.RecordedByUserId,
                    movement.Machine,
                    movement.RepairerId,
                    movement.Observations,
                    movement.Version,
                    saldo);
            })
            .ToList();

        return new RegisterFichaReadModel(
            register.BoquilhasId.Value,
            register.BqId,
            register.ToolId,
            reference,
            lot,
            register.Outstanding,
            register.CreatedByUserId,
            register.CreatedAt,
            register.Version,
            anchor,
            pendingTool,
            production,
            movements);
    }

    // ------------------------------------------------------------------ decision helpers

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
            BoquilhasPersistenceFailureReason.RegisterExists => Refuse(
                BoquilhasRefusalReason.RegisterExists,
                "Já existe um registo de Boquilhas para esta produção; nada foi criado."),
            BoquilhasPersistenceFailureReason.AlreadyAssociated => Refuse(
                BoquilhasRefusalReason.AlreadyAssociated,
                "Este registo já está associado a uma produção; a associação só é oferecida enquanto o registo está pendente."),
            BoquilhasPersistenceFailureReason.ConstraintViolation => new BoquilhasResult.ValidationFailed(
                [exception.ValidatorToken ?? BoquilhasValidationErrors.FilterInvalid]),
            BoquilhasPersistenceFailureReason.BqContextNotFound => new BoquilhasResult.ValidationFailed(
                [BoquilhasValidationErrors.BqContextNotFound]),
            BoquilhasPersistenceFailureReason.ToolNotFound => new BoquilhasResult.ValidationFailed(
                [BoquilhasValidationErrors.ToolNotFound]),
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