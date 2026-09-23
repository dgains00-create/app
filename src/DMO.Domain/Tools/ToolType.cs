namespace DMO.Domain.Tools;

/// <summary>
/// The closed Tool family set: the Tool-owned identity fact <c>tool_type</c>.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §2.3 (<c>ToolType = CM | MF | BQ</c>), §3.1 and §4.4
/// (<c>tools_type_check</c>). <c>CM</c>, <c>MF</c> and <c>BQ</c> are distinct Tool families, so
/// the type participates in the canonical Tool identity tuple.
/// </remarks>
public enum ToolType
{
    /// <summary>CM Tool family.</summary>
    Cm,

    /// <summary>MF Tool family.</summary>
    Mf,

    /// <summary>BQ Tool family.</summary>
    Bq,
}
