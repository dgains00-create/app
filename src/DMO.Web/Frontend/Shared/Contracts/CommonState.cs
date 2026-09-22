namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The frozen A3 shared presentation-state vocabulary.
/// </summary>
/// <remarks>
/// Authority: <c>docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md</c> §4 (FINAL PRESENTATION
/// CONTRACT, Architect-accepted) and <c>dmo-beta-master/contracts/SHARED_FRONTEND.md</c>
/// "Common state vocabulary".
/// <para>
/// These are <b>presentation states, not a backend error taxonomy</b>. Consumers map real
/// application/backend outcomes into one of these states; no state carries domain meaning,
/// lifecycle authority or an error code.
/// </para>
/// <para>
/// A consumer must never map <see cref="LookupFailed"/>, <see cref="PermissionDenied"/> or
/// <see cref="Conflict"/> to <see cref="Empty"/> (freeze §4 closing rule).
/// </para>
/// </remarks>
public enum CommonState
{
    /// <summary>Initial/replacement information is being obtained.</summary>
    Loading,

    /// <summary>Required presentation information is available.</summary>
    Ready,

    /// <summary>Request succeeded but returned no items/records.</summary>
    Empty,

    /// <summary>A requested lookup did not complete.</summary>
    LookupFailed,

    /// <summary>Required context/service is presently unavailable for a non-permission reason.</summary>
    Unavailable,

    /// <summary>The published access decision denies the surface/action.</summary>
    PermissionDenied,

    /// <summary>Generic save is in progress.</summary>
    Saving,

    /// <summary>A consumer-owned transition is in progress.</summary>
    Submitting,

    /// <summary>Shown information is older than its accepted source.</summary>
    Stale,

    /// <summary>A concurrency conflict was reported by the consumer.</summary>
    Conflict,
}
