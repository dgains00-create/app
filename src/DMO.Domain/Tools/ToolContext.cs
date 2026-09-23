using DMO.Domain.JobOn;

namespace DMO.Domain.Tools;

/// <summary>
/// The production-specific Tool context of one Job On occurrence: the CM, MF or BQ context.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §2.1, §2.2, §3.4, §7.
/// <para>
/// The context retains a <b>direct</b> relation to the canonical <see cref="ToolId"/> and freezes
/// the type/reference/lot triple actually used. It is not a Tool identity: the context identity
/// (<see cref="ContextId"/>) is a Job On-owned identity and never replaces the canonical
/// <c>tool_id</c>.
/// </para>
/// <para>
/// Deliberately absent: quantity, <c>processo</c>, operational note, Baffle/calote, measurements,
/// Boquilhas facts, any status/lifecycle state and any machine column (the machine is the Job On's
/// production fact, never a second per-context copy).
/// </para>
/// </remarks>
public sealed record ToolContext(
    ToolContextType ContextType,
    Guid ContextId,
    JobOnId JobOnId,
    ToolId ToolId,
    ToolContextSnapshot Frozen);
