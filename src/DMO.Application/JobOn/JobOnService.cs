using DMO.Application.Persistence;
using DMO.Application.Repositories;
using DMO.Application.Tools;
using DMO.Domain.JobOn;
using DMO.Domain.Tools;
using DomainJobOn = DMO.Domain.JobOn.JobOn;

namespace DMO.Application.JobOn;

/// <summary>
/// The Job On application service: occurrence read/create/edit/duplicate/delete over the real
/// <c>jobon_id</c>, with the date-threshold warning gate and the dependency-driven delete rule.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §6, §8.1, §8.3, §8.4, §10, §11, §12.3.
/// <para>
/// There is no Job On-wide lifecycle or status state anywhere in this service, and no
/// <c>production_id</c> or revision identity exists. The only protections are the two fact-based
/// ones that authority fixes: the planned-production-date warning and the existence of dependent
/// operational facts.
/// </para>
/// <para>
/// Canonical Tool resolution is performed here as a fast pre-check that produces the contracted
/// validation codes, and again authoritatively inside the write transaction by the repository, so a
/// concurrent change cannot bypass it.
/// </para>
/// </remarks>
public sealed class JobOnService : IJobOnService
{
    private readonly IJobOnRepository _jobOns;
    private readonly IToolRepository _tools;
    private readonly IReadOnlyList<IJobOnDependencyProbe> _dependencyProbes;

    /// <summary>Creates the service over the Job On repository, the Tool repository and the probes.</summary>
    public JobOnService(
        IJobOnRepository jobOns,
        IToolRepository tools,
        IEnumerable<IJobOnDependencyProbe> dependencyProbes)
    {
        ArgumentNullException.ThrowIfNull(jobOns);
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentNullException.ThrowIfNull(dependencyProbes);
        _jobOns = jobOns;
        _tools = tools;
        _dependencyProbes = dependencyProbes.ToList();
    }

    /// <inheritdoc />
    public async Task<JobOnResult> FindProductionsAsync(
        FindProductionsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var errors = JobOnValidator.Validate(query);
        if (errors.Count > 0)
        {
            // A blank reference is a validation failure, never an empty result (contract §8.1).
            return new JobOnResult.ValidationFailed(errors);
        }

        var productions = await _jobOns.ListByReferenceAsync(query.Reference.Trim(), cancellationToken);

        return new JobOnResult.ProductionsFound(productions);
    }

