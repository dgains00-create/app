namespace DMO.Application.Boquilhas;

/// <summary>
/// The narrow read-only <c>bq_contexts</c> traversal seam of the Boquilhas area.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §15.3/§22.4/§24 (frozen-triple presentation and production-context
/// reads through <c>bq_id → job_ons</c>). The shipped P2-T05 precedent
/// (<c>IPesoContextRead</c>/<c>DmoPesoContextRead</c>, review ACCEPT 4d88dbe) established this
/// exact shape for the identical gap: no application contract of the closed P2-T04 surface resolves
/// a bare <c>bq_id</c> (the Job On ficha read is <c>jobon_id</c>-keyed and the candidates read is
/// <c>tool_id</c>-keyed), so this single-table SELECT completes the anchor traversal. It is
/// read-only: no write, no creation, no identity minting, no foreign-state duplication — exactly
/// the accepted read-only composition pattern of review observation N1. Every other Job On fact
/// (reference/production number/machine) still comes from <c>IJobOnService</c>; this seam resolves
/// only the <c>bq_id → jobon_id + tool_id + frozen triple</c> chain.</remarks>
public interface IBoquilhasContextRead
{
    /// <summary>
    /// Reads one real <c>bq_contexts</c> row by identity, or <c>null</c> when it does not exist.
    /// </summary>
    Task<BqContextRead?> GetBqContextAsync(Guid bqId, CancellationToken cancellationToken);
}

/// <summary>
/// One real frozen <c>bq_contexts</c> row: the Job On owner, the direct canonical Tool relation and
/// the frozen type/reference/lot triple (presented as historical/production fact, never as the
/// Tool's current value).
/// </summary>
public sealed record BqContextRead(
    Guid BqId,
    Guid JobOnId,
    Guid ToolId,
    string ToolType,
    string ToolReference,
    string ToolLot);