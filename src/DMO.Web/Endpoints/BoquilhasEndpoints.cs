using DMO.Application.Access;
using DMO.Application.Boquilhas;
using DMO.Application.JobOn;
using DMO.Application.Session;
using DMO.Application.Tools;
using DMO.Domain.Tools;
using DMO.Web.Authorization;

namespace DMO.Web.Endpoints;

/// <summary>
/// Minimal API surface of Boquilhas (P2-T07 contract §13.2 routes 4–18; routes 1–3 are the Razor
/// pages <c>Pages/Boquilhas/Index</c> + <c>Novo</c> + <c>Historico</c>).
/// </summary>
/// <remarks>
/// <para>
/// Every route declares exactly ONE canonical Module policy —
/// <c>ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.Boquilhas)</c> = <c>dmo.module.boquilhas</c>.
/// No route carries a second policy; a grant to any sibling module (<c>job-on-*</c>,
/// <c>controlo-*</c>, <c>ferramentas</c>, …) never satisfies a P2-T07 route; direct-route denial is
/// server-side; ADMIN gains no operational access (§14, AC-A1…A6).</para>
/// <para>
/// The endpoints are thin: they bind transport shapes, call the application service and map the
/// closed result set to HTTP. No domain decision and no persistence live here, and no failure body
/// ever contains a secret, a path, a connection string or another user's data. There is no
/// settings/repairer-administration/PDF/file/email/document/availability route anywhere in this
/// surface (§13.2 route-count statement; the shared Tool search/create stays on the
/// <c>ferramentas</c>-gated P2-T04 routes 12/13 — §16).</para>
/// </remarks>
public static class BoquilhasEndpoints
{
    /// <summary>Base path of the Boquilhas surface.</summary>
    public const string BoquilhasBasePath = "/boquilhas";

