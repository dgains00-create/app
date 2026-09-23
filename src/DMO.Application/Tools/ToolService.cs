using DMO.Application.Persistence;
using DMO.Application.Repositories;
using DMO.Domain.Tools;

namespace DMO.Application.Tools;

/// <summary>
/// The single shared canonical Tool search/select/create orchestration.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §5.3 (create semantics), §5.5 (two-layer duplicate prevention), §5.6
/// and §8.2 (search behaviour), §9.1/§9.3 (one shared orchestration) and §12.3 (service rules).
/// <para>
/// There is no Tool update path and no Tool delete path: the Tool is immutable in this workstream,
/// so no <c>PUT</c>/<c>PATCH</c>/<c>DELETE</c> Tool primitive exists here or anywhere else.
/// </para>
/// </remarks>
public sealed class ToolService : IToolService
{
    private readonly IToolRepository _repository;

    /// <summary>Creates the service over the canonical Tool repository.</summary>
    public ToolService(IToolRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <inheritdoc />
    public async Task<ToolResult> SearchAsync(ToolSearchQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var errors = ToolValidator.Validate(query);
        if (errors.Count > 0)
        {
            return new ToolResult.ValidationFailed(errors);
        }

        // One query shape only: the validated query is projected into the repository criteria
        // record, so there is no second validation layer (contract §12.2 rule 0).
        var criteria = new ToolSearchCriteria(
            Trim(query.Query),
            query.Type,
            Trim(query.Reference),
            Trim(query.Lot),
            query.Machine,
            query.Limit);

        var tools = await _repository.SearchAsync(criteria, cancellationToken);

        return new ToolResult.SearchResults(tools.Select(ToItem).ToList());
    }

    /// <inheritdoc />
    public async Task<ToolResult> GetAsync(Guid toolId, CancellationToken cancellationToken)
    {
        var tool = await _repository.GetByIdAsync(toolId, cancellationToken);
        if (tool is null)
        {
            return new ToolResult.NotFound(toolId);
        }

        var usages = await _repository.ListUsageOccurrencesAsync(toolId, cancellationToken);

        return new ToolResult.Found(new ToolFicha(
            tool.ToolId.Value,
            tool.Type,
            tool.Reference,
            tool.Lot,
            tool.Processo,
            tool.Quantity,
            tool.CompatibleMachines,
            usages));
    }

    /// <inheritdoc />
    public async Task<ToolResult> CreateAsync(CreateToolCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = ToolValidator.Validate(command);
        if (errors.Count > 0)
        {
            return new ToolResult.ValidationFailed(errors);
        }

        var type = ToolTokens.ParseType(command.Type!.Trim())!.Value;
        var reference = command.Reference!.Trim();
        var lot = command.Lot!.Trim();
        var processo = string.IsNullOrWhiteSpace(command.Processo)
            ? (Processo?)null
            : ToolTokens.ParseProcesso(command.Processo.Trim())!.Value;

        var machines = command.CompatibleMachines!
            .Where(machine => !string.IsNullOrWhiteSpace(machine))
            .Select(machine => MachineCode.From(machine.Trim()))
            .ToList();

        // Layer 1 of the two-layer duplicate prevention: the identity query produces the useful
        // operator message and names the existing canonical tool_id. Layer 2 is the unique index,
        // mapped below for the losing concurrent insert.
        var existing = await _repository.FindByIdentityAsync(type, reference, lot, cancellationToken);
        if (existing is not null)
        {
            return new ToolResult.DuplicateIdentity(existing.ToolId.Value, DuplicateMessage(reference, lot));
        }

        var tool = new Tool(
            ToolId.New(),
            type,
            reference,
            lot,
            processo,
            command.Quantity,
            machines);

        try
        {
            var created = await _repository.CreatedAsync(tool, machines, cancellationToken);

            return new ToolResult.Created(created.ToolId.Value);
        }
        catch (ToolPersistenceException exception)
            when (exception.Reason == ToolPersistenceFailureReason.DuplicateIdentity)
        {
            // The repository reads the winning row when it can; otherwise the identity is resolved
            // here. A canonical identity is never fabricated: when it cannot be named, the genuine
            // infrastructure failure propagates instead of a made-up identity.
            var winnerId = exception.ExistingToolId
                ?? (await _repository.FindByIdentityAsync(type, reference, lot, cancellationToken))?.ToolId.Value;

            if (winnerId is { } existingToolId)
            {
                return new ToolResult.DuplicateIdentity(existingToolId, DuplicateMessage(reference, lot));
            }

            throw;
        }
    }

    private static ToolSearchItem ToItem(Tool tool) => new(
        tool.ToolId.Value,
        tool.Type,
        tool.Reference,
        tool.Lot,
        tool.Processo,
        tool.Quantity,
        tool.CompatibleMachines);

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string DuplicateMessage(string reference, string lot) =>
        $"A canonical Tool with this type, reference '{reference}' and lot '{lot}' already exists; " +
        "select the existing Tool instead of creating a duplicate.";
}
