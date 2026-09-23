namespace DMO.Domain.Tools;

/// <summary>
/// The closed process value set of a Tool-owned fact (<c>processo</c>).
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §2.3 (<c>Processo = NNPB | PS</c>), §3.1 and §4.4
/// (<c>tools_processo_check</c>: <c>NULL</c> or one of the two values).
/// <para>
/// <c>processo</c> is Tool-owned and is reached through the Job On's CM context
/// (<c>jobon_id -&gt; cm_id -&gt; tool_id -&gt; processo</c>). It is never a Job On fact and never a
/// context fact (contract §6.3).
/// </para>
/// </remarks>
public enum Processo
{
    /// <summary>NNPB.</summary>
    Nnpb,

    /// <summary>PS.</summary>
    Ps,
}