    /// <summary>
    /// The canonical policy of every Boquilhas route (the pinned-value pattern of the accepted
    /// <c>JobOnPolicyNames</c>/<c>ControloPolicyNames</c>; the Razor-page constants live in
    /// <c>DMO.Web.Pages.Boquilhas.BoquilhasPolicyNames</c>).
    /// </summary>
    public static string Policy { get; } =
        ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.Boquilhas);

    /// <summary>Non-static logger category marker (static types cannot be generic arguments).</summary>
    public sealed class LoggerCategory;

    /// <summary>Maps the Boquilhas endpoints onto the application.</summary>
    public static WebApplication MapBoquilhasEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup(BoquilhasBasePath)
            .RequireAuthorization(Policy);

        // Route 4 — the Registo lot grid (active default; state/reference/lot/machine filters; paging).
        group.MapGet("/aggregates", async (
            string? state,
            string? reference,
            string? lot,
            string? machine,
            int? page,
            int? pageSize,
            IBoquilhasService service,
            CancellationToken cancellationToken) =>
        {
            var query = new BoquilhasListQuery(
                state,
                reference,
                lot,
                machine,
                page ?? 1,
                pageSize ?? 50);

            var result = await service.GetListAsync(query, cancellationToken);

            return result is BoquilhasResult.ListFound(var rows, var total)
                ? Results.Ok(new AggregateListResponse(
                    rows.Select(row => new AggregateItemResponse(
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
                        .ToArray(),
                    total))
                : MapResult(result);
        });

        // Route 5 — the aggregate ficha: anchor projection, machines, opening facts, status/version,
        // derived balance buckets, movement ledger, close-snapshot/reopen presence.
        group.MapGet("/aggregates/{boquilhasId:guid}", async (
            Guid boquilhasId,
            IBoquilhasService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetAsync(boquilhasId, cancellationToken);

            return result is BoquilhasResult.Ficha(var ficha)
                ? Results.Ok(new { ficha = ToFicha(ficha) })
                : MapResult(result);
        });

        // Route 6 — the per-movement edit/audit trail (edited_at ASC).
        group.MapGet("/aggregates/{boquilhasId:guid}/movements/{movementId:guid}/audit", async (
            Guid boquilhasId,
            Guid movementId,
            IBoquilhasService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetMovementAuditAsync(boquilhasId, movementId, cancellationToken);

            return result is BoquilhasResult.MovementAuditFound(var movement, var entries)
                ? Results.Ok(new MovementAuditResponse(
                    movement,
                    entries.Select(ToAuditItem).ToArray()))
                : MapResult(result);
        });

        // Route 7 — create the aggregate (+ machine set + Início) transactionally.
        group.MapPost("/aggregates", async (
            CreateBoquilhasRequest? body,
            IBoquilhasService service,
            ICurrentAccountContext currentAccount,
            ILogger<LoggerCategory> logger,
            CancellationToken cancellationToken) =>
        {
            if (body is null)
            {
                return ValidationFailed(BoquilhasValidationErrors.AnchorRequired);
            }

            var account = await currentAccount.GetCurrentAsync(cancellationToken);

            if (account is not CurrentAccount.User(var user))
            {
                return MapResult(new BoquilhasResult.ValidationFailed(
                    [BoquilhasValidationErrors.FilterInvalid]));
            }

            var command = new CreateBoquilhasCommand(
                body.BqId,
                body.ToolId,
                body.Machines ?? [],
                body.InitialQuantity,
                body.OpeningDate,
                body.UtilisationPercent,
                body.Observations,
                user.AccountId);

            var result = await service.CreateAsync(command, cancellationToken);

            return await ExecuteAsync(
                result,
                logger,
                success: r => r is BoquilhasResult.Created(var id, var version)
                    ? Results.Created(
                        $"{BoquilhasBasePath}/aggregates/{id}",
                        new CreatedResponse(id, version))
                    : null);
        });

        // Route 8 — append one movement (four types; balance-relative validation).
        group.MapPost("/aggregates/{boquilhasId:guid}/movements", async (
            Guid boquilhasId,
            AppendMovementRequest? body,
            IBoquilhasService service,
            ILogger<LoggerCategory> logger,
            CancellationToken cancellationToken) =>
        {
            if (body is null)
            {
                return ValidationFailed(BoquilhasValidationErrors.MovementTypeInvalid);
            }

            var command = new AppendMovementCommand(
                boquilhasId,
                body.ExpectedAggregateVersion,
                body.MovementType,
                body.Quantity,
                body.BusinessDate,
                body.Machine,
                body.RepairerId,
                body.Observations);

            var result = await service.AppendMovementAsync(command, cancellationToken);

            return await ExecuteAsync(
                result,
                logger,
                success: r => r is BoquilhasResult.MovementAppended(var movementId, var version, var aggregateVersion)
                    ? Results.Created(
                        $"{BoquilhasBasePath}/aggregates/{boquilhasId}/movements/{movementId}",
                        new MovementAppliedResponse(movementId, version, aggregateVersion))
                    : null);
        });

        // Route 9 — edit the existing movement + audit (same row; no second quantity event).
        group.MapPut("/aggregates/{boquilhasId:guid}/movements/{movementId:guid}", async (
            Guid boquilhasId,
            Guid movementId,
            EditMovementRequest? body,
            IBoquilhasService service,
            ILogger<LoggerCategory> logger,
            CancellationToken cancellationToken) =>
        {
            if (body is null)
            {
                return ValidationFailed(BoquilhasValidationErrors.QuantityNotPositive);
            }

            var command = new EditMovementCommand(
                boquilhasId,
                body.ExpectedAggregateVersion,
                movementId,
                body.ExpectedMovementVersion,
                body.Quantity,
                body.BusinessDate,
                body.Machine,
                body.RepairerId,
                body.Observations);

            var result = await service.EditMovementAsync(command, cancellationToken);

            return await ExecuteAsync(
                result,
                logger,
                success: r => r is BoquilhasResult.MovementEdited(var editedMovementId, var version, var aggregateVersion)
                    ? Results.Ok(new MovementEditedResponse(editedMovementId, version, aggregateVersion))
                    : null);
        });

        // Route 10 — close (status + immutable snapshot), SAME boquilhas_id.
        group.MapPost("/aggregates/{boquilhasId:guid}/close", async (
            Guid boquilhasId,
            CloseBoquilhasRequest? body,
            IBoquilhasService service,
            ILogger<LoggerCategory> logger,
            CancellationToken cancellationToken) =>
        {
            if (body is null)
            {
                return ValidationFailed(BoquilhasValidationErrors.FilterInvalid);
            }

            var result = await service.CloseAsync(
                new CloseBoquilhasCommand(boquilhasId, body.ExpectedVersion),
                cancellationToken);

            return await ExecuteAsync(
                result,
                logger,
                success: r => r is BoquilhasResult.Closed(var id, var version, var closedAt)
                    ? Results.Ok(new ClosedResponse(id, version, closedAt))
                    : null);
        });

        // Route 11 — reopen (reason required; exact eligibility), SAME boquilhas_id.
        group.MapPost("/aggregates/{boquilhasId:guid}/reopen", async (
            Guid boquilhasId,
            ReopenBoquilhasRequest? body,
            IBoquilhasService service,
            ILogger<LoggerCategory> logger,
            CancellationToken cancellationToken) =>
        {
            if (body is null)
            {
                return ValidationFailed(BoquilhasValidationErrors.ReopenReasonRequired);
            }

            var result = await service.ReopenAsync(
                new ReopenBoquilhasCommand(boquilhasId, body.ExpectedVersion, body.Reason),
                cancellationToken);

            return await ExecuteAsync(
                result,
                logger,
                success: r => r is BoquilhasResult.Reopened(var id, var version, var reopenedAt)
                    ? Results.Ok(new ReopenedResponse(id, version, reopenedAt))
                    : null);
        });

        // Route 12 — update opening facts (opening date / manual utilisation / observations / machine set).
        group.MapPut("/aggregates/{boquilhasId:guid}/opening-facts", async (
            Guid boquilhasId,
            UpdateOpeningFactsRequest? body,
            IBoquilhasService service,
            ILogger<LoggerCategory> logger,
            CancellationToken cancellationToken) =>
        {
            if (body is null)
            {
                return ValidationFailed(BoquilhasValidationErrors.MachinesRequired);
            }

            var command = new UpdateOpeningFactsCommand(
                boquilhasId,
                body.ExpectedVersion,
                body.OpeningDate,
                body.UtilisationPercent,
                body.Observations,
                body.Machines ?? []);

            var result = await service.UpdateOpeningFactsAsync(command, cancellationToken);

            return await ExecuteAsync(
                result,
                logger,
                success: r => r is BoquilhasResult.OpeningFactsUpdated(var id, var version)
                    ? Results.Ok(new OpeningFactsUpdatedResponse(id, version))
                    : null);
        });

        // Route 13 — reference → productions for the OPENING flow (production-linked; composes
        // IJobOnService; explicit selection, never auto-selected).
        group.MapGet("/productions", async (
            string? reference,
            IBoquilhasService service,
            ILogger<LoggerCategory> logger,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return ValidationFailed(BoquilhasValidationErrors.ReferenceRequired);
            }

            var result = await service.FindProductionsAsync(
                new FindProductionsQuery(reference.Trim()),
                cancellationToken);

            return result is BoquilhasResult.ProductionsFound(var productions)
                ? Results.Ok(new ProductionsResponse(
                    productions.Select(production => new ProductionItemResponse(
                        production.JobOnId,
                        production.Reference,
                        production.ProductionNumber,
                        production.Machine,
                        production.ProductionDate))
                        .ToArray()))
                : MapResult(result, logger);
        });

        // Route 14 — Job On ficha read (production facts + BQ context presence) for the opening
        // flow and the contextual panel.
        group.MapGet("/jobons/{jobonId:guid}", async (
            Guid jobonId,
            IBoquilhasService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetJobOnAsync(jobonId, cancellationToken);

            return result is BoquilhasResult.JobOnFichaFound(var ficha)
                ? Results.Ok(new { ficha = ToJobOnFicha(ficha) })
                : MapResult(result);
        });

        // Route 15 — create the MISSING BQ context through the Job On application contract
        // (Keep every fact + Set the BQ slot): the bq_contexts row is created by Job On's own
        // application/repository code, never by a Boquilhas repository (§22.3; the accepted P2-T05
        // §21.4 composition precedent, BQ slot). Explicit human confirmation only.
        group.MapPost("/jobons/{jobonId:guid}/bq-association", async (
            Guid jobonId,
            BqAssociationRequest? body,
            IBoquilhasService service,
            ILogger<LoggerCategory> logger,
            CancellationToken cancellationToken) =>
        {
            if (body is null)
            {
                return ValidationFailed(BoquilhasValidationErrors.ToolNotFound);
            }

            var result = await service.AssociateBqAsync(
                new AssociateBqCommand(jobonId, body.ToolId, body.ExpectedJobOnVersion),
                cancellationToken);

            return await ExecuteAsync(
                result,
                logger,
                success: r => r is BoquilhasResult.BqAssociated(var associatedJobOnId, var bqId, var version)
                    ? Results.Created(
                        $"{BoquilhasBasePath}/jobons/{associatedJobOnId}",
                        new BqAssociatedResponse(associatedJobOnId, bqId, version))
                    : null);
        });

        // Route 16 — consumed read: the six current machine → repairer assignments (resolution
        // suggestions; absent machine = explicit assignmentUnavailable, never an error).
        group.MapGet("/machine-assignments", async (
            IBoquilhasService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetMachineAssignmentsAsync(cancellationToken);

            return result is BoquilhasResult.AssignmentsFound(var assignments)
                ? Results.Ok(new AssignmentsResponse(
                    assignments.Select(assignment => new AssignmentResponse(
                        assignment.Machine,
                        assignment.RepairerId,
                        assignment.RepairerName,
                        assignment.AssignmentUnavailable))
                        .ToArray()))
                : MapResult(result);
        });

        // Route 17 — consumed read: the repairer register (manual selection list).
        group.MapGet("/repairers", async (
            IBoquilhasService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetRepairersAsync(cancellationToken);

            return result is BoquilhasResult.RepairersFound(var repairers)
                ? Results.Ok(new RepairersResponse(
                    repairers.Select(repairer => new RepairerResponse(
                        repairer.RepairerId,
                        repairer.Name))
                        .ToArray()))
                : MapResult(result);
        });

        // Route 18 — the local Histórico query (full filter set §24.2, backend-applied).
        group.MapGet("/history", async (
            string? state,
            string? reference,
            string? lot,
            string? machine,
            DateOnly? businessDateFrom,
            DateOnly? businessDateTo,
            string? movementType,
            Guid? repairerId,
            int? page,
            int? pageSize,
            IBoquilhasService service,
            CancellationToken cancellationToken) =>
        {
            var query = new BoquilhasHistoryQuery(
                state,
                reference,
                lot,
                machine,
                businessDateFrom,
                businessDateTo,
                movementType,
                repairerId,
                page ?? 1,
                pageSize ?? 50);

            var result = await service.GetHistoryAsync(query, cancellationToken);

            return result is BoquilhasResult.HistoryFound(var rows, var total)
                ? Results.Ok(new HistoryListResponse(
                    rows.Select(row => new HistoryItemResponse(
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
                        .ToArray(),
                    total))
                : MapResult(result);
        });

        return app;
    }

    // ---------------------------------------------------------------------------------------------

    private static async Task<IResult> ExecuteAsync(
        BoquilhasResult result,
        ILogger logger,
        Func<BoquilhasResult, IResult?>? success)
    {
        // Status-level observability only: never payloads, never secrets.
        if (result is BoquilhasResult.Refused(var reason, var message))
        {
            logger.LogWarning("Boquilhas operation refused ({Reason}): {Message}", reason, message);
        }

        return success?.Invoke(result) ?? MapResult(result);
    }

    private static IResult MapResult(BoquilhasResult result, ILogger? logger = null) => result switch
    {
        BoquilhasResult.ListFound(var rows, var total) => Results.Ok(new AggregateListResponse(
            rows.Select(row => new AggregateItemResponse(
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
                .ToArray(),
            total)),

        BoquilhasResult.HistoryFound(var rows, var total) => Results.Ok(new HistoryListResponse(
            rows.Select(row => new HistoryItemResponse(
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
                .ToArray(),
            total)),

        BoquilhasResult.Ficha(var ficha) => Results.Ok(new { ficha = ToFicha(ficha) }),

        BoquilhasResult.MovementAuditFound(var movementId, var entries) =>
            Results.Ok(new MovementAuditResponse(movementId, entries.Select(ToAuditItem).ToArray())),

        BoquilhasResult.Created(var id, var version) =>
            Results.Created($"{BoquilhasBasePath}/aggregates/{id}", new CreatedResponse(id, version)),

        BoquilhasResult.OpeningFactsUpdated(var id, var version) =>
            Results.Ok(new OpeningFactsUpdatedResponse(id, version)),

        BoquilhasResult.MovementAppended(var movementId, var version, var aggregateVersion) =>
            Results.Created(
                $"{BoquilhasBasePath}/aggregates/{movementId}",
                new MovementAppliedResponse(movementId, version, aggregateVersion)),

        BoquilhasResult.MovementEdited(var movementId, var version, var aggregateVersion) =>
            Results.Ok(new MovementEditedResponse(movementId, version, aggregateVersion)),

        BoquilhasResult.Closed(var id, var version, var closedAt) =>
            Results.Ok(new ClosedResponse(id, version, closedAt)),

        BoquilhasResult.Reopened(var id, var version, var reopenedAt) =>
            Results.Ok(new ReopenedResponse(id, version, reopenedAt)),

        BoquilhasResult.ProductionsFound(var productions) => Results.Ok(new ProductionsResponse(
            productions.Select(production => new ProductionItemResponse(
                production.JobOnId,
                production.Reference,
                production.ProductionNumber,
                production.Machine,
                production.ProductionDate))
                .ToArray())),

        BoquilhasResult.JobOnFichaFound(var ficha) => Results.Ok(new { ficha = ToJobOnFicha(ficha) }),

        BoquilhasResult.BqAssociated(var jobOnId, var bqId, var version) =>
            Results.Created(
                $"{BoquilhasBasePath}/jobons/{jobOnId}",
                new BqAssociatedResponse(jobOnId, bqId, version)),

        BoquilhasResult.AssignmentsFound(var assignments) => Results.Ok(new AssignmentsResponse(
            assignments.Select(assignment => new AssignmentResponse(
                assignment.Machine,
                assignment.RepairerId,
                assignment.RepairerName,
                assignment.AssignmentUnavailable))
                .ToArray())),

        BoquilhasResult.RepairersFound(var repairers) => Results.Ok(new RepairersResponse(
            repairers.Select(repairer => new RepairerResponse(
                repairer.RepairerId,
                repairer.Name))
                .ToArray())),

        BoquilhasResult.ValidationFailed(var errors) => ValidationFailed(errors),

        BoquilhasResult.NotFound(var id) => Results.NotFound(new { reason = "not-found", id }),

        BoquilhasResult.Refused(var reason, var message) =>
            Results.Conflict(new BoquilhasRefusalResponse(RefusalToken(reason), message)),

        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };

    private static IResult ValidationFailed(params string[] errors) =>
        Results.BadRequest(new { reason = "validation-failed", errors });

    private static IResult ValidationFailed(IReadOnlyList<string> errors) =>
        Results.BadRequest(new { reason = "validation-failed", errors });

    /// <summary>The exact transport token of a Boquilhas refusal (contract §12.3).</summary>
    public static string RefusalToken(BoquilhasRefusalReason reason) => reason switch
    {
        BoquilhasRefusalReason.StaleVersion => "stale-version",
        BoquilhasRefusalReason.SaidaExceedsAvailable => "saida-exceeds-available",
        BoquilhasRefusalReason.IrreparavelExceedsInRepair => "irreparavel-exceeds-in-repair",
        BoquilhasRefusalReason.ActiveAggregateExists => "active-aggregate-exists",
        BoquilhasRefusalReason.AlreadyClosed => "already-closed",
        BoquilhasRefusalReason.NotClosed => "not-closed",
        BoquilhasRefusalReason.NotLastClosed => "not-last-closed",
        BoquilhasRefusalReason.AggregateClosed => "aggregate-closed",
        BoquilhasRefusalReason.OnlyOneInicio => "only-one-inicio",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown refusal reason."),
    };

    private static FichaResponse ToFicha(BoquilhasFichaReadModel ficha) => new(
        ficha.BoquilhasId,
        ficha.Version,
        ficha.State,
        ficha.BqId,
        ficha.ToolId,
        ficha.Reference,
        ficha.Lot,
        ficha.Machines,
        ficha.OpeningDate,
        ficha.UtilisationPercent,
        ficha.Observations,
        ficha.CreatedByUserId,
        ficha.CreatedAt,
        ficha.Anchor is { } anchor
            ? new AnchorResponse(
                anchor.ToolId,
                anchor.FrozenToolType,
                anchor.FrozenToolReference,
                anchor.FrozenToolLot,
                anchor.LiveToolReference,
                anchor.LiveToolLot,
                anchor.Processo,
                anchor.ToolQuantity,
                anchor.CompatibleMachines)
            : null,
        ficha.Production is { } production
            ? new ProductionContextResponse(
                production.Reference,
                production.ProductionNumber,
                production.Machine,
                production.ProductionDate)
            : null,
        new BalanceResponse(
            ficha.Balance.Disponivel,
            ficha.Balance.EmReparacao,
            ficha.Balance.Irreparavel,
            ficha.Balance.EntradaExcecional),
        ficha.Movements.Select(movement => new MovementResponse(
            movement.MovementId,
            movement.MovementType,
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
            movement.Saldo))
            .ToArray(),
        ficha.LastClose is { } close
            ? new CloseSnapshotResponse(
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
        ficha.LastReopen is { } reopen
            ? new ReopeningResponse(
                reopen.ReopenId,
                reopen.CloseSnapshotId,
                reopen.ReopenedByUserId,
                reopen.ReopenedAt,
                reopen.Reason)
            : null);

    private static MovementAuditItemResponse ToAuditItem(MovementAuditItemReadModel entry) => new(
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
        entry.AfterObservations);

    private static JobOnFichaResponse ToJobOnFicha(JobOnFicha ficha) => new(
        ficha.JobOnId,
        ficha.Reference,
        ficha.ProductionNumber,
        ficha.Machine,
        ficha.ProductionDate,
        ficha.Version,
        ficha.Contexts.Select(context => new JobOnBqContextResponse(
            context.ContextId,
            context.ToolId,
            ToolTokens.ToToken(context.ToolType),
            context.ToolReference,
            context.ToolLot))
            .ToArray());

    // ---------------------------------------------------------------------------------------------
    // Transport shapes (route table §13.2)
    // ---------------------------------------------------------------------------------------------

    /// <summary>Route 7 create carrier: anchor + opening facts. The backend resolves the actor.</summary>
    public sealed record CreateBoquilhasRequest(
        Guid? BqId,
        Guid? ToolId,
        IReadOnlyList<string>? Machines,
        int InitialQuantity,
        DateOnly OpeningDate,
        decimal? UtilisationPercent,
        string? Observations);

    /// <summary>Route 8 append carrier: observed aggregate version + the movement facts.</summary>
    public sealed record AppendMovementRequest(
        int ExpectedAggregateVersion,
        string MovementType,
        int Quantity,
        DateOnly BusinessDate,
        string? Machine,
        Guid? RepairerId,
        string? Observations);

    /// <summary>Route 9 edit carrier: both observed versions + the new editable values only.</summary>
    public sealed record EditMovementRequest(
        int ExpectedAggregateVersion,
        int ExpectedMovementVersion,
        int Quantity,
        DateOnly BusinessDate,
        string? Machine,
        Guid? RepairerId,
        string? Observations);

    /// <summary>Route 10 close carrier: identity + observed version only.</summary>
    public sealed record CloseBoquilhasRequest(int ExpectedVersion);

    /// <summary>Route 11 reopen carrier: identity + observed version + required reason.</summary>
    public sealed record ReopenBoquilhasRequest(int ExpectedVersion, string Reason);

    /// <summary>Route 12 opening-facts carrier: the aggregate context (+ machine set replace).</summary>
    public sealed record UpdateOpeningFactsRequest(
        int ExpectedVersion,
        DateOnly OpeningDate,
        decimal? UtilisationPercent,
        string? Observations,
        IReadOnlyList<string>? Machines);

    /// <summary>Route 15 BQ-association carrier: the canonical Tool + the observed Job On version.</summary>
    public sealed record BqAssociationRequest(Guid ToolId, int ExpectedJobOnVersion);

    /// <summary>Route 4 response.</summary>
    public sealed record AggregateListResponse(IReadOnlyList<AggregateItemResponse> Rows, int Total);

    /// <summary>One route 4 row.</summary>
    public sealed record AggregateItemResponse(
        Guid BoquilhasId,
        int Version,
        string State,
        Guid? BqId,
        Guid? ToolId,
        string? Reference,
        string? Lot,
        IReadOnlyList<string> Machines,
        DateOnly OpeningDate,
        int InitialQuantity,
        int Disponivel,
        int EmReparacao,
        int Irreparavel,
        int EntradaExcecional);

    /// <summary>Route 5 response.</summary>
    public sealed record FichaResponse(
        Guid BoquilhasId,
        int Version,
        string State,
        Guid? BqId,
        Guid? ToolId,
        string? Reference,
        string? Lot,
        IReadOnlyList<string> Machines,
        DateOnly OpeningDate,
        decimal? UtilisationPercent,
        string? Observations,
        Guid CreatedByUserId,
        DateTimeOffset CreatedAt,
        AnchorResponse? Anchor,
        ProductionContextResponse? Production,
        BalanceResponse Balance,
        IReadOnlyList<MovementResponse> Movements,
        CloseSnapshotResponse? LastClose,
        ReopeningResponse? LastReopen);

    /// <summary>The anchor context of a route 5 ficha (frozen triple or live Tool projection).</summary>
    public sealed record AnchorResponse(
        Guid ToolId,
        string? FrozenToolType,
        string? FrozenToolReference,
        string? FrozenToolLot,
        string? LiveToolReference,
        string? LiveToolLot,
        string? Processo,
        int? ToolQuantity,
        IReadOnlyList<string> CompatibleMachines);

    /// <summary>The real production context of a route 5 ficha (production-linked only).</summary>
    public sealed record ProductionContextResponse(
        string Reference,
        string ProductionNumber,
        string Machine,
        DateOnly? ProductionDate);

    /// <summary>The derived balance buckets of a route 5 ficha.</summary>
    public sealed record BalanceResponse(
        int Disponivel,
        int EmReparacao,
        int Irreparavel,
        int EntradaExcecional);

    /// <summary>One movement of the route 5 ledger (with the display-only Saldo projection).</summary>
    public sealed record MovementResponse(
        Guid MovementId,
        string MovementType,
        int Quantity,
        DateOnly BusinessDate,
        DateTimeOffset RecordedAt,
        Guid RecordedByUserId,
        string? Machine,
        Guid? RepairerId,
        int? ExpectedReturnQuantity,
        int? ExcessReceivedQuantity,
        string? Observations,
        int Version,
        int Saldo);

    /// <summary>The last close snapshot of a route 5 ficha, when closed.</summary>
    public sealed record CloseSnapshotResponse(
        Guid CloseSnapshotId,
        Guid ClosedByUserId,
        DateTimeOffset ClosedAt,
        int InitialQuantity,
        DateOnly OpeningDate,
        int Disponivel,
        int EmReparacao,
        int Irreparavel,
        int EntradaExcecional,
        decimal? UtilisationPercent);

    /// <summary>The last reopen record of a route 5 ficha, when present.</summary>
    public sealed record ReopeningResponse(
        Guid ReopenId,
        Guid CloseSnapshotId,
        Guid ReopenedByUserId,
        DateTimeOffset ReopenedAt,
        string Reason);

    /// <summary>Route 6 response: the per-movement edit/audit trail.</summary>
    public sealed record MovementAuditResponse(Guid MovementId, IReadOnlyList<MovementAuditItemResponse> Entries);

    /// <summary>One route 6 audit item (before/after of every editable field + backend actor/time).</summary>
    public sealed record MovementAuditItemResponse(
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

    /// <summary>Route 7 response.</summary>
    public sealed record CreatedResponse(Guid BoquilhasId, int Version);

    /// <summary>Route 12 response.</summary>
    public sealed record OpeningFactsUpdatedResponse(Guid BoquilhasId, int Version);

    /// <summary>Route 8 response.</summary>
    public sealed record MovementAppliedResponse(Guid MovementId, int Version, int AggregateVersion);

    /// <summary>Route 9 response.</summary>
    public sealed record MovementEditedResponse(Guid MovementId, int Version, int AggregateVersion);

    /// <summary>Route 10 response.</summary>
    public sealed record ClosedResponse(Guid BoquilhasId, int Version, DateTimeOffset ClosedAt);

    /// <summary>Route 11 response.</summary>
    public sealed record ReopenedResponse(Guid BoquilhasId, int Version, DateTimeOffset ReopenedAt);

    /// <summary>Route 13 response.</summary>
    public sealed record ProductionsResponse(IReadOnlyList<ProductionItemResponse> Productions);

    /// <summary>One route 13 production item (the accepted P2-T04 carrier shape).</summary>
    public sealed record ProductionItemResponse(
        Guid JobonId,
        string Reference,
        string ProductionNumber,
        string Machine,
        DateOnly? ProductionDate);

    /// <summary>Route 14 response: the Job On ficha facts + context presence.</summary>
    public sealed record JobOnFichaResponse(
        Guid JobOnId,
        string Reference,
        string ProductionNumber,
        string Machine,
        DateOnly? ProductionDate,
        int Version,
        IReadOnlyList<JobOnBqContextResponse> Contexts);

    /// <summary>One BQ context of a route 14 ficha.</summary>
    public sealed record JobOnBqContextResponse(
        Guid ContextId,
        Guid ToolId,
        string ToolType,
        string ToolReference,
        string ToolLot);

    /// <summary>Route 15 response: the REAL bq_id created/updated by Job On's own code.</summary>
    /// <remarks>The transport token is <c>jobonId</c> (the accepted P2-T04 convention).</remarks>
    public sealed record BqAssociatedResponse(Guid JobonId, Guid BqId, int Version);

    /// <summary>Route 16 response.</summary>
    public sealed record AssignmentsResponse(IReadOnlyList<AssignmentResponse> Assignments);

    /// <summary>One route 16 assignment (absent row = explicit <c>assignmentUnavailable</c>).</summary>
    public sealed record AssignmentResponse(
        string Machine,
        Guid? RepairerId,
        string? RepairerName,
        bool AssignmentUnavailable);

    /// <summary>Route 17 response.</summary>
    public sealed record RepairersResponse(IReadOnlyList<RepairerResponse> Repairers);

    /// <summary>One route 17 repairer.</summary>
    public sealed record RepairerResponse(Guid RepairerId, string Name);

    /// <summary>Route 18 response.</summary>
    public sealed record HistoryListResponse(IReadOnlyList<HistoryItemResponse> Rows, int Total);

    /// <summary>One route 18 row (the exact §24.2 shape).</summary>
    public sealed record HistoryItemResponse(
        Guid BoquilhasId,
        int Version,
        string State,
        Guid? BqId,
        Guid? ToolId,
        string? Reference,
        string? Lot,
        IReadOnlyList<string> Machines,
        DateOnly OpeningDate,
        int InitialQuantity,
        int Disponivel,
        int EmReparacao,
        int Irreparavel,
        int EntradaExcecional,
        int MovementCount,
        DateTimeOffset? ClosedAt,
        Guid? ClosedByUserId,
        DateTimeOffset? LastMovementAt);

    /// <summary>Typed, actionable refusal response.</summary>
    public sealed record BoquilhasRefusalResponse(string Reason, string Message);
}