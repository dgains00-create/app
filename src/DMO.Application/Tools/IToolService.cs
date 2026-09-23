namespace DMO.Application.Tools;

/// <summary>
/// The single shared canonical Tool search/select/create service contract.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §9.1 and §12.3.
/// <para>
/// One shared orchestration serves every P2-T04 surface; later workstreams consume the same
/// contract rather than forking it. The service holds no <c>HttpContext</c>, no route knowledge and
/// no presentation type, and it never reads another module's tables.
/// </para>
/// </remarks>
public interface IToolService
{
    /// <summary>Searches the canonical registry under the contracted bounded predicates.</summary>
    Task<ToolResult> SearchAsync(ToolSearchQuery query, CancellationToken cancellationToken);

    /// <summary>Reads the contextual Tool ficha, or <c>NotFound</c>.</summary>
    Task<ToolResult> GetAsync(Guid toolId, CancellationToken cancellationToken);

    /// <summary>Creates the canonical Tool and returns its real canonical identity.</summary>
    Task<ToolResult> CreateAsync(CreateToolCommand command, CancellationToken cancellationToken);
}
