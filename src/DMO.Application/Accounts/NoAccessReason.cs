namespace DMO.Application.Accounts;

/// <summary>
/// Account-resolution failure facts. Resolution-level only: they describe why an
/// authenticated identity did not map to an active application account.
/// </summary>
public enum NoAccessReason
{
    /// <summary>No application mapping exists for this authenticated identity.</summary>
    UnknownAccount,

    /// <summary>The identity maps to the ADMIN account, but the ADMIN is inactive.</summary>
    InactiveAdmin,

    /// <summary>The identity maps to a USER account, but the USER is inactive.</summary>
    InactiveUser,

    /// <summary>The identity maps to more than one application account.</summary>
    AmbiguousMapping,
}