    /// <inheritdoc />
    public async Task<JobOnResult> GetAsync(Guid jobOnId, CancellationToken cancellationToken)
    {
        var jobOn = await _jobOns.GetByIdAsync(jobOnId, cancellationToken);
        if (jobOn is null)
        {
            return new JobOnResult.NotFound(jobOnId);
        }

        return new JobOnResult.Ficha(await BuildFichaAsync(jobOn, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<JobOnResult> CreateAsync(
        CreateJobOnCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = JobOnValidator.Validate(command);
        if (errors.Count > 0)
        {
            return new JobOnResult.ValidationFailed(errors);
        }

        // Fast pre-check of every supplied slot Tool, so the operator receives the contracted
        // TOOL_NOT_FOUND / TOOL_TYPE_MISMATCH code instead of a retryable conflict. The repository
        // repeats this resolution inside the insert transaction, where it is authoritative.
        var slots = new (ToolContextType ContextType, Guid? ToolId)[]
        {
            (ToolContextType.Cm, command.CmToolId),
            (ToolContextType.Mf, command.MfToolId),
            (ToolContextType.Bq, command.BqToolId),
        };

        var jobOnId = JobOnId.New();
        var contexts = new List<ToolContext>(slots.Length);

        foreach (var (contextType, toolId) in slots)
        {
            if (toolId is not { } supplied)
            {
                // A context row is created iff the corresponding Tool slot is explicitly selected:
                // a Job On with no CM association has no cm_id, and no symmetry row is created.
                continue;
            }

            var resolution = await ResolveToolAsync(contextType, supplied, cancellationToken);
            if (resolution.Errors.Count > 0)
            {
                return new JobOnResult.ValidationFailed(resolution.Errors);
            }

            contexts.Add(new ToolContext(
                contextType,
                Guid.NewGuid(),
                jobOnId,
                ToolId.From(supplied),
                resolution.Frozen!));
        }

        var jobOn = new DomainJobOn(
            jobOnId,
            command.Reference.Trim(),
            command.ProductionNumber.Trim(),
            MachineCode.From(command.Machine.Trim()),
            command.ProductionDate,
            CopiedFromJobOnId: null,
            Version: 1,
            Contexts: contexts);

        try
        {
            var created = await _jobOns.CreatedAsync(jobOn, contexts, cancellationToken);

            return new JobOnResult.Created(created.JobOnId.Value, created.Version);
        }
        catch (JobOnPersistenceException exception)
        {
            return Map(exception);
        }
        catch (ConcurrencyConflictException exception)
        {
            return Refuse(JobOnRefusalReason.StaleVersion, exception.Message);
        }
    }

    /// <inheritdoc />
    public async Task<JobOnResult> UpdateAsync(
        UpdateJobOnCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = JobOnValidator.Validate(command);
        if (errors.Count > 0)
        {
            return new JobOnResult.ValidationFailed(errors);
        }

        var persisted = await _jobOns.GetByIdAsync(command.JobOnId, cancellationToken);
        if (persisted is null)
        {
            return new JobOnResult.NotFound(command.JobOnId);
        }

        if (persisted.Version != command.ExpectedVersion)
        {
            return Refuse(
                JobOnRefusalReason.StaleVersion,
                $"The Job On changed after it was observed (expected version {command.ExpectedVersion}, " +
                $"current version {persisted.Version}); nothing was written.");
        }

        // The threshold is evaluated against the CURRENTLY PERSISTED production date, not the value
        // being submitted: changing the date in the same edit does not remove the warning.
        if (IsDateThresholdReached(persisted.ProductionDate) && !command.DateThresholdWarningAcknowledged)
        {
            return Refuse(
                JobOnRefusalReason.DateThresholdConfirmationRequired,
                "This Job On concerns a production date already reached or passed; " +
                "explicit acknowledgement is required before the edit is applied.");
        }

        var changes = new List<ToolContextChange>(command.Associations?.Count ?? 0);
        foreach (var association in command.Associations ?? [])
        {
            if (association.Action == ToolAssociationAction.Set)
            {
                var resolution = await ResolveToolAsync(
                    association.ContextType,
                    association.ToolId!.Value,
                    cancellationToken);

                if (resolution.Errors.Count > 0)
                {
                    return new JobOnResult.ValidationFailed(resolution.Errors);
                }
            }

            changes.Add(new ToolContextChange(association.ContextType, association.Action, association.ToolId));
        }

        var jobOn = new DomainJobOn(
            persisted.JobOnId,
            command.Reference.Trim(),
            command.ProductionNumber.Trim(),
            MachineCode.From(command.Machine.Trim()),
            command.ProductionDate,
            persisted.CopiedFromJobOnId,
            persisted.Version,
            persisted.Contexts);

        try
        {
            var updated = await _jobOns.UpdatedAsync(jobOn, changes, cancellationToken);

            return new JobOnResult.Updated(updated.JobOnId.Value, updated.Version);
        }
        catch (JobOnPersistenceException exception)
        {
            return Map(exception);
        }
        catch (ConcurrencyConflictException exception)
        {
            return Refuse(JobOnRefusalReason.StaleVersion, exception.Message);
        }
    }

    /// <inheritdoc />
    public async Task<JobOnResult> PreviewDuplicateAsync(
        Guid sourceJobOnId,
        CancellationToken cancellationToken)
    {
        var source = await _jobOns.GetByIdAsync(sourceJobOnId, cancellationToken);
        if (source is null)
        {
            return new JobOnResult.NotFound(sourceJobOnId);
        }

        // The preview is read-only: opening it writes nothing, touches no version and creates no
        // draft Job On. Any historical source may be previewed.
        var ficha = await BuildFichaAsync(source, cancellationToken);

        return new JobOnResult.DuplicationPreview(sourceJobOnId, source.Version, ficha);
    }

    /// <inheritdoc />
    public async Task<JobOnResult> DuplicateAsync(
        DuplicateJobOnCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = JobOnValidator.Validate(command);
        if (errors.Count > 0)
        {
            return new JobOnResult.ValidationFailed(errors);
        }

        var source = await _jobOns.GetByIdAsync(command.SourceJobOnId, cancellationToken);
        if (source is null)
        {
            return new JobOnResult.NotFound(command.SourceJobOnId);
        }

        if (source.Version != command.ExpectedSourceVersion)
        {
            return Refuse(
                JobOnRefusalReason.StaleVersion,
                $"The duplication source changed after it was previewed (expected version " +
                $"{command.ExpectedSourceVersion}, current version {source.Version}); nothing was created.");
        }

        var jobOnId = JobOnId.New();
        var duplicate = new DomainJobOn(
            jobOnId,
            source.Reference,
            command.ProductionNumber.Trim(),
            MachineCode.From(command.Machine.Trim()),
            command.ProductionDate,
            source.JobOnId.Value,
            Version: 1,
            Contexts: []);

        // Every copied context gets a NEW identity and keeps the SOURCE context's canonical tool_id
        // and frozen triple verbatim: the live Tool is deliberately not re-read, so duplication never
        // silently refreshes what the source recorded.
        var duplicatedContexts = source.Contexts
            .Select(context => new ToolContext(
                context.ContextType,
                Guid.NewGuid(),
                jobOnId,
                context.ToolId,
                context.Frozen))
            .ToList();

        try
        {
            var created = await _jobOns.DuplicatedAsync(
                duplicate,
                duplicatedContexts,
                command.ExpectedSourceVersion,
                cancellationToken);

            return new JobOnResult.Duplicated(created.JobOnId.Value, source.JobOnId.Value, created.Version);
        }
        catch (JobOnPersistenceException exception)
        {
            return Map(exception);
        }
        catch (ConcurrencyConflictException exception)
        {
            return Refuse(JobOnRefusalReason.StaleVersion, exception.Message);
        }
    }

    /// <inheritdoc />
    public async Task<JobOnResult> DeleteAsync(
        DeleteJobOnCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = JobOnValidator.Validate(command);
        if (errors.Count > 0)
        {
            return new JobOnResult.ValidationFailed(errors);
        }

        var persisted = await _jobOns.GetByIdAsync(command.JobOnId, cancellationToken);
        if (persisted is null)
        {
            return new JobOnResult.NotFound(command.JobOnId);
        }

        if (persisted.Version != command.ExpectedVersion)
        {
            return Refuse(
                JobOnRefusalReason.StaleVersion,
                $"The Job On changed after it was observed (expected version {command.ExpectedVersion}, " +
                $"current version {persisted.Version}); nothing was deleted.");
        }

        // The stronger warning of the delete path: a reached/passed production date needs the
        // acknowledgement, but it is never a hard prohibition.
        if (IsDateThresholdReached(persisted.ProductionDate) && !command.DateThresholdWarningAcknowledged)
        {
            return Refuse(
                JobOnRefusalReason.DateThresholdConfirmationRequired,
                "This Job On concerns a production date already reached or passed; " +
                "explicit acknowledgement is required before the deletion.");
        }

        var target = new JobOnDependencyTarget(
            persisted.JobOnId.Value,
            ContextId(persisted, ToolContextType.Cm),
            ContextId(persisted, ToolContextType.Mf),
            ContextId(persisted, ToolContextType.Bq));

        // EVERY registered probe is evaluated, and any non-empty report refuses the delete. A probe
        // reports a fact, never a permission and never a rule about another module's data.
        var dependencies = new List<JobOnDependency>();

        foreach (var probe in _dependencyProbes)
        {
            var report = await probe.InspectAsync(target, cancellationToken);
            if (report.HasDependencies)
            {
                dependencies.AddRange(report.Dependencies);
            }
        }

        if (dependencies.Count > 0)
        {
            return new JobOnResult.Refused(
                JobOnRefusalReason.DependencyExists,
                "Dependent operational facts exist for this Job On, so it cannot be deleted: " +
                string.Join("; ", dependencies.Select(dependency => dependency.Description)),
                ExistingJobOnId: null,
                Dependencies: dependencies);
        }

        try
        {
            await _jobOns.DeletedAsync(command.JobOnId, command.ExpectedVersion, cancellationToken);

            return new JobOnResult.Deleted(command.JobOnId);
        }
        catch (JobOnPersistenceException exception)
        {
            return Map(exception);
        }
        catch (ConcurrencyConflictException exception)
        {
            return Refuse(JobOnRefusalReason.StaleVersion, exception.Message);
        }
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private static bool IsDateThresholdReached(DateOnly? productionDate) =>
        productionDate is { } date && Today >= date;

    private static Guid? ContextId(DomainJobOn jobOn, ToolContextType contextType) =>
        jobOn.Contexts.FirstOrDefault(context => context.ContextType == contextType)?.ContextId;

    private static JobOnResult Refuse(JobOnRefusalReason reason, string message) => new JobOnResult.Refused(reason, message);

    private static JobOnResult Map(JobOnPersistenceException exception) => exception.Reason switch
    {
        JobOnPersistenceFailureReason.DuplicateProduction => new JobOnResult.Refused(
            JobOnRefusalReason.DuplicateProduction,
            exception.Message,
            exception.ExistingJobOnId),

        JobOnPersistenceFailureReason.ToolNotFound => new JobOnResult.ValidationFailed(
            [JobOnValidationErrors.ToolNotFound]),

        JobOnPersistenceFailureReason.ToolTypeMismatch => new JobOnResult.ValidationFailed(
            [JobOnValidationErrors.ToolTypeMismatch]),

        JobOnPersistenceFailureReason.DependencyExists => new JobOnResult.Refused(
            JobOnRefusalReason.DependencyExists,
            exception.Message),

        _ => throw exception,
    };

    private async Task<ToolResolution> ResolveToolAsync(
        ToolContextType contextType,
        Guid toolId,
        CancellationToken cancellationToken)
    {
        var tool = await _tools.GetByIdAsync(toolId, cancellationToken);
        if (tool is null)
        {
            return new ToolResolution([JobOnValidationErrors.ToolNotFound], null);
        }

        if (tool.Type != ToolTokens.RequiredToolType(contextType))
        {
            return new ToolResolution([JobOnValidationErrors.ToolTypeMismatch], null);
        }

        return new ToolResolution([], new ToolContextSnapshot(tool.Type, tool.Reference, tool.Lot));
    }

    private async Task<JobOnFicha> BuildFichaAsync(DomainJobOn jobOn, CancellationToken cancellationToken)
    {
        var contexts = new List<ToolContextFicha>(jobOn.Contexts.Count);

        foreach (var context in jobOn.Contexts)
        {
            // The projection is LIVE Tool state, composed at read time and never persisted; the
            // frozen triple is presented separately and is never refreshed from it.
            var tool = await _tools.GetByIdAsync(context.ToolId.Value, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Context '{context.ContextId}' references canonical Tool '{context.ToolId}' which " +
                    "does not exist; the persisted context cannot be explained truthfully.");

            contexts.Add(new ToolContextFicha(
                context.ContextType,
                context.ContextId,
                context.ToolId.Value,
                context.Frozen.Type,
                context.Frozen.Reference,
                context.Frozen.Lot,
                new ToolSummaryProjection(
                    tool.ToolId.Value,
                    tool.Type,
                    tool.Reference,
                    tool.Lot,
                    tool.Processo,
                    tool.Quantity,
                    tool.CompatibleMachines)));
        }

        return new JobOnFicha(
            jobOn.JobOnId.Value,
            jobOn.Reference,
            jobOn.ProductionNumber,
            jobOn.Machine.Value,
            jobOn.ProductionDate,
            jobOn.CopiedFromJobOnId,
            jobOn.Version,
            contexts);
    }

    private sealed record ToolResolution(IReadOnlyList<string> Errors, ToolContextSnapshot? Frozen);
}
