using DMO.Application.Repositories;
using DMO.Domain.Tools;

namespace DMO.Application.Tools;

/// <summary>
/// The exact transport/domain tokens of the closed P2-T04 Tool value sets.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §2.3 (<c>ToolType = CM | MF | BQ</c>, <c>Processo = NNPB | PS</c>,
/// <c>MachineCode = B1 | B2 | B3 | C1 | C2 | C3</c>) and §3.1 (the stored tokens).
/// <para>
/// The mapping is explicit and ordinal: no case folding, no normalisation and no fuzzy matching is
/// invented anywhere in this workstream (contract §5.4 and Q14).
/// </para>
/// </remarks>
public static class ToolTokens
{
    /// <summary>The stored <c>tool_type</c> token of a Tool type.</summary>
    public static string ToToken(ToolType type) => type switch
    {
        ToolType.Cm => "CM",
        ToolType.Mf => "MF",
        ToolType.Bq => "BQ",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown Tool type."),
    };

    /// <summary>Parses a stored <c>tool_type</c> token, or <c>null</c> when it is not settled.</summary>
    public static ToolType? ParseType(string? token) => token switch
    {
        "CM" => ToolType.Cm,
        "MF" => ToolType.Mf,
        "BQ" => ToolType.Bq,
        _ => null,
    };

    /// <summary>The stored <c>processo</c> token, or <c>null</c> when not applicable.</summary>
    public static string? ToToken(Processo? processo) => processo switch
    {
        null => null,
        Processo.Nnpb => "NNPB",
        Processo.Ps => "PS",
        _ => throw new ArgumentOutOfRangeException(nameof(processo), processo, "Unknown processo."),
    };

    /// <summary>Parses a stored <c>processo</c> token, or <c>null</c> when it is not settled.</summary>
    public static Processo? ParseProcesso(string? token) => token switch
    {
        "NNPB" => Processo.Nnpb,
        "PS" => Processo.Ps,
        _ => null,
    };

    /// <summary>The stored context-type token of a context kind.</summary>
    public static string ToToken(ToolContextType contextType) => contextType switch
    {
        ToolContextType.Cm => "CM",
        ToolContextType.Mf => "MF",
        ToolContextType.Bq => "BQ",
        _ => throw new ArgumentOutOfRangeException(nameof(contextType), contextType, "Unknown context type."),
    };

    /// <summary>Parses a stored context-type token, or <c>null</c> when it is not settled.</summary>
    public static ToolContextType? ParseContextType(string? token) => token switch
    {
        "CM" => ToolContextType.Cm,
        "MF" => ToolContextType.Mf,
        "BQ" => ToolContextType.Bq,
        _ => null,
    };

    /// <summary>The Tool family a context kind is bound to (contract §7.4, Q3).</summary>
    public static ToolType RequiredToolType(ToolContextType contextType) => contextType switch
    {
        ToolContextType.Cm => ToolType.Cm,
        ToolContextType.Mf => ToolType.Mf,
        ToolContextType.Bq => ToolType.Bq,
        _ => throw new ArgumentOutOfRangeException(nameof(contextType), contextType, "Unknown context type."),
    };
}

/// <summary>
/// The bounded canonical Tool search query: the input carrier of contract §8.2.
/// </summary>
/// <remarks>
/// <para>
/// The query applies only the contracted predicates. No "same machine only", "latest only",
/// "in-stock only" or "compatible ranking" rule exists, and the query never resolves "the Tool" for
/// a reference/lot/machine combination.
/// </para>
/// <para>
/// <see cref="Limit"/> is required and bounded to <c>1..100</c>; at least one criterion must be
/// supplied, so no unbounded registry dump is contracted.
/// </para>
/// </remarks>
public sealed record ToolSearchQuery(
    string? Query,
    ToolType? Type,
    string? Reference,
    string? Lot,
    MachineCode? Machine,
    int Limit);

/// <summary>
/// The Tool create command: the six Beta minimum Tool facts and nothing else.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §5.2/§5.3. The closed-set facts are carried as supplied tokens so the
/// validator can report the contracted machine-readable codes (<c>TOOL_TYPE_REQUIRED</c>,
/// <c>TOOL_TYPE_UNKNOWN</c>, <c>PROCESSO_UNKNOWN</c>, <c>MACHINE_UNKNOWN</c>,
/// <c>MACHINE_COMPATIBILITY_REQUIRED</c>) before any write.
/// <para>
/// No <c>tool_id</c> is carried: the backend allocates the canonical identity and returns it. No
/// provisional, temporary, display-key or client-minted identity exists anywhere.
/// </para>
/// </remarks>
public sealed record CreateToolCommand(
    string? Type,
    string? Reference,
    string? Lot,
    string? Processo,
    int? Quantity,
    IReadOnlyList<string>? CompatibleMachines);

/// <summary>
/// One canonical Tool search result item: the canonical identity plus Tool-owned facts only.
/// </summary>
/// <remarks>
/// Deliberately not in the item: any fabricated status, availability, technical condition,
/// stock/location state, utilisation, operational note or "compatible with your Job On" verdict.
/// </remarks>
public sealed record ToolSearchItem(
    Guid ToolId,
    ToolType Type,
    string Reference,
    string Lot,
    Processo? Processo,
    int? Quantity,
    IReadOnlyList<MachineCode> CompatibleMachines);

/// <summary>
/// The contextual Tool ficha read model: live canonical Tool facts plus the Job On usage
/// occurrences this workstream owns.
/// </summary>
public sealed record ToolFicha(
    Guid ToolId,
    ToolType Type,
    string Reference,
    string Lot,
    Processo? Processo,
    int? Quantity,
    IReadOnlyList<MachineCode> CompatibleMachines,
    IReadOnlyList<ToolUsageOccurrence> UsageOccurrences);

/// <summary>
/// The result of exactly one explicit human Tool selection for exactly one origin slot.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §9.3.3. It is built by the caller from the resolved opaque candidate
/// key, never from display text, and always carries exactly one canonical <c>tool_id</c>.
/// </remarks>
public sealed record ToolSelection(Guid ToolId, ToolType ExpectedType);

/// <summary>
/// The closed canonical Tool result set.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §14.1. No result type carries a lifecycle status, an availability
/// state, a document state, a Controlo or Boquilhas fact, a permission decision or a navigation
/// target.
/// </remarks>
public abstract record ToolResult
{
    private ToolResult()
    {
    }

    /// <summary>The bounded search result.</summary>
    public sealed record SearchResults(IReadOnlyList<ToolSearchItem> Items) : ToolResult;

    /// <summary>The contextual Tool ficha.</summary>
    public sealed record Found(ToolFicha Ficha) : ToolResult;

    /// <summary>The real canonical <c>tool_id</c> allocated and persisted by the backend.</summary>
    public sealed record Created(Guid ToolId) : ToolResult;

    /// <summary>The exact contracted validation codes; nothing was written.</summary>
    public sealed record ValidationFailed(IReadOnlyList<string> Errors) : ToolResult;

    /// <summary>The requested Tool does not exist.</summary>
    public sealed record NotFound(Guid ToolId) : ToolResult;

    /// <summary>A canonical Tool with this identity tuple already exists.</summary>
    public sealed record DuplicateIdentity(Guid ExistingToolId, string Message) : ToolResult;
}
