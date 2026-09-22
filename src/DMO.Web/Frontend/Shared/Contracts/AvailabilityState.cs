namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The frozen A3 <c>AvailabilityState</c> vocabulary — eight mutually distinct outcomes.
/// </summary>
/// <remarks>
/// Authority: <c>docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md</c> §11 (FINAL PRESENTATION
/// CONTRACT) and <c>dmo-beta-master/contracts/SHARED_FRONTEND.md</c> "AvailabilityState";
/// the Beta-facing meanings are also recorded in
/// <c>dmo-beta-master/contracts/DOCUMENTS_AND_FILES.md</c> §5.
/// <para>
/// Frozen distinctions implemented by this vocabulary:
/// </para>
/// <list type="bullet">
/// <item><see cref="NotApplicable"/> is <b>not</b> an error;</item>
/// <item><see cref="FileMissing"/> is <b>not</b> <see cref="NotGenerated"/>;</item>
/// <item><see cref="LookupFailed"/> is never rendered as no-file/no-record.</item>
/// </list>
/// <para>
/// Common states (<c>loading</c>, <c>permission-denied</c>, <c>saving</c>, <c>submitting</c>,
/// <c>stale</c>, <c>conflict</c>) may <b>wrap</b> these outcomes but never <b>replace</b> them
/// (freeze §11).
/// </para>
/// </remarks>
public enum AvailabilityState
{
    /// <summary>A document/output is available; supplied open/download may be enabled.</summary>
    Available,

    /// <summary>Not generated yet; generation exists only if the consumer supplies it.</summary>
    NotGenerated,

    /// <summary>Pending approval, plus supplied allowed actions.</summary>
    AwaitingApproval,

    /// <summary>The workspace is unavailable; no invented fallback.</summary>
    WorkspaceUnavailable,

    /// <summary>A derived file is missing without denying the owning record.</summary>
    FileMissing,

    /// <summary>Explicit supplied version choice/action.</summary>
    VersionsAvailable,

    /// <summary>Neutral explanation; explicitly <b>not</b> an error.</summary>
    NotApplicable,

    /// <summary>The lookup itself failed; not another availability state.</summary>
    LookupFailed,
}
