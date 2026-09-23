namespace DMO.Domain.Tools;

/// <summary>
/// The context kind of a production-specific Tool context: CM, MF or BQ.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §2.2 (identity semantics) and §3.4 (three context tables, one per
/// context identity kind). Each kind is bound to its own Tool family: an MF context can only
/// reference an MF Tool (contract §7.4, <c>TOOL_TYPE_MISMATCH</c>, and the per-context database
/// CHECK that makes a context/frozen-type disagreement impossible).
/// </remarks>
public enum ToolContextType
{
    /// <summary>CM production context.</summary>
    Cm,

    /// <summary>MF production context.</summary>
    Mf,

    /// <summary>BQ production context.</summary>
    Bq,
}
